"""Resume offline ElevenLabs narration within an explicit character budget.

Credentials stay outside Unity. Completed clips and exact matching history items
are reused. An uncertain paid request is never retried automatically.
"""
from __future__ import annotations

import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import ssl
import urllib.error
import urllib.parse
import urllib.request

spec = importlib.util.spec_from_file_location("narration_import", Path(__file__).with_name("import-elevenlabs.py"))
pipeline = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pipeline)
API = "https://api.elevenlabs.io"


class Client:
    def __init__(self, key_file):
        self.key = os.environ.get("ELEVENLABS_API_KEY", "").strip()
        if not self.key and key_file.is_file():
            self.key = key_file.read_text(encoding="utf-8-sig").strip()
        if not self.key:
            raise RuntimeError("ELEVENLABS_API_KEY or the external key file is required; no request sent.")
        self.context = ssl.create_default_context()

    def request(self, path, body=None, audio=False):
        request = urllib.request.Request(API + path,
                    data=None if body is None else json.dumps(body, ensure_ascii=False).encode("utf8"),
                    headers={"xi-api-key": self.key, "Content-Type": "application/json",
                             "Accept": "audio/mpeg" if audio else "application/json"})
        try:
            with urllib.request.urlopen(request, context=self.context, timeout=90) as response:
                data = response.read()
                if audio and not response.headers.get("Content-Type", "").startswith("audio/"):
                    raise RuntimeError("ElevenLabs returned a non-audio response; not installed.")
                return data if audio else json.loads(data)
        except urllib.error.HTTPError as error:
            # Do not log headers, credentials, account details, or arbitrary response text.
            try:
                detail = json.loads(error.read()).get("detail", {})
                code = detail.get("status", "") if isinstance(detail, dict) else ""
            except (ValueError, AttributeError):
                code = ""
            raise RuntimeError(f"ElevenLabs HTTP {error.code} {code}; stopped without automatic retry.") from None

    def remaining(self):
        subscription = self.request("/v1/user/subscription")
        limit, used = subscription.get("character_limit"), subscription.get("character_count")
        if not isinstance(limit, int) or not isinstance(used, int):
            raise RuntimeError("Cannot verify remaining credits; no generation requested.")
        return max(0, limit - used)

    def nisa(self):
        result = self.request("/v2/voices?" + urllib.parse.urlencode({"search": "Nisa", "page_size": 100}))
        voices = [v for v in result.get("voices", []) if v.get("name") == pipeline.VOICE]
        if len(voices) != 1:
            raise RuntimeError("The exact Nisa voice must be available in My Voices; no substitute selected.")
        return voices[0]["voice_id"]

    def history_audio(self, voice_id, job):
        # Dialogue history stores voice/text inside dialogue, not the top-level fields.
        # Long narration queries can be rejected. Search by a short prefix, then
        # still require the complete prompt, voice and model to match below.
        query = urllib.parse.urlencode({"search": job["spoken"][:100],
                                        "model_id": pipeline.MODEL, "source": "TTS", "page_size": 100})
        result = self.request("/v1/history?" + query)
        normalized = lambda value: re.sub(r"\s+", " ", value).strip()
        for entry in result.get("history", []):
            dialogue = entry.get("dialogue") or [{"voice_id": entry.get("voice_id"), "text": entry.get("text")}]
            if (entry.get("model_id") == pipeline.MODEL and len(dialogue) == 1
                    and dialogue[0].get("voice_id") == voice_id
                    and normalized(dialogue[0].get("text") or "") == normalized(job["prompt"])):
                identifier = urllib.parse.quote(entry["history_item_id"], safe="")
                return self.request(f"/v1/history/{identifier}/audio", audio=True)
        return None


def run(args):
    plan = pipeline.read(pipeline.PLAN)
    if args.prepare:
        print(json.dumps(pipeline.status(plan), ensure_ascii=False))
        return
    client = Client(args.key_file)
    remaining, voice_id = client.remaining(), client.nisa()
    print(json.dumps(dict(voice=pipeline.VOICE, voiceId=voice_id, model=pipeline.MODEL,
                         remainingCredits=remaining, maxCharacters=args.max_characters)), flush=True)
    if args.preflight:
        return
    if not args.recover_only and (not args.max_characters or args.max_characters < 1):
        raise RuntimeError("Generation requires --max-characters with an explicit positive budget.")
    installed = {entry["resource"] for entry in pipeline.read(pipeline.MANIFEST)["entries"]}
    staging = pipeline.EXPORTS / "API"
    staging.mkdir(parents=True, exist_ok=True)
    budget, generated, recovered = min(remaining, args.max_characters or 0), 0, 0
    for job in plan["jobs"]:
        if job["resource"] in installed:
            continue
        name = job["resource"].rsplit("/", 1)[1]
        raw, pending = staging / (name + ".mp3"), staging / (name + ".pending.json")
        if not raw.is_file():
            data = client.history_audio(voice_id, job)
            if data is not None:
                recovered += 1
            else:
                if args.recover_only:
                    print(json.dumps(dict(stopped="history_not_found", nextIndex=job["index"])), flush=True)
                    break
                if pending.exists():
                    raise RuntimeError(f"Item {job['index']} has an uncertain previous request; check history before retrying.")
                cost = len(job["prompt"])
                remaining = min(client.remaining(), budget)
                if cost > remaining:
                    print(json.dumps(dict(stopped="credit_budget", nextIndex=job["index"],
                                          remainingBudget=remaining, neededCharacters=cost)), flush=True)
                    break
                if cost > 2000:
                    raise RuntimeError("Split this long narration before sending it to Eleven v4.")
                pipeline.write(pending, dict(index=job["index"], voiceId=voice_id,
                                            model=pipeline.MODEL, prompt=job["prompt"]))
                data = client.request("/v1/text-to-dialogue?output_format=mp3_44100_128",
                         body={"inputs": [{"text": job["prompt"], "voice_id": voice_id}],
                               "model_id": pipeline.MODEL}, audio=True)
                budget -= cost
                generated += 1
            if len(data) < 1024:
                raise RuntimeError("Incomplete audio; inspect history before spending credits again.")
            temporary = raw.with_suffix(".mp3.tmp")
            temporary.write_bytes(data)
            temporary.replace(raw)
        pipeline.install(plan, job["index"], raw)
        pending.unlink(missing_ok=True)
        installed.add(job["resource"])
        print(json.dumps(dict(installedIndex=job["index"], generated=generated,
                              recovered=recovered, remainingBudget=budget)), flush=True)
    pipeline.write(pipeline.EXPORTS / "api-report.json",
                   dict(generated=generated, recovered=recovered, remainingBudget=budget,
                        progress=pipeline.status(plan)))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--prepare", action="store_true", help="No network or credentials; report pending clips.")
    mode.add_argument("--preflight", action="store_true", help="Read voice and quota only; generate nothing.")
    mode.add_argument("--recover-only", action="store_true", help="Download existing history until the first missing clip; never generate.")
    parser.add_argument("--max-characters", type=int)
    parser.add_argument("--key-file", type=Path, default=Path.home() / ".codex/elevenlabs-api-key.txt")
    try:
        run(parser.parse_args())
    except (RuntimeError, urllib.error.URLError, TimeoutError) as error:
        parser.exit(1, str(error) + "\n")
