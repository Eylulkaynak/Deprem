"""Run the installed Android tools and write evidence without recording device serial numbers."""
from pathlib import Path
import datetime
import json
import subprocess

ROOT = Path(__file__).resolve().parents[2]
ANDROID = Path('C:/Program Files/Unity/Hub/Editor/6000.0.58f2/Editor/Data/PlaybackEngines/AndroidPlayer')


def main():
    checks = {}
    commands = {
        'java': [str(ANDROID / 'OpenJDK/bin/java.exe'), '-version'],
        'clang': [str(ANDROID / 'NDK/toolchains/llvm/prebuilt/windows-x86_64/bin/clang.exe'), '--version'],
        'adb': [str(ANDROID / 'SDK/platform-tools/adb.exe'), 'version'],
        'cmake': [str(ANDROID / 'SDK/cmake/3.22.1/bin/cmake.exe'), '--version'],
        'aapt2': [str(ANDROID / 'SDK/build-tools/34.0.0/aapt2.exe'), 'version'],
    }
    for name, args in commands.items():
        try:
            result = subprocess.run(args, capture_output=True, text=True, timeout=30)
            checks[name] = {'passed': result.returncode == 0, 'exitCode': result.returncode, 'versionOutput': (result.stdout + result.stderr).strip()}
        except Exception as error:
            checks[name] = {'passed': False, 'error': str(error)}
    for name, path in {
        'androidEditorExtension': 'UnityEditor.Android.Extensions.dll',
        'android35Platform': 'SDK/platforms/android-35/android.jar',
        'sdkManager': 'SDK/cmdline-tools/16.0/bin/sdkmanager.bat',
        'androidSdkLicense': 'SDK/licenses/android-sdk-license',
    }.items():
        file = ANDROID / path
        checks[name] = {'passed': file.is_file() and file.stat().st_size > 0}
    devices = subprocess.run([str(ANDROID / 'SDK/platform-tools/adb.exe'), 'devices'], capture_output=True, text=True, timeout=30)
    states = [line.split()[1] for line in devices.stdout.splitlines() if '\t' in line]
    report = {
        'checkedAt': datetime.datetime.now().astimezone().isoformat(),
        'unityVersion': '6000.0.58f2', 'androidRoot': str(ANDROID),
        'passed': all(check['passed'] for check in checks.values()),
        'checks': checks, 'adbDevicesExitCode': devices.returncode,
        'deviceStates': states, 'connectedDeviceCount': len(states),
        'realDevicePlaytestCompleted': False,
    }
    (ROOT / 'ClientExports/YanYana/Reports/android-toolchain-verification.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report, indent=2))
    if not report['passed']:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
