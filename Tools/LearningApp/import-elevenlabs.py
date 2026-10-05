"""Resume ElevenLabs web exports without putting credentials in the Unity project.

plan: prepare the ordered export queue; import: normalize and install a downloaded
MP3 for one queue item. The live manifest changes only after audio validation.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
RESOURCES = ROOT / "Assets/LearningApp/Resources/LearningApp"
MANIFEST = RESOURCES / "Narration/manifest.json"
EXPORTS = ROOT / "ClientExports/DepremApp/Narration/ElevenLabs"
PLAN = EXPORTS / "queue.json"
VOICE = "Nisa - Encouraging, Friendly and Soft"
MODEL = "eleven_v4"


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def make_plan():
    manifest, catalog = read(MANIFEST), read(RESOURCES / "catalog.json")
    by_text = {entry["text"]: entry for entry in manifest["entries"]}
    priority = []
    children = [c for c in catalog["courses"] if c["id"].startswith("child-")]
    # Long educational passages first; their exported files are independently resumable.
    for course in children:
        priority.append(course["exercises"][0]["text"])
    priority.extend(level["instruction"] for level in catalog["levels"] if not level["adult"])
    priority.extend(course["exercises"][0]["title"] for course in children)
    priority.extend([
        "Merhaba. Bugün seninle birlikte öğreneceğiz. Acele etmene gerek yok.",
        "Seçenekleri dinleyelim.", "Birlikte bir daha düşünelim.",
        "Evet, doğru seçimi yaptın.",
        "Denemeye devam edebilirsin. Öğrenmek için zamanın var.",
        "Bu bölümü tamamladın. İstersen biraz dinlenebilir, sonra devam edebilirsin.",
        "Oyunu tamamladın. Denediğin için teşekkür ederim. İstersen yeniden oynayabiliriz.",
        "Şimdi aynı sırayla dokunabilirsin.", "Önce sırayı birlikte izleyelim.",
        "Bu, güvenli bir davranış. Biz tehlikeli olanları arıyoruz. Bir daha bakalım.",
    ])
    for course in children:
        for exercise in course["exercises"]:
            priority.extend(exercise.get(field) for field in ("title", "text", "prompt", "tip", "success"))
            for choice in exercise["choices"]:
                priority.extend([choice["label"], choice.get("feedback")])
    priority.extend(by_text)
    seen, jobs = set(), []
    for text in priority:
        if not text or text not in by_text:
            continue
        spoken = by_text[text]["spoken"]
        if spoken in seen:
            continue
        seen.add(spoken)
        prompt = "[gently] " + spoken
        digest = hashlib.sha256((VOICE + MODEL + "loudnorm-v1" + prompt).encode()).hexdigest()[:24]
        jobs.append(dict(index=len(jobs), spoken=spoken, prompt=prompt,
                         texts=[e["text"] for e in manifest["entries"] if e["spoken"] == spoken],
                         resource="LearningApp/Narration/Clips/" + digest))
    plan = dict(provider="ElevenLabs", voice=VOICE, model=MODEL, jobs=jobs)
    write(PLAN, plan)
    return plan


def status(plan):
    manifest = read(MANIFEST)
    installed = {entry["resource"] for entry in manifest["entries"]}
    pending = [job for job in plan["jobs"] if job["resource"] not in installed]
    return dict(total=len(plan["jobs"]), completed=len(plan["jobs"]) - len(pending),
                remainingCharacters=sum(len(job["prompt"]) for job in pending),
                next=pending[0] if pending else None)


def install(plan, index, audio):
    job = plan["jobs"][index]
    ffmpeg, ffprobe = shutil.which("ffmpeg"), shutil.which("ffprobe")
    if not ffmpeg or not ffprobe:
        raise RuntimeError("ffmpeg and ffprobe are required")
    audio = audio.resolve(strict=True)
    manifest = read(MANIFEST)
    candidates = [e for e in manifest["entries"] if e["text"] in job["texts"]]
    if not candidates or any(e["spoken"] != job["spoken"] for e in candidates):
        raise RuntimeError("Catalog changed; rebuild the queue before importing")
    if all(e["resource"] == job["resource"] for e in candidates):
        print("Already installed; unchanged.")
        return
    backup = EXPORTS / "manifest-before-elevenlabs.json"
    if not backup.exists():
        write(backup, manifest)
    target = RESOURCES.parent / (job["resource"] + ".mp3")
    target.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="deprem_elevenlabs_") as temporary:
        normalized = Path(temporary) / "normalized.mp3"
        subprocess.run([ffmpeg, "-v", "error", "-y", "-i", str(audio), "-af",
                        "loudnorm=I=-18:LRA=7:TP=-2,apad=pad_dur=0.12",
                        "-ar", "44100", "-ac", "1", "-b:a", "128k", str(normalized)],
                       check=True, capture_output=True)
        probe = json.loads(subprocess.check_output([ffprobe, "-v", "error", "-show_entries",
                           "format=duration:stream=channels,sample_rate", "-of", "json", str(normalized)]))
        if float(probe["format"]["duration"]) < .15 or probe["streams"][0]["channels"] != 1:
            raise RuntimeError("Invalid narration audio")
        # Keep the actual ElevenLabs export as provenance and to avoid spending credits twice.
        raw = EXPORTS / "Originals" / (target.stem + ".mp3")
        raw.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(audio, raw)
        shutil.copyfile(normalized, target)
    for entry in manifest["entries"]:
        entry.setdefault("provider", "Microsoft Edge TTS")
        entry.setdefault("voice", "tr-TR-EmelNeural")
    for entry in candidates:
        entry.update(resource=job["resource"], provider="ElevenLabs", voice=VOICE, model=MODEL)
    completed = sum(e["provider"] == "ElevenLabs" for e in manifest["entries"])
    manifest.update(version=2, voice=VOICE if completed == len(manifest["entries"]) else
                    VOICE + " / tr-TR-EmelNeural (migration in progress)",
                    elevenLabsEntries=completed, synthetic=True)
    manifest.pop("rate", None)
    manifest.pop("pitch", None)
    write(EXPORTS / "Receipts" / (target.stem + ".json"),
          dict(index=index, provider="ElevenLabs", voice=VOICE, model=MODEL,
               prompt=job["prompt"], sourceFilename=audio.name,
               sourceSha256=hashlib.sha256(audio.read_bytes()).hexdigest(),
               importedAt=datetime.now(timezone.utc).isoformat(), audio=probe))
    write(MANIFEST, manifest)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["plan", "status", "import", "import-web"])
    parser.add_argument("--index", type=int)
    parser.add_argument("--audio", type=Path)
    args = parser.parse_args()
    plan = make_plan() if args.action == "plan" else read(PLAN)
    if args.action == "import":
        if args.index is None or args.audio is None:
            parser.error("import requires --index and --audio")
        install(plan, args.index, args.audio)
    if args.action == "import-web":
        installed = {entry["resource"] for entry in read(MANIFEST)["entries"]}
        for receipt in read(EXPORTS / "web-downloads.json"):
            if plan["jobs"][receipt["index"]]["resource"] in installed:
                continue
            if len(receipt["files"]) != 1:
                print(f"Not imported: item {receipt['index']} needs download verification")
                continue
            install(plan, receipt["index"], Path.home() / "Downloads" / receipt["files"][0])
    print(json.dumps(status(plan), ensure_ascii=False))
