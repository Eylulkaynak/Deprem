"""Test the actual mobile UI against a temporary API in an isolated Unity project.

This never opens, resets, or tests against the user's active project/save/database.
Usage: python Tools/LearningApp/verify-family.py [--unity /path/to/Unity]
"""
import argparse
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import time
from urllib.request import urlopen
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]


def copy(source, destination):
    destination.parent.mkdir(parents=True, exist_ok=True)
    if source.is_dir():
        shutil.copytree(source, destination)
    else:
        shutil.copy2(source, destination)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", default=r"C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Unity.exe")
    parser.add_argument("--reuse", type=Path, help="Reuse a previous isolated verification folder inside this workspace")
    args = parser.parse_args()
    if not Path(args.unity).is_file():
        parser.error("Set --unity to the Unity 6000.0.58f2 editor executable.")
    folder = args.reuse.resolve() if args.reuse else ROOT / ".codex_tmp" / ("family-isolated-" + uuid.uuid4().hex)
    project = folder / "project"
    if not folder.resolve().is_relative_to(ROOT.resolve()):
        raise RuntimeError("Verification must stay inside the workspace")
    folder.mkdir(parents=True, exist_ok=True)
    if args.reuse:
        for directory in ("Scripts", "Scenes"):
            source = ROOT / "Assets/LearningApp" / directory
            for file in source.iterdir():
                if file.is_file():
                    copy(file, project / "Assets/LearningApp" / directory / file.name)
        for name in ("LearningFamilyTests.cs", "LearningFamilyIntegrationTests.cs"):
            copy(ROOT / "Assets/LearningApp/Editor" / name, project / "Assets/LearningApp/Editor" / name)
        for name in ("FamilyStyles.uss", "LearningStyles.uss"):
            copy(ROOT / "Assets/LearningApp/Resources/LearningApp" / name, project / "Assets/LearningApp/Resources/LearningApp" / name)
    for name in ("Scripts", "Resources", "Scenes"):
        source = ROOT / "Assets/LearningApp" / name
        if not args.reuse:
            copy(source, project / "Assets/LearningApp" / name)
        copy(source.with_suffix(".meta"), (project / "Assets/LearningApp" / name).with_suffix(".meta"))
    for relative in (
        "Assets/LearningApp.meta",
        "Assets/Fonts/StoryPlayful/Nunito-Regular.ttf",
        "Assets/Fonts/StoryPlayful/Nunito-Regular.ttf.meta",
        "Assets/Fonts/StoryPlayful/Nunito-SemiBold.ttf",
        "Assets/Fonts/StoryPlayful/Nunito-SemiBold.ttf.meta",
        "Assets/Scripts/Minigames/MinigameScenarioJourney.cs",
        "Assets/Scripts/Minigames/MinigameScenarioJourney.cs.meta",
        "Assets/Editor/MobilePortraitGameView.cs",
        "Assets/Editor/MobilePortraitGameView.cs.meta",
        "Assets/LearningApp/Editor/LearningFamilyTests.cs",
        "Assets/LearningApp/Editor/LearningFamilyTests.cs.meta",
        "Assets/LearningApp/Editor/LearningFamilyIntegrationTests.cs",
        "Assets/LearningApp/Editor/LearningFamilyIntegrationTests.cs.meta",
        "ProjectSettings/ProjectVersion.txt",
        "ProjectSettings/ProjectSettings.asset",
    ):
        copy(ROOT / relative, project / relative)
    build_settings = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1045 &1
EditorBuildSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Scenes:
  - enabled: 1
    path: Assets/LearningApp/Scenes/Deprem_App.unity
    guid: """ + (ROOT / "Assets/LearningApp/Scenes/Deprem_App.unity.meta").read_text().split("guid: ")[1].splitlines()[0] + "\n  m_configObjects: {}\n"
    (project / "ProjectSettings/EditorBuildSettings.asset").write_text(build_settings)
    packages = project / "Packages"
    packages.mkdir(exist_ok=True)
    (packages / "manifest.json").write_text(json.dumps({"dependencies": {
        "com.unity.inputsystem": "1.14.2", "com.unity.ugui": "2.0.0", "com.unity.test-framework": "1.5.1",
        "com.unity.modules.audio": "1.0.0", "com.unity.modules.jsonserialize": "1.0.0",
        "com.unity.modules.uielements": "1.0.0", "com.unity.modules.unitywebrequest": "1.0.0",
        "com.unity.modules.screencapture": "1.0.0", "com.unity.modules.imageconversion": "1.0.0"
    }}, indent=2))
    # Never leave a publishing URL in a verification copy.
    (project / "Assets/LearningApp/Resources/LearningApp/FamilyConnection.json").write_text('{"serviceUrl":"","timeoutSeconds":5}')
    with socket.socket() as reservation:
        reservation.bind(("127.0.0.1", 0))
        port = reservation.getsockname()[1]
    environment = os.environ.copy()
    environment["DEPREM_FAMILY_TEST_URL"] = f"http://127.0.0.1:{port}"
    startup = None
    if os.name == "nt":
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = subprocess.SW_HIDE
    result_file = folder / "unity-results.xml"
    log_file = folder / "unity.log"
    if result_file.exists():
        result_file.unlink()
    print("Isolated Unity verification:", folder, flush=True)
    with (folder / "service.log").open("w") as service_log:
        service = subprocess.Popen([sys.executable, str(ROOT / "Services/Family/server.py"), "--dev", "--port", str(port),
                                    "--database", str(folder / "family.sqlite3")], stdout=service_log, stderr=service_log, startupinfo=startup)
        try:
            for attempt in range(40):
                try:
                    with urlopen(environment["DEPREM_FAMILY_TEST_URL"] + "/health", timeout=1) as response:
                        if response.status == 200:
                            break
                except OSError:
                    if service.poll() is not None:
                        raise RuntimeError("Verification API could not start")
                    time.sleep(.1)
            else:
                raise RuntimeError("Verification API did not become ready")
            command = [args.unity, "-batchmode", "-projectPath", str(project), "-runTests", "-testPlatform", "EditMode",
                       "-testFilter", "Deprem.Learning.Tests.LearningFamilyTests;Deprem.Learning.Tests.LearningFamilyIntegrationTests",
                       "-testResults", str(result_file), "-logFile", str(log_file)]
            editor = subprocess.Popen(command, cwd=project, env=environment, startupinfo=startup)
            try:
                exit_code = editor.wait(timeout=1200)
            except subprocess.TimeoutExpired:
                editor.terminate()
                raise RuntimeError("Isolated Unity verification exceeded 20 minutes") from None
        finally:
            service.terminate()
            service.wait(timeout=10)
    reports = ROOT / "ClientExports/DepremApp/Family"
    reports.mkdir(parents=True, exist_ok=True)
    if result_file.exists():
        shutil.copy2(result_file, reports / "EditMode.results.xml")
        result = ET.parse(result_file).getroot()
        print(f"Unity family tests: {result.get('passed')} passed, {result.get('failed')} failed", flush=True)
        for failure in result.findall(".//test-case/failure/message"):
            print(failure.text, flush=True)
    captures = project / "ClientExports/DepremApp/Family"
    if captures.is_dir():
        for artifact in captures.iterdir():
            if artifact.is_file():
                shutil.copy2(artifact, reports / artifact.name)
    if not result_file.exists():
        print("Unity did not produce results; inspect", log_file, flush=True)
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
