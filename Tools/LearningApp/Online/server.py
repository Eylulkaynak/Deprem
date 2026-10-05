"""Deprem learning social/race API. Python 3.11+, SQLite, WSGI.

Local: python server.py --host 127.0.0.1 --port 8765
Production: gunicorn --workers 1 --threads 8 server:application (behind HTTPS).
"""
from __future__ import annotations

import argparse
from collections import defaultdict, deque
import copy
import hashlib
import json
import logging
import os
from pathlib import Path
import random
import re
import secrets
import sqlite3
from socketserver import ThreadingMixIn
import threading
import time
from urllib.parse import parse_qs
from wsgiref.simple_server import WSGIServer, WSGIRequestHandler, make_server

SOURCE_DIR = Path(__file__).resolve().parent
ROOT = SOURCE_DIR.parents[2] if len(SOURCE_DIR.parents) >= 3 else SOURCE_DIR
CATALOG = Path(os.environ.get("DEPREM_ONLINE_CATALOG",str(ROOT / "Assets/LearningApp/Resources/LearningApp/catalog.json")))
TERMINAL = {"finished", "cancelled", "expired"}
CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"


class ApiError(Exception):
    def __init__(self, status: int, message: str):
        self.status, self.message = status, message


def require(condition, message, status=400):
    if not condition:
        raise ApiError(status, message)


def public_name(value):
    require(isinstance(value, str), "Bir oyuncu adı yaz.")
    value = value.strip()
    require(1 <= len(value) <= 24 and not any(ord(c) < 32 for c in value),
            "Oyuncu adı 1–24 karakter olmalı.")
    return value


def integer(value, minimum, maximum):
    require(type(value) is int and minimum <= value <= maximum, "Geçersiz oyun hamlesi.")
    return value


class OnlineService:
    def __init__(self, database, catalog=CATALOG, clock=time.time):
        self.clock = clock
        self.lock = threading.RLock()
        self.rate = defaultdict(deque)
        self.levels = {(l["id"], l["adult"]): l for l in json.loads(Path(catalog).read_text(encoding="utf-8"))["levels"]}
        if str(database) != ":memory:":
            Path(database).parent.mkdir(parents=True, exist_ok=True)
        self.db = sqlite3.connect(str(database), check_same_thread=False)
        self.db.row_factory = sqlite3.Row
        self.db.execute("PRAGMA foreign_keys=ON")
        self.db.execute("PRAGMA journal_mode=WAL")
        self.db.executescript("""
            CREATE TABLE IF NOT EXISTS players (
                id TEXT PRIMARY KEY, credential TEXT UNIQUE NOT NULL,
                name TEXT NOT NULL, code TEXT UNIQUE NOT NULL, last_seen REAL NOT NULL);
            CREATE TABLE IF NOT EXISTS friends (
                sender TEXT NOT NULL REFERENCES players(id),
                recipient TEXT NOT NULL REFERENCES players(id), status TEXT NOT NULL,
                PRIMARY KEY(sender, recipient));
            CREATE TABLE IF NOT EXISTS rooms (
                id TEXT PRIMARY KEY, code TEXT UNIQUE NOT NULL, owner TEXT NOT NULL REFERENCES players(id),
                invited TEXT REFERENCES players(id), game TEXT NOT NULL, adult INTEGER NOT NULL,
                seed INTEGER NOT NULL, round TEXT NOT NULL, goal INTEGER NOT NULL,
                state TEXT NOT NULL, created REAL NOT NULL, starts REAL NOT NULL DEFAULT 0,
                deadline REAL NOT NULL DEFAULT 0, winner TEXT NOT NULL DEFAULT '');
            CREATE TABLE IF NOT EXISTS members (
                room TEXT NOT NULL REFERENCES rooms(id), player TEXT NOT NULL REFERENCES players(id),
                ready INTEGER NOT NULL DEFAULT 0, status TEXT NOT NULL DEFAULT 'joined',
                done INTEGER NOT NULL DEFAULT 0, mistakes INTEGER NOT NULL DEFAULT 0,
                event_seq INTEGER NOT NULL DEFAULT 0, accepted TEXT NOT NULL DEFAULT '[]',
                score INTEGER NOT NULL DEFAULT 0, elapsed REAL NOT NULL DEFAULT 0,
                last_seen REAL NOT NULL, acknowledged INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY(room, player));
            CREATE TABLE IF NOT EXISTS results (
                room TEXT NOT NULL REFERENCES rooms(id), player TEXT NOT NULL REFERENCES players(id),
                game TEXT NOT NULL, adult INTEGER NOT NULL, score INTEGER NOT NULL,
                elapsed REAL NOT NULL, outcome TEXT NOT NULL, finished REAL NOT NULL,
                PRIMARY KEY(room, player));
            CREATE INDEX IF NOT EXISTS member_player ON members(player);
            CREATE INDEX IF NOT EXISTS result_board ON results(adult, game, finished);
        """)
        self.db.commit()

    def code(self, table):
        while True:
            code = "".join(secrets.choice(CODE_ALPHABET) for _ in range(8))
            if self.db.execute(f"SELECT 1 FROM {table} WHERE code=?", (code,)).fetchone() is None:
                return code

    def player_view(self, player):
        p = self.db.execute("SELECT id,name,code,last_seen FROM players WHERE id=?", (player,)).fetchone()
        return {"id": p["id"], "name": p["name"], "code": p["code"], "online": self.clock() - p["last_seen"] < 25}

    def are_friends(self, a, b):
        return self.db.execute("SELECT 1 FROM friends WHERE status='accepted' AND ((sender=? AND recipient=?) OR (sender=? AND recipient=?))", (a,b,b,a)).fetchone() is not None

    def friends(self, player, status="accepted", direction=None):
        rows = self.db.execute("SELECT * FROM friends WHERE status=? AND (sender=? OR recipient=?)", (status,player,player))
        return [self.player_view(r["recipient"] if r["sender"] == player else r["sender"]) for r in rows
                if direction is None or r[direction] == player]

    def member(self, room, player):
        result = self.db.execute("SELECT * FROM members WHERE room=? AND player=?", (room,player)).fetchone()
        require(result is not None, "Bu odaya erişimin yok.", 403)
        return result

    def room(self, room):
        result = self.db.execute("SELECT * FROM rooms WHERE id=?", (room,)).fetchone()
        require(result is not None, "Oda bulunamadı.", 404)
        return result

    def active_room(self, player):
        return self.db.execute("""SELECT r.* FROM rooms r JOIN members m ON m.room=r.id
            WHERE m.player=? AND m.acknowledged=0 ORDER BY r.created DESC LIMIT 1""", (player,)).fetchone()

    def free_to_play(self, player):
        active = self.active_room(player)
        require(active is None or active["state"] in TERMINAL, "Önce mevcut yarış odasından ayrıl.", 409)
        if active:
            self.db.execute("UPDATE members SET acknowledged=1 WHERE room=? AND player=?", (active["id"],player))

    def room_view(self, room):
        r = self.room(room)
        players = []
        for member in self.db.execute("SELECT * FROM members WHERE room=? ORDER BY rowid", (room,)):
            p = self.player_view(member["player"])
            p.update(ready=bool(member["ready"]), status=member["status"], done=member["done"],
                     mistakes=member["mistakes"], score=member["score"], elapsed=member["elapsed"], eventSeq=member["event_seq"], accepted=json.loads(member["accepted"]))
            players.append(p)
        return {"id":r["id"], "code":r["code"], "ownerId":r["owner"], "gameId":r["game"],
                "adult":bool(r["adult"]), "seed":r["seed"], "round":json.loads(r["round"]), "goal":r["goal"],
                "state":r["state"], "startsAt":r["starts"], "deadline":r["deadline"], "winnerId":r["winner"], "players":players}

    def snapshot(self, player):
        active = self.active_room(player)
        invites = [{"roomId":r["id"], "code":r["code"], "gameId":r["game"], "adult":bool(r["adult"]),
                    "from":self.player_view(r["owner"])} for r in self.db.execute(
                        "SELECT * FROM rooms WHERE invited=? AND state='waiting' AND NOT EXISTS (SELECT 1 FROM members WHERE room=rooms.id AND player=?)",
                        (player,player))]
        return {"serverTime":self.clock(), "me":self.player_view(player), "friends":self.friends(player),
                "incoming":self.friends(player,"pending","recipient"), "outgoing":self.friends(player,"pending","sender"),
                "invites":invites, "room":self.room_view(active["id"]) if active else None}

    def make_round(self, definition, seed):
        r, rng = copy.deepcopy(definition), random.Random(seed)
        def pick(pool, count):
            return rng.sample(pool, min(len(pool), count))
        kind = r["type"]
        if kind == "bag":
            r["correctItems"] = pick(r["correctPool"], r["correctCount"])
            r["items"] = r["correctItems"] + pick(r["negativePool"], r["negativeCount"])
            goal = len(r["correctItems"])
        elif kind == "sort":
            r["packItems"] = pick(r["packPool"], r["packCount"])
            r["items"] = r["packItems"] + pick(r["negativePool"], r["negativeCount"])
            goal = len(r["items"])
        elif kind == "match":
            pairs = pick(r["pairPool"],r["matchCount"])
            r["items"] = [p["itemId"] for p in pairs]
            r["targets"] = [{"id":p["itemId"], "accepts":p["itemId"], "label":p["targetLabel"]} for p in pairs]
            goal = len(pairs)
        elif kind == "quiz":
            r["questions"] = pick(r["questions"], r["questionCount"] or len(r["questions"]))
            for q in r["questions"]:
                rng.shuffle(q["choices"])
            goal = len(r["questions"])
        elif kind == "danger":
            goal = sum(a["dangerous"] for a in r["actions"])
        elif kind == "sequence":
            goal = sum(len(q["steps"]) for q in r["rounds"])
        elif kind == "memory":
            r["sequence"] = pick(r["memoryPads"], r["sequenceLength"])
            goal = len(r["sequence"])
        elif kind == "catch":
            # Both clients use the same item stream and normalized horizontal positions.
            r["catchSpawns"] = []
            for index in range(334):
                good = rng.random() < .74
                r["catchSpawns"].append({"id":rng.choice(r["goodItems"] if good else r["badItems"]),
                                         "x":rng.random(), "good":good, "at":round(index * .9, 3)})
            goal = r["catchTarget"]
        else:
            raise ApiError(400, "Bu oyun çevrimiçi yarışı desteklemiyor.")
        return r, goal

    def finish_room(self, room):
        r = self.room(room)
        if r["state"] in TERMINAL:
            return
        members = list(self.db.execute("SELECT * FROM members WHERE room=?", (room,)))
        if len(members) != 2 or not all(m["status"] in {"finished","left"} for m in members):
            return
        finished = [m for m in members if m["status"] == "finished"]
        winner = ""
        if finished:
            best = sorted(finished, key=lambda m:(-m["score"],m["elapsed"]))
            if len(best) == 1 or best[0]["score"] != best[1]["score"] or abs(best[0]["elapsed"] - best[1]["elapsed"]) > .1:
                winner = best[0]["player"]
        self.db.execute("UPDATE rooms SET state='finished',winner=? WHERE id=?", (winner,room))
        for m in members:
            outcome = "win" if m["player"] == winner else "loss" if winner or m["status"] == "left" else "draw"
            self.db.execute("INSERT OR IGNORE INTO results VALUES (?,?,?,?,?,?,?,?)",
                            (room,m["player"],r["game"],r["adult"],m["score"] if m["status"] == "finished" else 0,m["elapsed"],outcome,self.clock()))

    def tick(self):
        now = self.clock()
        self.db.execute("UPDATE rooms SET state='expired' WHERE state='waiting' AND created<?", (now-300,))
        self.db.execute("UPDATE rooms SET state='playing' WHERE state='countdown' AND starts<=?", (now,))
        for r in list(self.db.execute("SELECT * FROM rooms WHERE state='playing'")):
            self.db.execute("""UPDATE members SET status='left',score=0,elapsed=?
                WHERE room=? AND status='joined' AND (last_seen<? OR ? >= ?)""",
                (max(0,now-r["starts"]),r["id"],now-60,now,r["deadline"]))
            self.finish_room(r["id"])

    def apply_action(self, r, m, event):
        kind, data = json.loads(r["round"])["type"], json.loads(r["round"])
        accepted = set(json.loads(m["accepted"]))
        done, mistakes = m["done"], m["mistakes"]
        key, target, index = event.get("key",""), event.get("target",""), event.get("index",0)
        require(isinstance(key,str) and len(key)<=80 and isinstance(target,str) and len(target)<=80, "Geçersiz oyun hamlesi.")
        correct, identity = False, None
        if kind in {"bag","sort","match"}:
            require(key in data["items"], "Bu eşya oyunda yok.")
            identity = key
            if identity in accepted:
                return done,mistakes,accepted
            if kind == "bag":
                correct = key in data["correctItems"]
            elif kind == "sort":
                require(target in {"pack","leave"}, "Bir alan seç.")
                correct = (key in data["packItems"]) == (target == "pack")
            else:
                destination = next((t for t in data["targets"] if t["id"] == target),None)
                require(destination is not None, "Eşleştirme alanı bulunamadı.")
                correct = destination["accepts"] == key
        elif kind == "quiz":
            q = integer(index,0,len(data["questions"])-1)
            require(q == done, "Sıradaki soruyu yanıtla.", 409)
            require(key.isdigit(), "Bir yanıt seç.")
            choice = integer(int(key),0,len(data["questions"][q]["choices"])-1)
            correct = data["questions"][q]["choices"][choice]["correct"]
        elif kind == "danger":
            action = next((a for a in data["actions"] if a["id"] == key),None)
            require(action is not None, "Bu davranış oyunda yok.")
            identity = key
            if identity in accepted:
                return done,mistakes,accepted
            correct = action["dangerous"]
        elif kind == "sequence":
            steps = [s for q in data["rounds"] for s in q["steps"]]
            require(any(s["id"] == key for s in steps), "Bu adım oyunda yok.")
            correct = steps[done]["id"] == key
        elif kind == "memory":
            if key == "replay":
                return 0,mistakes,accepted
            require(key in data["memoryPads"], "Bu eşya oyunda yok.")
            correct = data["sequence"][done] == key
            if not correct:
                return 0,mistakes+1,accepted
        elif kind == "catch":
            i = integer(index,0,len(data["catchSpawns"])-1)
            spawn = data["catchSpawns"][i]
            require(key in {"catch","miss"}, "Geçersiz yakalama hamlesi.")
            require(self.clock() - r["starts"] >= spawn["at"] + .5, "Eşya henüz çantaya ulaşmadı.")
            identity = str(i)
            if identity in accepted:
                return done,mistakes,accepted
            accepted.add(identity)
            if key == "miss" and not spawn["good"]:
                return done,mistakes,accepted
            correct = key == "catch" and spawn["good"]
        if correct:
            done += 1
            if identity is not None:
                accepted.add(identity)
        else:
            mistakes += 1
        return done,mistakes,accepted

    def leaderboard(self, player, query):
        adult = query.get("adult",["false"])[0]
        scope, period, game = (query.get(k,[default])[0] for k,default in [("scope","global"),("period","all"),("game","")])
        require(adult in {"true","false"} and scope in {"global","friends"} and period in {"all","week"}, "Geçersiz sıralama filtresi.")
        require(not game or (game,adult=="true") in self.levels, "Oyun bulunamadı.")
        params = [int(adult == "true"), self.clock()-7*86400 if period == "week" else 0]
        where = "adult=? AND finished>=? AND score>0"
        if game:
            where += " AND game=?"
            params.append(game)
        if scope == "friends":
            ids = [player] + [p["id"] for p in self.friends(player)]
            where += " AND player IN (" + ",".join("?" for _ in ids) + ")"
            params += ids
        rows = self.db.execute(f"""WITH filtered AS (SELECT * FROM results WHERE {where}),
            best AS (SELECT player,game,MAX(score) AS score FROM filtered GROUP BY player,game),
            points AS (SELECT player,SUM(score) AS score FROM best GROUP BY player),
            stats AS (SELECT player,SUM(outcome='win') AS wins,COUNT(*) AS played FROM filtered GROUP BY player)
            SELECT p.player,p.score,s.wins,s.played FROM points p JOIN stats s ON s.player=p.player
            ORDER BY p.score DESC,s.wins DESC,p.player""", params).fetchall()
        entries = []
        me = None
        for rank,row in enumerate(rows,1):
            entry = self.player_view(row["player"])
            entry.update(rank=rank,score=row["score"],wins=row["wins"],played=row["played"])
            if row["player"] == player:
                me = entry
            if rank <= 50:
                entries.append(entry)
        return {"entries":entries, "self":me, "serverTime":self.clock()}

    def dispatch(self, method, path, payload=None, credential="", query=None, ip="local"):
        payload, query = payload or {}, query or {}
        with self.lock, self.db:
            now = self.clock()
            if method == "GET" and path == "/health":
                return {"ok":True, "service":"deprem-online", "version":1, "serverTime":now}
            require(re.fullmatch(r"[a-f0-9]{64}",credential) is not None, "Çevrimiçi hesaba bağlan.", 401)
            digest = hashlib.sha256(credential.encode()).hexdigest()
            bucket = self.rate[digest]
            while bucket and bucket[0] < now-60:
                bucket.popleft()
            require(len(bucket)<240, "Çok fazla istek. Biraz sonra tekrar dene.", 429)
            bucket.append(now)
            # Bound the rate limiter without retaining secrets or unbounded idle entries.
            if len(self.rate)>10000:
                for k in list(self.rate):
                    if not self.rate[k] or self.rate[k][-1]<now-60:
                        del self.rate[k]
            p = self.db.execute("SELECT * FROM players WHERE credential=?", (digest,)).fetchone()
            if method == "POST" and path == "/v1/session":
                name = public_name(payload.get("name"))
                if p is None:
                    recent = self.rate["register:"+ip]
                    while recent and recent[0]<now-3600:
                        recent.popleft()
                    require(len(recent)<40, "Yeni hesap sınırına ulaşıldı. Daha sonra tekrar dene.", 429)
                    recent.append(now)
                    player = secrets.token_hex(16)
                    self.db.execute("INSERT INTO players VALUES (?,?,?,?,?)", (player,digest,name,self.code("players"),now))
                else:
                    player = p["id"]
                    self.db.execute("UPDATE players SET name=?,last_seen=? WHERE id=?", (name,now,player))
            else:
                require(p is not None, "Önce çevrimiçi hesabına bağlan.", 401)
                player = p["id"]
                self.db.execute("UPDATE players SET last_seen=? WHERE id=?", (now,player))
            # Heartbeat before expiry gives a returning client its 60-second reconnect window.
            self.db.execute("UPDATE members SET last_seen=? WHERE player=? AND status='joined'", (now,player))
            self.tick()
            if (method,path) in {("GET","/v1/state"),("POST","/v1/session")}:
                return self.snapshot(player)
            if method == "GET" and path == "/v1/leaderboard":
                return self.leaderboard(player,query)
            if method == "POST" and path == "/v1/friends/request":
                code = str(payload.get("code","")).strip().upper().replace("-","")
                other = self.db.execute("SELECT id FROM players WHERE code=?", (code,)).fetchone()
                require(other is not None, "Bu arkadaş kodu bulunamadı.", 404)
                other = other["id"]
                require(other != player, "Kendine arkadaşlık isteği gönderemezsin.")
                if not self.are_friends(player,other):
                    reverse = self.db.execute("SELECT 1 FROM friends WHERE sender=? AND recipient=?",(other,player)).fetchone()
                    if reverse:
                        self.db.execute("UPDATE friends SET status='accepted' WHERE sender=? AND recipient=?", (other,player))
                    else:
                        require(len(self.friends(player))+len(self.friends(player,"pending"))<100, "Arkadaş listesi dolu.")
                        self.db.execute("INSERT OR IGNORE INTO friends VALUES (?,?,'pending')",(player,other))
                return self.snapshot(player)
            if method == "POST" and path in {"/v1/friends/respond","/v1/friends/remove"}:
                other = payload.get("playerId","")
                require(isinstance(other,str), "Geçersiz arkadaş.")
                if path.endswith("respond"):
                    require(type(payload.get("accept")) is bool, "İsteği kabul et veya reddet.")
                    exists = self.db.execute("SELECT 1 FROM friends WHERE sender=? AND recipient=? AND status='pending'", (other,player)).fetchone()
                    require(exists is not None, "Arkadaşlık isteği artık beklemiyor.", 409)
                    if payload["accept"]:
                        self.db.execute("UPDATE friends SET status='accepted' WHERE sender=? AND recipient=?", (other,player))
                    else:
                        self.db.execute("DELETE FROM friends WHERE sender=? AND recipient=?", (other,player))
                else:
                    self.db.execute("DELETE FROM friends WHERE (sender=? AND recipient=?) OR (sender=? AND recipient=?)", (other,player,player,other))
                return self.snapshot(player)
            if method == "POST" and path == "/v1/rooms":
                game,adult,invited = payload.get("gameId"),payload.get("adult"),payload.get("friendId") or None
                require(isinstance(game,str) and type(adult) is bool and (game,adult) in self.levels, "Oyun bulunamadı.")
                if invited:
                    require(isinstance(invited,str) and self.are_friends(player,invited), "Önce bu kişiyi arkadaş olarak ekle.", 403)
                    busy = self.active_room(invited)
                    require(busy is None or busy["state"] in TERMINAL, "Arkadaşın şu anda başka bir yarışta.", 409)
                request_id = payload.get("requestId","")
                require(re.fullmatch(r"[a-f0-9]{32}",request_id) is not None, "Geçersiz oda isteği.")
                existing = self.db.execute("SELECT * FROM rooms WHERE id=?", (request_id,)).fetchone()
                if existing:
                    require(existing["owner"] == player, "Bu odaya erişimin yok.", 403)
                    return self.snapshot(player)
                self.free_to_play(player)
                seed = secrets.randbelow(2**31-1)+1
                round_data,goal = self.make_round(self.levels[(game,adult)],seed)
                self.db.execute("INSERT INTO rooms (id,code,owner,invited,game,adult,seed,round,goal,state,created) VALUES (?,?,?,?,?,?,?,?,?,'waiting',?)",
                                (request_id,self.code("rooms"),player,invited,game,int(adult),seed,json.dumps(round_data,ensure_ascii=False),goal,now))
                self.db.execute("INSERT INTO members (room,player,last_seen) VALUES (?,?,?)", (request_id,player,now))
                return self.snapshot(player)
            if method == "POST" and path == "/v1/rooms/join":
                code = str(payload.get("code","")).strip().upper().replace("-","")
                room = self.db.execute("SELECT * FROM rooms WHERE code=?", (code,)).fetchone()
                require(room is not None, "Oda kodu bulunamadı.", 404)
                require(room["state"] == "waiting", "Bu oda artık katılıma açık değil.", 409)
                require(room["invited"] in {None,player} or room["owner"] == player, "Bu oda başka bir arkadaşa ayrılmış.", 403)
                if self.db.execute("SELECT 1 FROM members WHERE room=? AND player=?", (room["id"],player)).fetchone() is None:
                    self.free_to_play(player)
                    require(self.db.execute("SELECT COUNT(*) FROM members WHERE room=?",(room["id"],)).fetchone()[0]<2,"Oda dolu.",409)
                    self.db.execute("INSERT INTO members (room,player,last_seen) VALUES (?,?,?)", (room["id"],player,now))
                return self.snapshot(player)
            match = re.fullmatch(r"/v1/rooms/([a-f0-9]{32})/(ready|leave|ack|decline|actions)",path)
            if method == "POST" and match:
                room,operation = match.groups()
                r = self.room(room)
                if operation == "decline":
                    require(r["invited"] == player and r["state"] == "waiting", "Bu davet artık beklemiyor.", 409)
                    self.db.execute("UPDATE rooms SET state='cancelled' WHERE id=?",(room,))
                    return self.snapshot(player)
                m = self.member(room,player)
                if operation == "ack":
                    require(r["state"] in TERMINAL, "Yarış hâlâ sürüyor.", 409)
                    self.db.execute("UPDATE members SET acknowledged=1 WHERE room=? AND player=?", (room,player))
                elif operation == "ready":
                    require(r["state"] == "waiting", "Yarış zaten başladı.", 409)
                    self.db.execute("UPDATE members SET ready=1 WHERE room=? AND player=?", (room,player))
                    if self.db.execute("SELECT COUNT(*) FROM members WHERE room=? AND ready=1", (room,)).fetchone()[0]==2:
                        self.db.execute("UPDATE rooms SET state='countdown',starts=?,deadline=? WHERE id=?", (now+5,now+305,room))
                elif operation == "leave":
                    if r["state"] in {"waiting","countdown"}:
                        self.db.execute("UPDATE rooms SET state='cancelled' WHERE id=?", (room,))
                    elif r["state"] == "playing" and m["status"] == "joined":
                        self.db.execute("UPDATE members SET status='left',score=0,elapsed=? WHERE room=? AND player=?", (now-r["starts"],room,player))
                        self.finish_room(room)
                    self.db.execute("UPDATE members SET acknowledged=1 WHERE room=? AND player=?", (room,player))
                elif operation == "actions":
                    events = payload.get("events")
                    require(isinstance(events,list) and 1<=len(events)<=32 and all(isinstance(e,dict) for e in events), "Geçersiz oyun hamleleri.")
                    # An already acknowledged batch is harmless, even after match completion.
                    if not all(type(e.get("seq")) is int and e["seq"] <= m["event_seq"] for e in events):
                        require(r["state"] == "playing" and m["status"] == "joined", "Bu yarışta hamle yapılamaz.", 409)
                        for event in events:
                            seq = integer(event.get("seq"),1,10000)
                            if seq <= m["event_seq"]:
                                continue
                            require(seq == m["event_seq"]+1, "Hamle sırası uyuşmuyor. Yeniden bağlan.", 409)
                            done,mistakes,accepted = self.apply_action(r,m,event)
                            completed = done == r["goal"]
                            score = max(100,1000-mistakes*75) if completed else max(0,round(1000*done/r["goal"])-mistakes*75)
                            self.db.execute("""UPDATE members SET done=?,mistakes=?,accepted=?,event_seq=?,score=?,status=?,elapsed=?
                                WHERE room=? AND player=?""", (done,mistakes,json.dumps(sorted(accepted)),seq,score,"finished" if completed else "joined",round(now-r["starts"],3),room,player))
                            m = self.member(room,player)
                            if completed:
                                require(event is events[-1], "Oyun zaten tamamlandı.")
                        self.finish_room(room)
                return self.snapshot(player)
            raise ApiError(404,"İstek bulunamadı.")


class WsgiApplication:
    def __init__(self, service):
        self.service = service

    def __call__(self, environ, start_response):
        status = 200
        try:
            length = int(environ.get("CONTENT_LENGTH") or 0)
            require(0<=length<=16384,"İstek çok büyük.",413)
            payload = json.loads(environ["wsgi.input"].read(length) or b"{}")
            require(isinstance(payload,dict),"Geçersiz istek.")
            auth = environ.get("HTTP_AUTHORIZATION","")
            data = self.service.dispatch(environ["REQUEST_METHOD"],environ.get("PATH_INFO",""),payload,
                                         auth[7:] if auth.startswith("Bearer ") else "", parse_qs(environ.get("QUERY_STRING","")),environ.get("REMOTE_ADDR","local"))
        except ApiError as error:
            status, data = error.status,{"error":error.message}
        except (ValueError, UnicodeError):
            status,data = 400,{"error":"Geçersiz JSON isteği."}
        except Exception:
            logging.exception("Online API failed")
            status,data = 500,{"error":"Sunucu isteği tamamlayamadı. Tekrar dene."}
        body = json.dumps(data,ensure_ascii=False,allow_nan=False).encode("utf-8")
        reason = {200:"OK",400:"Bad Request",401:"Unauthorized",403:"Forbidden",404:"Not Found",409:"Conflict",413:"Payload Too Large",429:"Too Many Requests",500:"Internal Server Error"}[status]
        start_response(f"{status} {reason}",[("Content-Type","application/json; charset=utf-8"),("Content-Length",str(len(body))),("Cache-Control","no-store"),("X-Content-Type-Options","nosniff")])
        return [body]


_application = None
_application_lock = threading.Lock()
def application(environ,start_response):
    global _application
    if _application is None:
        with _application_lock:
            if _application is None:
                _application = WsgiApplication(OnlineService(os.environ.get("DEPREM_ONLINE_DB",str(Path(__file__).parent / "data/online.sqlite3")),os.environ.get("DEPREM_ONLINE_CATALOG",str(CATALOG))))
    return _application(environ,start_response)


class ThreadedServer(ThreadingMixIn,WSGIServer):
    daemon_threads = True


class QuietHandler(WSGIRequestHandler):
    def log_message(self,format,*args):
        # Access logs omit player codes, credentials and query strings.
        pass


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host",default="127.0.0.1")
    parser.add_argument("--port",type=int,default=8765)
    parser.add_argument("--db",default=str(Path(__file__).parent / "data/online.sqlite3"))
    args = parser.parse_args()
    app = WsgiApplication(OnlineService(args.db))
    with make_server(args.host,args.port,app,ThreadedServer,QuietHandler) as server:
        print(f"Deprem Online running on {args.host}:{args.port}",flush=True)
        server.serve_forever()


if __name__ == "__main__":
    main()
