import io
import json
from pathlib import Path
import secrets
import tempfile
import threading
import unittest
from concurrent.futures import ThreadPoolExecutor
from contextlib import closing
from urllib.error import HTTPError
from urllib.request import Request, urlopen
from wsgiref.simple_server import make_server

from server import CODE_TTL, SESSION_TTL, FamilyApplication, QuietHandler, ThreadedServer, digest


class FamilyApiTests(unittest.TestCase):
    def setUp(self):
        self.folder = tempfile.TemporaryDirectory()
        self.now = [1_791_158_400]
        self.app = FamilyApplication(Path(self.folder.name) / "family.sqlite3", clock=lambda: self.now[0])

    def tearDown(self):
        self.folder.cleanup()

    def request(self, method, path, data=None, token="", app=None):
        payload = json.dumps(data or {}).encode()
        status = []
        env = {"REQUEST_METHOD": method, "PATH_INFO": path, "CONTENT_TYPE": "application/json",
               "CONTENT_LENGTH": str(len(payload)), "wsgi.input": io.BytesIO(payload), "REMOTE_ADDR": "127.0.0.1",
               "HTTP_AUTHORIZATION": "Bearer " + token}
        output = (app or self.app)(env, lambda code, headers: status.append(int(code.split()[0])))
        return status[0], json.loads(b"".join(output))

    def parent(self, email="veli@example.test", name="Veli"):
        status, data = self.request("POST", "/v1/parent/register", {"email": email, "name": name, "password": "Birlikte2026!"})
        self.assertEqual(status, 200, data)
        return data["token"]

    @staticmethod
    def progress(name="Ada", completed=None):
        return {"progress": {"name": name, "completed": completed or ["child-0"],
                             "results": [{"id": "child:bag-packing", "stars": 3, "score": 900}],
                             "reviewTopics": [{"courseId": "child-0", "count": 2}],
                             "lastActivity": "2026-10-05", "streak": 2, "longestStreak": 3,
                             "todayActivities": 2, "xp": 9999, "resumeCourse": "child-1", "resumeExercise": 1}}

    def child(self, name="Ada"):
        token = "d_" + secrets.token_urlsafe(32)
        status, data = self.request("PUT", "/v1/child/progress", self.progress(name), token)
        self.assertEqual(status, 200, data)
        return token, data["id"]

    def code(self, token):
        status, data = self.request("POST", "/v1/child/pair-code", {}, token)
        self.assertEqual(status, 200, data)
        return data["code"]

    def link(self, parent, child):
        status, data = self.request("POST", "/v1/parent/links", {"code": self.code(child)}, parent)
        self.assertEqual(status, 200, data)

    def test_registration_hashes_credentials_and_login_is_case_insensitive(self):
        token = self.parent(email="Veli@Example.Test")
        with closing(self.app.connect()) as conn:
            row = conn.execute("SELECT * FROM parents").fetchone()
            self.assertEqual(row["email"], "veli@example.test")
            self.assertNotEqual(row["password_hash"], b"Birlikte2026!")
            self.assertEqual(len(row["password_salt"]), 16)
            saved = conn.execute("SELECT token_hash FROM sessions").fetchone()[0]
            self.assertNotEqual(saved, token)
        status, data = self.request("POST", "/v1/parent/login", {"email": "VELI@example.test", "password": "Birlikte2026!"})
        self.assertEqual(status, 200)
        self.assertNotEqual(data["token"], token)
        status, _ = self.request("POST", "/v1/parent/login", {"email": "veli@example.test", "password": "yanlisparola"})
        self.assertEqual(status, 401)

    def test_link_sync_and_restart_keep_each_child_separate(self):
        parent = self.parent()
        ada, ada_id = self.child()
        can, can_id = self.child("Can")
        self.link(parent, ada)
        self.link(parent, can)
        status, _ = self.request("PUT", "/v1/child/progress", self.progress("Ada", ["child-0", "child-1"]), ada)
        self.assertEqual(status, 200)
        restarted = FamilyApplication(self.app.database, clock=lambda: self.now[0])
        status, data = self.request("GET", "/v1/parent/children", token=parent, app=restarted)
        self.assertEqual(status, 200)
        children = {c["id"]: c for c in data["children"]}
        self.assertEqual(children[ada_id]["progress"]["xp"], 80)
        self.assertEqual(children[can_id]["progress"]["xp"], 50)
        self.assertEqual(len(children[can_id]["progress"]["completed"]), 1)
        self.assertNotIn("token", json.dumps(data))
        self.assertNotIn("email", json.dumps(data))

    def test_other_parent_and_child_cannot_access_or_write_another_child(self):
        parent = self.parent()
        stranger = self.parent("baska@example.test")
        child, child_id = self.child()
        other, _ = self.child("Can")
        self.link(parent, child)
        _, data = self.request("GET", "/v1/parent/children", token=stranger)
        self.assertEqual(data["children"], [])
        status, _ = self.request("DELETE", "/v1/parent/children/" + child_id, token=stranger)
        self.assertEqual(status, 404)
        for method, path, token, body in [
            ("GET", "/v1/parent/children", child, {}),
            ("GET", "/v1/parent/children", "", {}),
            ("PUT", "/v1/child/progress", parent, self.progress()),
            ("POST", "/v1/child/pair-code", parent, {}),
        ]:
            status, _ = self.request(method, path, body, token)
            self.assertEqual(status, 401, path)
        _, guardians = self.request("GET", "/v1/child/guardians", token=other)
        self.assertEqual(guardians["guardians"], [])

    def test_pair_codes_expire_are_single_use_and_rotation_invalidates_previous(self):
        parent = self.parent()
        child, _ = self.child()
        old = self.code(child)
        new = self.code(child)
        status, _ = self.request("POST", "/v1/parent/links", {"code": old}, parent)
        self.assertEqual(status, 400)
        status, _ = self.request("POST", "/v1/parent/links", {"code": new[:4].lower() + "-" + new[4:].lower()}, parent)
        self.assertEqual(status, 200)
        status, _ = self.request("POST", "/v1/parent/links", {"code": new}, parent)
        self.assertEqual(status, 400)
        expired = self.code(child)
        self.now[0] += CODE_TTL
        status, _ = self.request("POST", "/v1/parent/links", {"code": expired}, parent)
        self.assertEqual(status, 400)

    def test_failed_code_guesses_are_rate_limited(self):
        parent = self.parent()
        for _ in range(5):
            status, _ = self.request("POST", "/v1/parent/links", {"code": "ABCDEFGH"}, parent)
            self.assertEqual(status, 400)
        status, _ = self.request("POST", "/v1/parent/links", {"code": "ABCDEFGH"}, parent)
        self.assertEqual(status, 429)
        self.now[0] += 60
        status, _ = self.request("POST", "/v1/parent/links", {"code": "ABCDEFGH"}, parent)
        self.assertEqual(status, 400)

    def test_failed_login_counters_survive_errors(self):
        self.parent()
        for _ in range(9):
            status, _ = self.request("POST", "/v1/parent/login", {"email": "veli@example.test", "password": "yanlisparola"})
            self.assertEqual(status, 401)
        status, _ = self.request("POST", "/v1/parent/login", {"email": "veli@example.test", "password": "Birlikte2026!"})
        self.assertEqual(status, 429)

    def test_child_can_revoke_one_guardian_or_stop_all_sharing(self):
        parent = self.parent()
        second = self.parent("anne@example.test", "Anne")
        child, _ = self.child()
        self.link(parent, child)
        self.link(second, child)
        _, guardians = self.request("GET", "/v1/child/guardians", token=child)
        self.assertEqual(len(guardians["guardians"]), 2)
        self.assertNotIn("email", json.dumps(guardians))
        parent_id = next(p["id"] for p in guardians["guardians"] if p["name"] == "Veli")
        status, _ = self.request("DELETE", "/v1/child/guardians/" + parent_id, token=child)
        self.assertEqual(status, 200)
        _, data = self.request("GET", "/v1/parent/children", token=parent)
        self.assertEqual(data["children"], [])
        unused = self.code(child)
        status, _ = self.request("DELETE", "/v1/child", token=child)
        self.assertEqual(status, 200)
        _, data = self.request("GET", "/v1/parent/children", token=second)
        self.assertEqual(data["children"], [])
        status, _ = self.request("PUT", "/v1/child/progress", self.progress(), child)
        self.assertEqual(status, 410, "A stopped device must never silently re-enroll")
        status, _ = self.request("POST", "/v1/parent/links", {"code": unused}, second)
        self.assertEqual(status, 400)
        self.assertEqual(self.request("DELETE", "/v1/child", token=child)[0], 200)
        with closing(self.app.connect()) as conn:
            self.assertEqual(conn.execute("SELECT snapshot FROM children").fetchone()[0], "{}")

    def test_parent_unlink_and_account_deletion_revoke_access(self):
        parent = self.parent()
        child, child_id = self.child()
        self.link(parent, child)
        status, _ = self.request("DELETE", "/v1/parent/children/" + child_id, token=parent)
        self.assertEqual(status, 200)
        self.link(parent, child)
        status, _ = self.request("DELETE", "/v1/parent/account", token=parent)
        self.assertEqual(status, 200)
        self.assertEqual(self.request("GET", "/v1/parent/children", token=parent)[0], 401)
        _, data = self.request("GET", "/v1/child/guardians", token=child)
        self.assertEqual(data["guardians"], [])

    def test_logout_and_expiry_invalidate_sessions(self):
        parent = self.parent()
        status, _ = self.request("POST", "/v1/parent/logout", {}, parent)
        self.assertEqual(status, 200)
        self.assertEqual(self.request("GET", "/v1/parent/children", token=parent)[0], 401)
        new = self.parent("anne@example.test")
        self.now[0] += SESSION_TTL
        self.assertEqual(self.request("GET", "/v1/parent/children", token=new)[0], 401)

    def test_private_fields_adult_progress_and_invalid_payloads_are_rejected(self):
        child, _ = self.child()
        for patch in [{"plan": [{"key": "address", "value": "private"}]},
                      {"mistakes": [{"question": "private"}]}, {"completed": ["adult-0"]},
                      {"results": [{"id": "adult:bag-packing", "stars": 3, "score": 100}]},
                      {"streak": -1}, {"streak": True}, {"lastActivity": "yesterday"},
                      {"reviewTopics": [{"courseId": "adult-0", "count": 2}]}]:
            data = self.progress()
            data["progress"].update(patch)
            status, _ = self.request("PUT", "/v1/child/progress", data, child)
            self.assertEqual(status, 400, patch)
        self.assertEqual(self.request("PUT", "/v1/child/progress", {"progress": "invalid"}, child)[0], 400)
        self.assertEqual(self.request("POST", "/v1/parent/register", {"email": "x", "password": "short", "name": "Veli"})[0], 400)

    def test_device_retry_is_idempotent_and_code_is_stored_only_as_hash(self):
        child, child_id = self.child()
        status, data = self.request("PUT", "/v1/child/progress", self.progress(), child)
        self.assertEqual(status, 200)
        self.assertEqual(data["id"], child_id)
        code = self.code(child)
        with closing(self.app.connect()) as conn:
            self.assertEqual(conn.execute("SELECT COUNT(*) FROM children").fetchone()[0], 1)
            saved = conn.execute("SELECT code_hash FROM pair_codes").fetchone()[0]
            self.assertEqual(saved, digest(code))
            self.assertNotEqual(saved, code)

    def test_real_http_redeems_code_atomically_for_concurrent_parents(self):
        parent = self.parent()
        second = self.parent("anne@example.test")
        child, _ = self.child()
        code = self.code(child)
        with make_server("127.0.0.1", 0, self.app, server_class=ThreadedServer, handler_class=QuietHandler) as server:
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            url = f"http://127.0.0.1:{server.server_port}/v1/parent/links"

            def redeem(token):
                request = Request(url, json.dumps({"code": code}).encode(), headers={
                    "Content-Type": "application/json", "Authorization": "Bearer " + token}, method="POST")
                try:
                    with urlopen(request, timeout=10) as response:
                        return response.status
                except HTTPError as error:
                    return error.code

            try:
                with ThreadPoolExecutor(max_workers=2) as pool:
                    statuses = list(pool.map(redeem, [parent, second]))
                self.assertEqual(sorted(statuses), [200, 400])
            finally:
                server.shutdown()
                thread.join(timeout=5)


if __name__ == "__main__":
    unittest.main(verbosity=2)
