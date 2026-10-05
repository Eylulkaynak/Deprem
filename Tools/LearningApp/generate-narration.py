"""Build offline Turkish narration. Only authored educational text is sent to TTS.

Install requirements-narration.txt in a virtual environment; ffmpeg must be on PATH.
Run from any directory. --prepare writes the catalog/manifest without network calls.
Existing clips are reused by text + voice configuration hash, so interrupted runs resume.
"""
from __future__ import annotations

import argparse
import asyncio
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
RESOURCES = ROOT / "Assets/LearningApp/Resources/LearningApp"
OUT = RESOURCES / "Narration"
VOICE, RATE, PITCH = "tr-TR-EmelNeural", "-8%", "+0Hz"
COMMON = [
    "Merhaba. Bugün seninle birlikte öğreneceğiz. Acele etmene gerek yok.",
    "Seçenekleri dinleyelim.",
    "Birlikte bir daha düşünelim.",
    "Evet, doğru seçimi yaptın.",
    "Denemeye devam edebilirsin. Öğrenmek için zamanın var.",
    "Bu bölümü tamamladın. İstersen biraz dinlenebilir, sonra devam edebilirsin.",
    "Oyunu tamamladın. Denediğin için teşekkür ederim. İstersen yeniden oynayabiliriz.",
    "Şimdi aynı sırayla dokunabilirsin.",
    "Önce sırayı birlikte izleyelim.",
    "Bu, güvenli bir davranış. Biz tehlikeli olanları arıyoruz. Bir daha bakalım.",
]


def prepare():
    existing = OUT / "manifest.json"
    if existing.exists() and any(e.get("provider") == "ElevenLabs" for e in
                                 json.loads(existing.read_text(encoding="utf-8"))["entries"]):
        raise RuntimeError("ElevenLabs migration has started. Use import-elevenlabs.py; legacy generation would replace approved audio.")
    path = RESOURCES / "catalog.json"
    catalog = json.loads(path.read_text(encoding="utf-8"))
    copy = json.loads(Path(__file__).with_name("narration-copy.json").read_text(encoding="utf-8"))
    courses = {c["id"]: c for c in catalog["courses"]}
    for key, value in copy["courseInfo"].items():
        courses[key]["exercises"][0]["text"] = value
    for patch in copy["exercisePatches"]:
        exercise = courses[patch["course"]]["exercises"][patch["index"]]
        for key, value in patch.items():
            if key == "choiceLabels":
                assert len(value) == len(exercise["choices"])
                for choice, label in zip(exercise["choices"], value):
                    choice["label"] = label
            elif key not in ("course", "index"):
                exercise[key] = value
    for level in catalog["levels"]:
        if not level["adult"]:
            level["instruction"] = copy["gameInstructions"][level["id"]]
    path.write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    texts = set(COMMON)
    for course in catalog["courses"]:
        for exercise in course["exercises"]:
            for field in ("title", "text", "prompt", "tip", "success"):
                if exercise.get(field): texts.add(exercise[field])
            for choice in exercise["choices"]:
                texts.add(choice["label"])
                if choice.get("feedback"): texts.add(choice["feedback"])
    for level in catalog["levels"]:
        texts.add(level["instruction"])
        for question in level.get("questions", []):
            texts.add(question["prompt"])
            for choice in question["choices"]: texts.add(choice["label"])
    # The pictorial game changes this prompt at runtime before randomizing its answer UI.
    texts.add("Dışarıda güvenli yer hangisi?")
    texts.add("Açık alana git")
    for guide in catalog["guides"]:
        texts.update([guide["title"], guide.get("summary", ""), *guide["points"]])

    entries = []
    for text in sorted(t for t in texts if t and t.strip()):
        spoken = re.sub(r"^(Aferin|Harika|Süper|Çok iyi|Doğru cevap|Doğru bildin)!\s*", "Evet. ", text)
        spoken = spoken.replace("Çök-Kapan-Tutun", "Çök, kapan, tutun").replace("çök-kapan-tutun", "çök, kapan, tutun")
        spoken = spoken.replace("•", " ").replace("&", "ve").replace("SMS", "kısa mesaj")
        spoken = re.sub(r"\b112\b", "yüz on iki", spoken)
        spoken = re.sub(r"\s+", " ", spoken).strip()
        digest = hashlib.sha256((VOICE + RATE + PITCH + "loudnorm-v1" + spoken).encode()).hexdigest()[:24]
        entries.append(dict(text=text, spoken=spoken, resource="LearningApp/Narration/Clips/" + digest))
    OUT.mkdir(parents=True, exist_ok=True)
    manifest = dict(version=1, voice=VOICE, rate=RATE, pitch=PITCH, synthetic=True,
                    direction=copy["direction"], entries=entries)
    (OUT / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return manifest


async def generate(args):
    manifest = prepare()
    unique = {entry["resource"]: entry for entry in manifest["entries"]}
    print(f"Manifest: {len(manifest['entries'])} texts, {len(unique)} unique clips", flush=True)
    if args.prepare: return
    import edge_tts
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg: raise RuntimeError("ffmpeg is required on PATH")
    clips = OUT / "Clips"
    clips.mkdir(parents=True, exist_ok=True)
    semaphore = asyncio.Semaphore(3)
    failures = []
    done = 0

    async def render(resource, entry):
        nonlocal done
        target = clips / (resource.rsplit("/", 1)[1] + ".mp3")
        if target.exists() and target.stat().st_size > 1024 and not args.force:
            done += 1
            return
        async with semaphore:
            for attempt in range(3):
                try:
                    with tempfile.TemporaryDirectory(prefix="deprem_narration_") as tmp:
                        raw, normalized = Path(tmp) / "raw.mp3", Path(tmp) / "normalized.mp3"
                        await edge_tts.Communicate(entry["spoken"], VOICE, rate=RATE, pitch=PITCH,
                                                   connect_timeout=15, receive_timeout=45).save(str(raw))
                        command = [ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-i", str(raw),
                                   "-af", "loudnorm=I=-18:LRA=7:TP=-2,apad=pad_dur=0.12",
                                   "-ar", "24000", "-ac", "1", "-b:a", "96k", str(normalized)]
                        await asyncio.to_thread(subprocess.run, command, check=True, capture_output=True)
                        shutil.copyfile(normalized, target)
                    done += 1
                    if done % 20 == 0 or done == len(unique): print(f"Ready: {done}/{len(unique)}", flush=True)
                    return
                except Exception as error:
                    if attempt == 2: failures.append((target.name, type(error).__name__, str(error)[:180]))
                    else: await asyncio.sleep(1 + attempt * 2)

    await asyncio.gather(*(render(key, entry) for key, entry in unique.items()))
    report = dict(voice=VOICE, texts=len(manifest["entries"]), uniqueClips=len(unique), ready=done, failures=failures)
    report_dir = ROOT / "ClientExports/DepremApp/Narration"
    report_dir.mkdir(parents=True, exist_ok=True)
    (report_dir / "generation-report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False), flush=True)
    if failures: raise SystemExit(1)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--prepare", action="store_true")
    parser.add_argument("--force", action="store_true")
    asyncio.run(generate(parser.parse_args()))
