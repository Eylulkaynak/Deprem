import io
import json
from pathlib import Path
import secrets
import tempfile
import threading
import unittest
import urllib.error
import urllib.request
from wsgiref.simple_server import make_server

from server import ApiError, CATALOG, OnlineService, QuietHandler, ThreadedServer, WsgiApplication


class OnlineTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.now = 1791198000.0
        self.path = Path(self.directory.name) / "online.sqlite3"
        self.service = OnlineService(self.path, clock=lambda: self.now)
        self.credentials = {}
        self.a = self.register("Ada")
        self.b = self.register("Bora")
        self.c = self.register("Can")

    def tearDown(self):
        self.service.db.close()
        self.directory.cleanup()

    def register(self, name):
        credential = secrets.token_hex(32)
        snapshot = self.service.dispatch("POST", "/v1/session", {"name":name}, credential)
        self.credentials[snapshot["me"]["id"]] = credential
        return snapshot["me"]

    def call(self, who, path, payload=None, method="POST", query=None):
        return self.service.dispatch(method,path,payload,self.credentials[who["id"]],query)

    def state(self, who):
        return self.call(who,"/v1/state",method="GET")

    def room(self, game="bag-packing", adult=False, invite=None):
        body = {"gameId":game,"adult":adult,"requestId":secrets.token_hex(16)}
        if invite:
            body["friendId"] = invite["id"]
        r = self.call(self.a,"/v1/rooms",body)["room"]
        self.call(self.b,"/v1/rooms/join",{"code":r["code"]})
        self.call(self.a,f'/v1/rooms/{r["id"]}/ready')
        r = self.call(self.b,f'/v1/rooms/{r["id"]}/ready')["room"]
        self.assertEqual(r["state"],"countdown")
        self.now += 5
        return self.state(self.a)["room"]

    def actions(self, r):
        d = r["round"]
        if d["type"] == "bag":
            values = [{"key":key} for key in d["correctItems"]]
        elif d["type"] == "sort":
            values = [{"key":key,"target":"pack" if key in d["packItems"] else "leave"} for key in d["items"]]
        elif d["type"] == "match":
            values = [{"key":t["accepts"],"target":t["id"]} for t in d["targets"]]
        elif d["type"] == "quiz":
            values = [{"index":i,"key":str(next(j for j,c in enumerate(q["choices"]) if c["correct"]))} for i,q in enumerate(d["questions"])]
        elif d["type"] == "danger":
            values = [{"key":a["id"]} for a in d["actions"] if a["dangerous"]]
        elif d["type"] == "sequence":
            values = [{"key":s["id"]} for q in d["rounds"] for s in q["steps"]]
        elif d["type"] == "memory":
            values = [{"key":key} for key in d["sequence"]]
        else:
            indices = [i for i,s in enumerate(d["catchSpawns"]) if s["good"]][:r["goal"]]
            self.now = max(self.now,r["startsAt"]+d["catchSpawns"][indices[-1]]["at"]+4)
            values = [{"key":"catch","index":i} for i in indices]
        return [dict(value,seq=i+1) for i,value in enumerate(values)]

    def send(self, who, r, events):
        return self.call(who,f'/v1/rooms/{r["id"]}/actions',{"events":events,"score":999999})

    def board(self, who, adult=False, **filters):
        return self.call(who,"/v1/leaderboard",method="GET",query={k:[str(v)] for k,v in dict(adult="true" if adult else "false",**filters).items()})

    def friend(self):
        self.call(self.a,"/v1/friends/request",{"code":self.b["code"]})
        self.call(self.b,"/v1/friends/respond",{"playerId":self.a["id"],"accept":True})

    def test_session_is_idempotent_private_and_persistent(self):
        snapshot = self.call(self.a,"/v1/session",{"name":"Ada Yeni"})
        self.assertEqual(snapshot["me"]["id"],self.a["id"])
        self.assertEqual(snapshot["me"]["code"],self.a["code"])
        self.assertNotIn("credential",json.dumps(snapshot))
        self.service.db.close()
        self.service = OnlineService(self.path,clock=lambda:self.now)
        self.assertEqual(self.state(self.a)["me"]["name"],"Ada Yeni")
        with self.assertRaises(ApiError) as error:
            self.service.dispatch("GET","/v1/state")
        self.assertEqual(error.exception.status,401)

    def test_friend_request_consent_reverse_request_remove_and_reject(self):
        self.call(self.a,"/v1/friends/request",{"code":self.b["code"]})
        self.assertEqual(len(self.state(self.a)["friends"]),0)
        self.assertEqual(self.state(self.b)["incoming"][0]["id"],self.a["id"])
        self.call(self.b,"/v1/friends/request",{"code":self.a["code"]})
        self.assertEqual(self.state(self.a)["friends"][0]["id"],self.b["id"])
        self.call(self.a,"/v1/friends/remove",{"playerId":self.b["id"]})
        self.assertEqual(len(self.state(self.b)["friends"]),0)
        self.call(self.a,"/v1/friends/request",{"code":self.b["code"]})
        self.call(self.b,"/v1/friends/respond",{"playerId":self.a["id"],"accept":False})
        self.assertEqual(len(self.state(self.a)["outgoing"]),0)
        with self.assertRaises(ApiError):
            self.call(self.a,"/v1/friends/request",{"code":self.a["code"]})

    def test_private_invite_access_and_decline(self):
        self.friend()
        r = self.call(self.a,"/v1/rooms",{"gameId":"safe-choice","adult":False,"friendId":self.b["id"],"requestId":secrets.token_hex(16)})["room"]
        self.assertEqual(self.state(self.b)["invites"][0]["roomId"],r["id"])
        with self.assertRaises(ApiError) as error:
            self.call(self.c,"/v1/rooms/join",{"code":r["code"]})
        self.assertEqual(error.exception.status,403)
        with self.assertRaises(ApiError):
            self.call(self.c,f'/v1/rooms/{r["id"]}/ready')
        self.call(self.b,f'/v1/rooms/{r["id"]}/decline')
        self.assertEqual(self.state(self.a)["room"]["state"],"cancelled")

    def test_room_retry_capacity_and_common_round(self):
        body = {"gameId":"match-pairs","adult":False,"requestId":secrets.token_hex(16)}
        r = self.call(self.a,"/v1/rooms",body)["room"]
        self.assertEqual(self.call(self.a,"/v1/rooms",body)["room"]["id"],r["id"])
        joined = self.call(self.b,"/v1/rooms/join",{"code":r["code"]})["room"]
        self.assertEqual(r["round"],joined["round"])
        self.assertEqual(r["seed"],joined["seed"])
        with self.assertRaises(ApiError):
            self.call(self.c,"/v1/rooms/join",{"code":r["code"]})

    def test_all_eight_games_in_both_age_modes_finish_and_rank(self):
        for adult in (False,True):
            for game in ("bag-packing","category-sorting","match-pairs","safe-choice","danger-hunt","catch-bag","safe-order","memory-bag"):
                with self.subTest(adult=adult,game=game):
                    r = self.room(game,adult)
                    moves = self.actions(r)
                    self.send(self.a,r,moves)
                    self.now += 1
                    result = self.send(self.b,r,moves)["room"]
                    self.assertEqual(result["state"],"finished")
                    self.assertEqual(result["winnerId"],self.a["id"])
                    self.assertTrue(all(p["score"]==1000 for p in result["players"]))
            self.assertEqual(self.board(self.a,adult)["self"]["score"],8000)

    def test_server_scores_actions_and_retries_exactly_once(self):
        r = self.room()
        wrong = next(i for i in r["round"]["items"] if i not in r["round"]["correctItems"])
        events = [{"seq":1,"key":wrong}] + [dict(e,seq=e["seq"]+1) for e in self.actions(r)]
        first = self.send(self.a,r,events)
        again = self.send(self.a,r,events)
        self.assertEqual(first["room"]["players"][0]["score"],925)
        self.assertEqual(first["room"]["players"][0]["eventSeq"],again["room"]["players"][0]["eventSeq"])
        self.send(self.b,r,self.actions(r))
        self.send(self.a,r,events)
        count = self.service.db.execute("SELECT COUNT(*) FROM results WHERE room=?",(r["id"],)).fetchone()[0]
        self.assertEqual(count,2)
        self.assertEqual(self.board(self.a)["self"]["score"],925)

    def test_duplicate_item_and_malformed_batch_do_not_inflate_or_partially_commit(self):
        r = self.room()
        item = r["round"]["correctItems"][0]
        self.send(self.a,r,[{"seq":1,"key":item},{"seq":2,"key":item}])
        self.assertEqual(self.state(self.a)["room"]["players"][0]["done"],1)
        with self.assertRaises(ApiError):
            self.send(self.a,r,[{"seq":3,"key":r["round"]["correctItems"][1]},{"seq":5,"key":"fake"}])
        me = self.state(self.a)["room"]["players"][0]
        self.assertEqual((me["done"],me["eventSeq"]),(1,2))

    def test_memory_error_and_replay_reset_progress_without_extra_points(self):
        r = self.room("memory-bag")
        first = r["round"]["sequence"][0]
        wrong = next(p for p in r["round"]["memoryPads"] if p!=r["round"]["sequence"][1])
        self.send(self.a,r,[{"seq":1,"key":first},{"seq":2,"key":wrong},{"seq":3,"key":"replay"}])
        me = self.state(self.a)["room"]["players"][0]
        self.assertEqual((me["done"],me["mistakes"]),(0,1))
        result = self.send(self.a,r,[dict(e,seq=e["seq"]+3) for e in self.actions(r)])["room"]["players"][0]
        self.assertEqual((result["status"],result["score"]),("finished",925))

    def test_catch_events_cannot_precede_spawn_or_repeat_for_credit(self):
        r = self.room("catch-bag")
        index = next(i for i,s in enumerate(r["round"]["catchSpawns"]) if s["good"])
        with self.assertRaises(ApiError):
            self.send(self.a,r,[{"seq":1,"key":"catch","index":index}])
        self.now += r["round"]["catchSpawns"][index]["at"]+4
        self.send(self.a,r,[{"seq":1,"key":"catch","index":index},{"seq":2,"key":"catch","index":index}])
        self.assertEqual(self.state(self.a)["room"]["players"][0]["done"],1)

    def test_leave_timeout_ack_and_lobby_expiry(self):
        r = self.room()
        self.send(self.a,r,self.actions(r))
        self.call(self.b,f'/v1/rooms/{r["id"]}/leave')
        result = self.state(self.a)["room"]
        self.assertEqual((result["state"],result["winnerId"]),("finished",self.a["id"]))
        self.assertIsNone(self.state(self.b)["room"])
        self.call(self.a,f'/v1/rooms/{r["id"]}/ack')
        self.assertIsNone(self.state(self.a)["room"])
        r = self.room()
        self.send(self.a,r,self.actions(r))
        self.now += 61
        self.assertEqual(self.state(self.a)["room"]["state"],"finished")
        self.assertEqual(self.state(self.a)["room"]["players"][1]["status"],"left")
        self.now += 1
        pending = self.call(self.a,"/v1/rooms",{"gameId":"safe-order","adult":False,"requestId":secrets.token_hex(16)})["room"]
        self.now += 301
        self.assertEqual(self.state(self.a)["room"]["state"],"expired")

    def test_best_scores_filters_and_weekly_window(self):
        r = self.room(); moves = self.actions(r)
        self.send(self.a,r,moves); self.send(self.b,r,moves)
        self.assertEqual(self.state(self.a)["room"]["winnerId"],"")
        r = self.room(); moves = self.actions(r)
        self.send(self.a,r,moves); self.send(self.b,r,moves)
        self.assertEqual(self.board(self.a)["self"]["score"],1000)
        self.assertEqual(self.board(self.a,scope="friends")["entries"][0]["id"],self.a["id"])
        self.assertEqual(len(self.board(self.a,adult=True)["entries"]),0)
        self.friend()
        self.assertEqual(len(self.board(self.a,scope="friends")["entries"]),2)
        self.assertEqual(len(self.board(self.a,game="safe-choice")["entries"]),0)
        self.now += 8*86400
        self.assertEqual(len(self.board(self.a,period="week")["entries"]),0)
        self.assertEqual(len(self.board(self.a)["entries"]),2)

    def test_wsgi_validation_and_real_two_client_http_flow(self):
        app = WsgiApplication(self.service)
        statuses = []
        response = app({"REQUEST_METHOD":"POST","PATH_INFO":"/v1/session","CONTENT_LENGTH":"2","wsgi.input":io.BytesIO(b"[]")},lambda status,headers:statuses.append(status))
        self.assertEqual(statuses[0],"400 Bad Request")
        with make_server("127.0.0.1",0,app,ThreadedServer,QuietHandler) as http:
            worker = threading.Thread(target=http.serve_forever,daemon=True); worker.start()
            base = f"http://127.0.0.1:{http.server_port}"
            def request(who,path,payload=None):
                data = json.dumps(payload).encode() if payload is not None else None
                req = urllib.request.Request(base+path,data=data,headers={"Authorization":"Bearer "+self.credentials[who["id"]],"Content-Type":"application/json"})
                return json.load(urllib.request.urlopen(req,timeout=3))
            request(self.a,"/v1/friends/request",{"code":self.b["code"]})
            request(self.b,"/v1/friends/respond",{"playerId":self.a["id"],"accept":True})
            r = request(self.a,"/v1/rooms",{"gameId":"bag-packing","adult":False,"requestId":secrets.token_hex(16)})["room"]
            request(self.b,"/v1/rooms/join",{"code":r["code"]})
            request(self.a,f'/v1/rooms/{r["id"]}/ready',{})
            request(self.b,f'/v1/rooms/{r["id"]}/ready',{})
            self.now += 5
            events = self.actions(r)
            request(self.a,f'/v1/rooms/{r["id"]}/actions',{"events":events})
            self.now += 1
            result = request(self.b,f'/v1/rooms/{r["id"]}/actions',{"events":events})
            self.assertEqual(result["room"]["winnerId"],self.a["id"])
            self.assertEqual(request(self.a,"/v1/leaderboard?adult=false")["self"]["score"],1000)
            http.shutdown(); worker.join(timeout=3)


if __name__ == "__main__":
    unittest.main(verbosity=2)
