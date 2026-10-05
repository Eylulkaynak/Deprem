"""Family linking API. No child's address, preparation plan or answers are accepted.

Run locally: python Services/Family/server.py --dev
Deploy with Waitress behind HTTPS; see Docs/Learning_App_Family.md.
"""
from __future__ import annotations

import argparse
import hashlib
import hmac
import json
import logging
import os
from pathlib import Path
import re
import secrets
import sqlite3
import time
from contextlib import closing
from datetime import date, datetime, timezone
from http import HTTPStatus
from socketserver import ThreadingMixIn
from wsgiref.simple_server import WSGIRequestHandler, WSGIServer, make_server

PASSWORD_ITERATIONS = 600_000
CODE_TTL = 600
SESSION_TTL = 12 * 60 * 60
MAX_BODY = 32_768
CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"
COURSES = {f"child-{i}" for i in range(21)}
GAMES = {"child:" + name for name in (
    "bag-packing", "category-sorting", "match-pairs", "safe-choice",
    "danger-hunt", "catch-bag", "safe-order", "memory-bag",
)}
SCHEMA = """
CREATE TABLE IF NOT EXISTS parents (
    id TEXT PRIMARY KEY, email TEXT UNIQUE NOT NULL, name TEXT NOT NULL,
    password_salt BLOB NOT NULL, password_hash BLOB NOT NULL
);
CREATE TABLE IF NOT EXISTS sessions (
    token_hash TEXT PRIMARY KEY, parent_id TEXT NOT NULL REFERENCES parents(id) ON DELETE CASCADE,
    expires_at INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS children (
    id TEXT PRIMARY KEY, token_hash TEXT UNIQUE NOT NULL,
    snapshot TEXT NOT NULL, updated_at INTEGER NOT NULL, revoked INTEGER NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS pair_codes (
    code_hash TEXT PRIMARY KEY, child_id TEXT UNIQUE NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    expires_at INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS links (
    parent_id TEXT NOT NULL REFERENCES parents(id) ON DELETE CASCADE,
    child_id TEXT NOT NULL REFERENCES children(id) ON DELETE CASCADE,
    linked_at INTEGER NOT NULL, PRIMARY KEY(parent_id, child_id)
);
CREATE TABLE IF NOT EXISTS rate_limits (
    key TEXT PRIMARY KEY, window_start INTEGER NOT NULL, attempts INTEGER NOT NULL
);
PRAGMA user_version = 1;
"""


class ApiError(Exception):
    def __init__(self, status, code, message):
        self.status, self.code, self.message = status, code, message


def digest(value):
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def stamp(seconds):
    return datetime.fromtimestamp(seconds, timezone.utc).isoformat().replace("+00:00", "Z")


def text_field(body, key, minimum=1, maximum=128):
    value = body.get(key)
    if not isinstance(value, str) or not minimum <= len(value.strip()) <= maximum:
        raise ApiError(400, "invalid_input", f"{key} alanını kontrol edin.")
    # Passwords are validated separately and are never trimmed.
    value = value.strip()
    if any(ord(c) < 32 for c in value):
        raise ApiError(400, "invalid_input", "Geçersiz metin.")
    return value


def password_field(body, key="password"):
    password = body.get(key)
    if not isinstance(password, str) or not 10 <= len(password) <= 128 or len(password.encode("utf-8")) > 512:
        raise ApiError(400, "invalid_password", "Parola 10–128 karakter olmalı.")
    return password


def email_field(body):
    email = text_field(body, "email", 3, 254).casefold()
    if not re.fullmatch(r"[^\s@]+@[^\s@]+\.[^\s@]+", email):
        raise ApiError(400, "invalid_email", "Geçerli bir e-posta adresi girin.")
    return email


def int_field(body, key, maximum=1_000_000):
    value = body.get(key, 0)
    if type(value) is not int or not 0 <= value <= maximum:
        raise ApiError(400, "invalid_progress", "İlerleme verisi geçersiz.")
    return value


def validate_snapshot(body):
    data = body.get("progress")
    allowed = {"name", "completed", "results", "reviewTopics", "xp", "streak", "longestStreak",
               "todayActivities", "lastActivity", "resumeCourse", "resumeExercise"}
    if not isinstance(data, dict) or set(data) - allowed or set(body) != {"progress"}:
        raise ApiError(400, "invalid_progress", "Yalnızca eğitim ilerlemesi paylaşılabilir.")
    name = text_field(data, "name", 1, 24)
    completed = data.get("completed", [])
    if not isinstance(completed, list) or len(completed) > 21 or any(not isinstance(c, str) or c not in COURSES for c in completed):
        raise ApiError(400, "invalid_progress", "Ders listesi geçersiz.")
    results = data.get("results", [])
    if not isinstance(results, list) or len(results) > 8:
        raise ApiError(400, "invalid_progress", "Oyun listesi geçersiz.")
    games = {}
    for item in results:
        if not isinstance(item, dict) or set(item) - {"id", "stars", "score"} or not isinstance(item.get("id"), str) or item["id"] not in GAMES:
            raise ApiError(400, "invalid_progress", "Oyun sonucu geçersiz.")
        stars = int_field(item, "stars", 3)
        if stars == 0 or item["id"] in games:
            raise ApiError(400, "invalid_progress", "Oyun yıldızları geçersiz.")
        games[item["id"]] = {"id": item["id"], "stars": stars, "score": int_field(item, "score")}
    topics = data.get("reviewTopics", [])
    if not isinstance(topics, list) or len(topics) > 21:
        raise ApiError(400, "invalid_progress", "Tekrar konuları geçersiz.")
    reviews = {}
    for item in topics:
        if not isinstance(item, dict) or set(item) - {"courseId", "count"} or not isinstance(item.get("courseId"), str) or item["courseId"] not in COURSES:
            raise ApiError(400, "invalid_progress", "Tekrar konusu geçersiz.")
        if item["courseId"] in reviews:
            raise ApiError(400, "invalid_progress", "Tekrar konusu yinelenmiş.")
        reviews[item["courseId"]] = {"courseId": item["courseId"], "count": int_field(item, "count")}
    last = data.get("lastActivity", "")
    try:
        if not isinstance(last, str) or (last and date.fromisoformat(last).isoformat() != last):
            raise ValueError()
    except ValueError:
        raise ApiError(400, "invalid_progress", "Etkinlik tarihi geçersiz.") from None
    resume = data.get("resumeCourse", "")
    if not isinstance(resume, str) or (resume and resume not in COURSES):
        raise ApiError(400, "invalid_progress", "Devam edilen ders geçersiz.")
    # Derive XP from the accepted child achievements, never from adult results.
    return {"name": name, "completed": sorted(set(completed)), "results": list(games.values()),
            "reviewTopics": list(reviews.values()), "xp": len(set(completed)) * 30 + len(games) * 20,
            "streak": int_field(data, "streak", 10_000), "longestStreak": int_field(data, "longestStreak", 10_000),
            "todayActivities": int_field(data, "todayActivities", 10_000), "lastActivity": last,
            "resumeCourse": resume, "resumeExercise": int_field(data, "resumeExercise", 100)}


class FamilyApplication:
    def __init__(self, database, clock=time.time):
        self.database, self.clock = str(database), clock
        Path(self.database).parent.mkdir(parents=True, exist_ok=True)
        with closing(self.connect()) as conn:
            version = conn.execute("PRAGMA user_version").fetchone()[0]
            if version not in (0, 1):
                raise RuntimeError("Unsupported family database version")
            conn.execute("PRAGMA journal_mode = WAL")
            conn.executescript(SCHEMA)
        # Equal work for unknown accounts; this value never authenticates a user.
        self.dummy_salt = secrets.token_bytes(16)

    def connect(self):
        conn = sqlite3.connect(self.database, timeout=10)
        conn.row_factory = sqlite3.Row
        conn.execute("PRAGMA foreign_keys = ON")
        return conn

    def __call__(self, environ, start_response):
        conn = None
        try:
            body = {}
            method = environ.get("REQUEST_METHOD", "GET")
            if method in ("POST", "PUT"):
                if environ.get("CONTENT_TYPE", "").split(";")[0].strip() != "application/json":
                    raise ApiError(415, "json_required", "JSON verisi gerekiyor.")
                try:
                    size = int(environ.get("CONTENT_LENGTH") or 0)
                except ValueError:
                    raise ApiError(400, "invalid_input", "İstek boyutu geçersiz.") from None
                if size < 0 or size > MAX_BODY:
                    raise ApiError(413, "body_too_large", "İstek çok büyük.")
                try:
                    body = json.loads(environ["wsgi.input"].read(size).decode("utf-8"))
                except (UnicodeDecodeError, ValueError):
                    raise ApiError(400, "invalid_json", "İstek verisi okunamadı.") from None
                if not isinstance(body, dict):
                    raise ApiError(400, "invalid_json", "İstek bir nesne olmalı.")
            conn = self.connect()
            # Serializes redemption, revocation, and progress writes across workers.
            conn.execute("BEGIN IMMEDIATE")
            result = self.dispatch(conn, environ, body)
            conn.commit()
            status = 200
        except ApiError as error:
            # Persist failed-attempt counters as well as successful ones.
            if conn is not None:
                conn.commit()
            status, result = error.status, {"error": error.code, "message": error.message}
        except Exception:
            if conn is not None:
                conn.rollback()
            logging.exception("Family API internal error")
            status, result = 500, {"error": "server_error", "message": "Hizmet şu anda kullanılamıyor. Tekrar deneyin."}
        finally:
            if conn is not None:
                conn.close()
        payload = json.dumps(result, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
        headers = [("Content-Type", "application/json; charset=utf-8"), ("Content-Length", str(len(payload))),
                   ("Cache-Control", "no-store"), ("X-Content-Type-Options", "nosniff")]
        if status == 429:
            headers.append(("Retry-After", "60"))
        start_response(f"{status} {HTTPStatus(status).phrase}", headers)
        return [payload]

    def limit(self, conn, key, maximum, window=60):
        now = int(self.clock())
        key = digest(key)
        row = conn.execute("SELECT * FROM rate_limits WHERE key = ?", (key,)).fetchone()
        if row is None or now - row["window_start"] >= window:
            conn.execute("INSERT OR REPLACE INTO rate_limits VALUES (?, ?, 1)", (key, now))
        elif row["attempts"] >= maximum:
            raise ApiError(429, "rate_limited", "Çok fazla deneme yapıldı. Biraz sonra tekrar deneyin.")
        else:
            conn.execute("UPDATE rate_limits SET attempts = attempts + 1 WHERE key = ?", (key,))
        conn.execute("DELETE FROM rate_limits WHERE window_start < ?", (now - 86_400,))

    @staticmethod
    def token(env, prefix):
        auth = env.get("HTTP_AUTHORIZATION", "")
        token = auth[7:] if auth.startswith("Bearer ") else ""
        if not re.fullmatch(re.escape(prefix) + r"[A-Za-z0-9_-]{43}", token):
            raise ApiError(401, "unauthorized", "Lütfen yeniden giriş yapın.")
        return token

    def parent(self, conn, env):
        token = self.token(env, "p_")
        row = conn.execute("SELECT p.* FROM parents p JOIN sessions s ON s.parent_id = p.id "
                           "WHERE s.token_hash = ? AND s.expires_at > ?", (digest(token), int(self.clock()))).fetchone()
        if row is None:
            raise ApiError(401, "unauthorized", "Oturum sona erdi. Yeniden giriş yapın.")
        return row

    def child(self, conn, env):
        token = self.token(env, "d_")
        row = conn.execute("SELECT * FROM children WHERE token_hash = ?", (digest(token),)).fetchone()
        if row is None:
            raise ApiError(401, "unauthorized", "Çocuk bağlantısı bulunamadı.")
        if row["revoked"]:
            raise ApiError(410, "sharing_stopped", "İlerleme paylaşımı kapalı.")
        return row

    def session(self, conn, parent):
        token = "p_" + secrets.token_urlsafe(32)
        expiry = int(self.clock()) + SESSION_TTL
        conn.execute("DELETE FROM sessions WHERE expires_at <= ?", (int(self.clock()),))
        conn.execute("INSERT INTO sessions VALUES (?, ?, ?)", (digest(token), parent["id"], expiry))
        return {"token": token, "email": parent["email"], "name": parent["name"], "expiresAt": stamp(expiry)}

    def dispatch(self, conn, env, body):
        method, path = env["REQUEST_METHOD"], env.get("PATH_INFO", "")
        now = int(self.clock())
        ip = env.get("REMOTE_ADDR", "unknown")
        if method == "GET" and path == "/health":
            return {"ok": True, "version": 1}
        if method == "POST" and path in ("/v1/parent/register", "/v1/parent/login"):
            self.limit(conn, "auth:ip:" + ip, 20, 900)
            email, password = email_field(body), password_field(body)
            self.limit(conn, "auth:email:" + email, 10, 900)
            parent = conn.execute("SELECT * FROM parents WHERE email = ?", (email,)).fetchone()
            if path.endswith("/register"):
                name = text_field(body, "name", 1, 48)
                if parent is not None:
                    raise ApiError(409, "account_exists", "Bu adresle bir hesap var. Giriş yapın.")
                salt = secrets.token_bytes(16)
                hashed = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, PASSWORD_ITERATIONS)
                parent_id = secrets.token_hex(16)
                conn.execute("INSERT INTO parents VALUES (?, ?, ?, ?, ?)", (parent_id, email, name, salt, hashed))
                parent = conn.execute("SELECT * FROM parents WHERE id = ?", (parent_id,)).fetchone()
            else:
                salt = parent["password_salt"] if parent else self.dummy_salt
                hashed = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, PASSWORD_ITERATIONS)
                if parent is None or not hmac.compare_digest(hashed, parent["password_hash"]):
                    raise ApiError(401, "invalid_credentials", "E-posta veya parola hatalı.")
            return self.session(conn, parent)
        if method == "POST" and path == "/v1/parent/logout":
            self.parent(conn, env)
            conn.execute("DELETE FROM sessions WHERE token_hash = ?", (digest(self.token(env, "p_")),))
            return {"ok": True}
        if method == "GET" and path == "/v1/parent/children":
            parent = self.parent(conn, env)
            rows = conn.execute("SELECT c.*, l.linked_at FROM children c JOIN links l ON l.child_id = c.id "
                                "WHERE l.parent_id = ? AND c.revoked = 0 ORDER BY l.linked_at, c.id", (parent["id"],)).fetchall()
            return {"children": [{"id": c["id"], "linkedAt": stamp(c["linked_at"]), "updatedAt": stamp(c["updated_at"]),
                                  "progress": json.loads(c["snapshot"])} for c in rows], "serverTime": stamp(now)}
        if method == "POST" and path == "/v1/parent/links":
            parent = self.parent(conn, env)
            self.limit(conn, "redeem:ip:" + ip, 20)
            self.limit(conn, "redeem:parent:" + parent["id"], 5)
            code = re.sub(r"[\s-]", "", text_field(body, "code", 1, 24)).upper()
            entry = conn.execute("SELECT * FROM pair_codes WHERE code_hash = ? AND expires_at > ?", (digest(code), now)).fetchone()
            if entry is None:
                raise ApiError(400, "invalid_code", "Kod hatalı, süresi dolmuş veya kullanılmış. Çocuk uygulamasından yeni kod alın.")
            child_id = entry["child_id"]
            if conn.execute("SELECT COUNT(*) FROM links WHERE parent_id = ?", (parent["id"],)).fetchone()[0] >= 12:
                raise ApiError(409, "child_limit", "En fazla 12 çocuk bağlanabilir.")
            if conn.execute("SELECT COUNT(*) FROM links WHERE child_id = ?", (child_id,)).fetchone()[0] >= 5:
                raise ApiError(409, "guardian_limit", "Bu çocuk için en fazla 5 veli bağlanabilir.")
            conn.execute("INSERT OR IGNORE INTO links VALUES (?, ?, ?)", (parent["id"], child_id, now))
            conn.execute("DELETE FROM pair_codes WHERE child_id = ?", (child_id,))
            return {"ok": True, "childId": child_id}
        if method == "DELETE" and path.startswith("/v1/parent/children/"):
            parent = self.parent(conn, env)
            child_id = path.removeprefix("/v1/parent/children/")
            cursor = conn.execute("DELETE FROM links WHERE parent_id = ? AND child_id = ?", (parent["id"], child_id))
            if not cursor.rowcount:
                raise ApiError(404, "not_found", "Bağlantı bulunamadı.")
            return {"ok": True}
        if method == "DELETE" and path == "/v1/parent/account":
            parent = self.parent(conn, env)
            conn.execute("DELETE FROM parents WHERE id = ?", (parent["id"],))
            return {"ok": True}
        if method == "PUT" and path == "/v1/child/progress":
            token = self.token(env, "d_")
            snapshot = validate_snapshot(body)
            self.limit(conn, "progress:" + digest(token), 120)
            row = conn.execute("SELECT * FROM children WHERE token_hash = ?", (digest(token),)).fetchone()
            if row is not None and row["revoked"]:
                raise ApiError(410, "sharing_stopped", "İlerleme paylaşımı kapalı.")
            if row is None:
                self.limit(conn, "enroll:ip:" + ip, 30, 3600)
                child_id = secrets.token_hex(16)
                conn.execute("INSERT INTO children (id, token_hash, snapshot, updated_at) VALUES (?, ?, ?, ?)",
                             (child_id, digest(token), json.dumps(snapshot, ensure_ascii=False), now))
            else:
                child_id = row["id"]
                conn.execute("UPDATE children SET snapshot = ?, updated_at = ? WHERE id = ?",
                             (json.dumps(snapshot, ensure_ascii=False), now, child_id))
            return {"id": child_id, "updatedAt": stamp(now)}
        if method == "POST" and path == "/v1/child/pair-code":
            child = self.child(conn, env)
            self.limit(conn, "code:" + child["id"], 5)
            conn.execute("DELETE FROM pair_codes WHERE expires_at <= ? OR child_id = ?", (now, child["id"]))
            code = "".join(secrets.choice(CODE_ALPHABET) for _ in range(8))
            # Regenerate on the very unlikely cross-child collision.
            while conn.execute("SELECT 1 FROM pair_codes WHERE code_hash = ?", (digest(code),)).fetchone():
                code = "".join(secrets.choice(CODE_ALPHABET) for _ in range(8))
            conn.execute("INSERT INTO pair_codes VALUES (?, ?, ?)", (digest(code), child["id"], now + CODE_TTL))
            return {"code": code, "expiresAt": stamp(now + CODE_TTL)}
        if method == "GET" and path == "/v1/child/guardians":
            child = self.child(conn, env)
            rows = conn.execute("SELECT p.id, p.name, l.linked_at FROM parents p JOIN links l ON l.parent_id = p.id "
                                "WHERE l.child_id = ? ORDER BY l.linked_at", (child["id"],)).fetchall()
            return {"guardians": [{"id": p["id"], "name": p["name"], "linkedAt": stamp(p["linked_at"])} for p in rows]}
        if method == "DELETE" and path.startswith("/v1/child/guardians/"):
            child = self.child(conn, env)
            parent_id = path.removeprefix("/v1/child/guardians/")
            conn.execute("DELETE FROM links WHERE child_id = ? AND parent_id = ?", (child["id"], parent_id))
            # Also invalidate any unredeemed code during revocation.
            conn.execute("DELETE FROM pair_codes WHERE child_id = ?", (child["id"],))
            return {"ok": True}
        if method == "DELETE" and path == "/v1/child":
            # Idempotent for offline retry, even after a previous successful deletion.
            token = self.token(env, "d_")
            row = conn.execute("SELECT id FROM children WHERE token_hash = ?", (digest(token),)).fetchone()
            if row:
                child_id = row["id"]
                conn.execute("DELETE FROM links WHERE child_id = ?", (child_id,))
                conn.execute("DELETE FROM pair_codes WHERE child_id = ?", (child_id,))
                conn.execute("UPDATE children SET snapshot = '{}', revoked = 1 WHERE id = ?", (child_id,))
            return {"ok": True}
        raise ApiError(404, "not_found", "İstek bulunamadı.")


def create_app():
    return FamilyApplication(os.environ.get("FAMILY_DATABASE", str(Path(__file__).parent / "data/family.sqlite3")))


class ThreadedServer(ThreadingMixIn, WSGIServer):
    daemon_threads = True


class QuietHandler(WSGIRequestHandler):
    # The API never puts passwords, pairing codes, or tokens in paths or logs.
    def log_message(self, format, *args):
        pass


def main():
    parser = argparse.ArgumentParser(description="Deprem family linking service")
    parser.add_argument("--dev", action="store_true", help="Loopback-only development server")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8787)
    parser.add_argument("--database", default=None)
    parser.add_argument("--trusted-proxy", default=None, help="Only trust forwarded headers from this reverse proxy")
    args = parser.parse_args()
    app = FamilyApplication(args.database) if args.database else create_app()
    if args.dev:
        if args.host not in ("127.0.0.1", "localhost", "::1"):
            parser.error("Development HTTP is restricted to loopback. Use Waitress behind HTTPS for devices.")
        with make_server(args.host, args.port, app, server_class=ThreadedServer, handler_class=QuietHandler) as server:
            print(f"Family development service: http://{args.host}:{args.port}", flush=True)
            server.serve_forever()
    else:
        try:
            from waitress import serve
        except ImportError:
            parser.error("Install Services/Family/requirements.txt, or use --dev for local testing.")
        proxy = {"trusted_proxy": args.trusted_proxy, "trusted_proxy_headers": {"x-forwarded-for", "x-forwarded-proto"}} if args.trusted_proxy else {}
        serve(app, host=args.host, port=args.port, threads=4, max_request_body_size=MAX_BODY,
              channel_timeout=30, ident="DepremFamily", **proxy)


if __name__ == "__main__":
    main()
