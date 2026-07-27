#!/usr/bin/env python3
"""Generate the Turkish prototype cast and scene dialogue clips.

The manifest is the single source of truth used by both this script and Unity's
Story 01 scene builder. Each subtitle may contain several speakers; their
segments are synthesized separately and then joined into one clip so the
existing subtitle event remains the synchronization point.
"""

from __future__ import annotations

import argparse
import asyncio
import json
import subprocess
import tempfile
from pathlib import Path

import edge_tts
import imageio_ffmpeg


VOICE_CONFIG = {
    "anne": {
        "voice": "tr-TR-EmelNeural",
        "rate": "-5%",
        "pitch": "-2Hz",
    },
    "baba": {
        "voice": "tr-TR-AhmetNeural",
        "rate": "-8%",
        "pitch": "-8Hz",
    },
    "deniz": {
        "voice": "tr-TR-AhmetNeural",
        "rate": "+4%",
        "pitch": "+22Hz",
    },
    "can": {
        "voice": "tr-TR-AhmetNeural",
        "rate": "+7%",
        "pitch": "+42Hz",
    },
    "radyo": {
        "voice": "tr-TR-AhmetNeural",
        "rate": "-10%",
        "pitch": "-12Hz",
    },
}


async def synthesize_segment(text: str, speaker: str, output_path: Path) -> None:
    config = VOICE_CONFIG[speaker]
    communicator = edge_tts.Communicate(
        text=text,
        voice=config["voice"],
        rate=config["rate"],
        pitch=config["pitch"],
    )
    await communicator.save(str(output_path))


async def render_entry(entry: dict, output_directory: Path, force: bool) -> None:
    destination = output_directory / f"{entry['id']}.mp3"
    if destination.exists() and not force:
        print(f"skip  {destination.name}")
        return

    with tempfile.TemporaryDirectory(prefix="deprem_voice_") as temp_directory:
        temp_root = Path(temp_directory)
        segment_paths = []
        for index, segment in enumerate(entry["segments"]):
            speaker = segment["speaker"].lower()
            if speaker not in VOICE_CONFIG:
                raise ValueError(f"Unknown speaker '{speaker}' in {entry['id']}")

            segment_path = temp_root / f"{index:02d}_{speaker}.mp3"
            await synthesize_segment(segment["text"], speaker, segment_path)
            segment_paths.append(segment_path)

        ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
        silence_path = temp_root / "pause.mp3"
        subprocess.run(
            [
                ffmpeg,
                "-hide_banner",
                "-loglevel",
                "error",
                "-f",
                "lavfi",
                "-i",
                "anullsrc=channel_layout=mono:sample_rate=24000",
                "-t",
                "0.23",
                "-b:a",
                "64k",
                "-y",
                str(silence_path),
            ],
            check=True,
        )

        concat_paths = []
        for index, segment_path in enumerate(segment_paths):
            concat_paths.append(segment_path)
            if index + 1 < len(segment_paths):
                concat_paths.append(silence_path)

        concat_file = temp_root / "concat.txt"
        concat_file.write_text(
            "".join(f"file '{path.as_posix()}'\n" for path in concat_paths),
            encoding="utf-8",
        )
        subprocess.run(
            [
                ffmpeg,
                "-hide_banner",
                "-loglevel",
                "error",
                "-f",
                "concat",
                "-safe",
                "0",
                "-i",
                str(concat_file),
                "-af",
                "loudnorm=I=-18:LRA=11:TP=-2,apad=pad_dur=0.12",
                "-ar",
                "24000",
                "-ac",
                "1",
                "-b:a",
                "128k",
                "-y",
                str(destination),
            ],
            check=True,
        )
        print(f"write {destination.name}")


async def main_async() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--manifest",
        type=Path,
        default=Path("Assets/Story/Audio/Voices/Story01/manifest.json"),
    )
    parser.add_argument("--force", action="store_true")
    args = parser.parse_args()

    manifest_path = args.manifest.resolve()
    output_directory = manifest_path.parent
    output_directory.mkdir(parents=True, exist_ok=True)

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    entries = list(manifest.get("entries", [])) + list(manifest.get("auditions", []))
    for entry in entries:
        await render_entry(entry, output_directory, args.force)


if __name__ == "__main__":
    asyncio.run(main_async())
