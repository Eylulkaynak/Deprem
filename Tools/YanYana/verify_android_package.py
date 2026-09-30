"""Check the APK manifest, ARM64 IL2CPP payload and signature; this does not simulate device play."""
from pathlib import Path
import datetime
import hashlib
import json
import os
import re
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[2]
ANDROID = Path('C:/Program Files/Unity/Hub/Editor/6000.0.58f2/Editor/Data/PlaybackEngines/AndroidPlayer')
REPORTS = ROOT / 'ClientExports/YanYana/Reports'


def main():
    build = dict(line.split('=', 1) for line in (REPORTS / 'android-build.txt').read_text(encoding='utf-8-sig').splitlines() if '=' in line)
    if build.get('Result') != 'Succeeded' or build.get('Errors') != '0':
        raise SystemExit('A successful Android build is required before package verification.')
    apk = Path(build['Apk']).resolve()
    if not apk.is_relative_to(ROOT / 'ClientExports/YanYana/Android') or not apk.is_file():
        raise SystemExit('Invalid APK path in the build report.')
    with apk.open('rb') as stream:
        sha = hashlib.file_digest(stream, 'sha256').hexdigest()
    if sha != build['ApkSha256']:
        raise SystemExit('The APK changed after the build report.')
    environment = dict(os.environ, JAVA_HOME=str(ANDROID / 'OpenJDK'))
    badging = subprocess.run([str(ANDROID / 'SDK/build-tools/34.0.0/aapt.exe'), 'dump', 'badging', str(apk)], capture_output=True, text=True, timeout=60, env=environment)
    signing = subprocess.run([str(ANDROID / 'SDK/build-tools/34.0.0/apksigner.bat'), 'verify', '--verbose', '--print-certs', str(apk)], capture_output=True, text=True, timeout=60, env=environment)
    (REPORTS / 'android-apk-manifest.txt').write_text(badging.stdout + badging.stderr, encoding='utf-8')
    (REPORTS / 'android-apk-signature.txt').write_text(signing.stdout + signing.stderr, encoding='utf-8')
    with zipfile.ZipFile(apk) as package:
        corrupt = package.testzip()
        names = package.namelist()
    architectures = sorted({name.split('/')[1] for name in names if name.startswith('lib/') and name.endswith('.so')})
    checks = {
        'zipCrc': corrupt is None,
        'aaptReadable': badging.returncode == 0,
        'packageId': "package: name='com.yanyana.adventure.review'" in badging.stdout,
        'originalGameName': "application-label:'Deprem'" in badging.stdout,
        'minimumAndroidApi26': "sdkVersion:'26'" in badging.stdout,
        'targetAndroidApi35': "targetSdkVersion:'35'" in badging.stdout,
        'arm64Only': architectures == ['arm64-v8a'],
        'il2cppPresent': 'lib/arm64-v8a/libil2cpp.so' in names,
        'signatureVerified': signing.returncode == 0,
    }
    report = {
        'checkedAt': datetime.datetime.now().astimezone().isoformat(),
        'apk': str(apk), 'sha256': sha, 'bytes': apk.stat().st_size,
        'scene': build['Scenes'], 'sceneSha256': build['SceneSha256'],
        'checks': checks, 'passed': all(checks.values()),
        'architectures': architectures, 'buildWarnings': int(build['Warnings']),
        'signing': 'Android debug certificate for local review',
        'realDevicePlaytestCompleted': False,
    }
    (REPORTS / 'android-package.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report, indent=2))
    if not report['passed']:
        raise SystemExit('APK verification failed; inspect the manifest/signature reports.')
    notes = f'''# Deprem — Android inceleme paketi

**{apk.name}** — {apk.stat().st_size / 1024**2:.1f} MiB. Android 8.0 veya üzeri, ARM64 telefon/tablet için hazırlanmıştır. Tek sahne: `YanYana_Adventure`. Paket kimliği `com.yanyana.adventure.review`; yerel inceleme için Android debug sertifikasıyla imzalıdır.

APK derlemesi başarılı. Paket manifesti, ZIP bütünlüğü, ARM64 IL2CPP içeriği ve imzası kontrol edildi. SHA256: `{sha}`. Build uyarı sayısı: {build['Warnings']}. Kanıtlar `android-build.txt`, `android-package.json`, `android-apk-manifest.txt` ve `android-apk-signature.txt` dosyalarındadır.

Bu derlemede mevcut proje kodundaki `OnMouse_` işleyicileri için bir mobil performans uyarısı bulunuyor. Mevcut runtime dosyaları değiştirilmedi; bu uyarının gerçek cihazdaki etkisi henüz ölçülmedi.

APK'yı Android cihaza aktararak açabilirsiniz. Android kurulum izni isterse yalnız APK'yı açtığınız uygulamaya gerekli izni verin. USB ile test yapılacaksa SDK içindeki ADB ile kurulum komutu: `adb install -r "{apk.name}"`.

Kaynak projede Android platformuna geçilmedi; derleme bağımsız kopyada yapıldı. Mevcut Windows inceleme paketi korunuyor. Bu APK'nın oyun sahnesi SHA256 değeri `{build['SceneSha256']}`.

**Cihaz testi yapılmadı.** Kullanıcı şu anda Android telefonu olmadığını belirtti; bağlı cihaz yok. APK'nın cihazda açılması, gerçek dokunma, arka plan/ön plan, dört final ve 30 dakikalık FPS/p95/bellek ölçümü açık doğrulama koşullarıdır. Sekiz çocukla ilk oynayış ve 28–35 dakika medyanı da henüz doğrulanmadı.
'''
    (REPORTS / 'ANDROID_REVIEW.md').write_text(notes, encoding='utf-8')


if __name__ == '__main__':
    main()
