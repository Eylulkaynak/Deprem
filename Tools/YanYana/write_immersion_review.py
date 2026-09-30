"""Publish verified artifacts for the lower camera and fire presentation pass."""
from pathlib import Path
import datetime
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[2]
REPORTS = ROOT / 'ClientExports/YanYana/Reports'
SCENE = ROOT / 'Assets/YanYana/Scenes/YanYana_Adventure.unity'

def read(name):
    return (REPORTS / name).read_text(encoding='utf-8-sig')

def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def link(path):
    return Path(path).resolve().as_posix()

def fields(name):
    return dict(line.split('=', 1) for line in read(name).splitlines() if '=' in line)

scene_sha = sha(SCENE)
checks = ('immersion-fire-grip.txt', 'physical-effects-pause.txt',
          'physical-map-screen-reachability.txt', 'physical-reunion-screen-targets.txt')
for name in checks:
    assert (REPORTS / name).stat().st_mtime >= SCENE.stat().st_mtime, name
    assert 'PASS complete' in read(name) and 'FAIL ' not in read(name), name
assert 'COMPLETE Ending=2' in read('physical-recorded-route-2.txt')
assert (REPORTS / 'physical-recorded-route-2.txt').stat().st_mtime >= SCENE.stat().st_mtime
camera = fields('camera-motion-latest.txt')
assert camera['ProjectionChanges'] == camera['AbruptRenderedSteps'] == '0'
assert (ROOT / camera['Csv']).stat().st_mtime >= SCENE.stat().st_mtime
windows = json.loads(read('windows-package.json'))
android = json.loads(read('android-package.json'))
assert windows['sceneSha256'] == android['sceneSha256'] == scene_sha
assert sha(windows['archive']) == windows['sha256']
assert sha(android['apk']) == android['sha256'] and android['passed']
assert all(android['checks'].values())
assert fields('android-tool-preferences-restoration.txt')['Restored'] == 'True'
preservation = json.loads((ROOT / '.codex_tmp/yanyana_implementation/preservation-report.json').read_text(encoding='utf-8-sig'))
assert preservation['unchanged'] and preservation['checked'] == 10443
assert not json.loads(read('new-files-preservation.json'))['unexpectedNewPaths']
assert not [p for p in (ROOT / 'Assets/YanYana').rglob('*.cs') if 'Editor' not in p.parts]
saved = re.search(r'Restored and verified (\d+)', read('adventure-save-restoration.txt'))
assert saved
assert 'Restored' in read('adventure-save-cache-restoration.txt')
video = ROOT / 'ClientExports/YanYana/Recordings/Deprem-kamera-hortum-20260915.mp4'
assert video.is_file()
fire = read('immersion-fire-grip.txt')
metrics = dict(re.findall(r'(samples|leftMax|leftP95|rightMax|rightP95|waterStartGap)=([\d.Ee+-]+)', fire))
data = {
    'checkedAt': datetime.datetime.now().astimezone().isoformat(),
    'sceneSha256': scene_sha, 'windows': windows, 'android': android,
    'renderedCamera': camera, 'firePoseMetres': metrics,
    'fireScreenChecks': fire.count('PASS screen '),
    'firesExtinguishedThroughScreen': fire.count('PASS extinguished '),
    'mapDrags': read(checks[2]).count('PASS actual raycast and drag'),
    'reunionScreenChecks': read(checks[3]).count('PASS raycast '),
    'reunionFaceChecks': read(checks[3]).count('PASS framed '),
    'screenDrivenReunions': read(checks[3]).count('PASS screen-driven reunion'),
    'originalFilesUnchanged': preservation['checked'],
    'saveValuesRestored': int(saved.group(1)), 'newRuntimeCSharp': 0,
    'video': link(video),
    'editableArt': [link(ROOT / 'ArtDirection/YanYana/Props' / (name + '.blend'))
                    for name in ('HoseNozzle', 'FireVolume', 'CouplingSocket', 'CouplingPlug', 'CouplingStation', 'CouplingWheel')],
    'limits': [
        'Editor production pointer handlers and validated EventSystem raycasts; not physical touch.',
        'Camera trace excludes the deliberate title/world cut and frames above 90 ms; not a device FPS benchmark.',
        'No target-age child session or physical Android device was available. 30-minute content budget and device FPS/memory acceptance remain unverified.'
    ]
}
for path in data['editableArt']:
    assert Path(path).is_file()
(REPORTS / 'immersion-review.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
body = f'''# DEPREM — kamera ve itfaiye sunumu

Keşif kamerası yaklaşık 24 dereceye, itfaiye kamerası yaklaşık 14 dereceye indirildi. Oyun kameraları aynı perspektif sisteminde; takipte sabit yön ve yumuşatma kullanılıyor. Yardım alanındaki uzun yakınlaşmanın geçiş süresi ayrıca düzenlendi.

Hortum başlığı, bağlantı ağzı, vana ve kısa bağlantı hortumu Blender’da yeniden üretildi. Arka tutamak ve öndeki destek kavraması ayrı. Su hattı doğrudan başlığın ucundan başlıyor; besleme hortumu yumuşak bir eğriyle zemine iniyor. Hacimli alev, küçük kıvılcım, duman ve söndürme sonrası buhar aynı sahneye bağlı. Hazırlık sırasında sokak yangınları görünmüyor.

Etkileşim düğmeleri yakın planların üst tarafına taşındı; çanta çalışma yüzeyi kadraja alındı. DEPREM adı ve mevcut görsel arayüz korunuyor.

- [Normal hızda oyun görüntüsü]({link(video)})
- [Windows paketi]({windows['archive'].replace(chr(92), '/')})
- [Android APK]({android['apk'].replace(chr(92), '/')})
- [Unity sahnesi]({link(SCENE)})
- [Ayrıntılı test ve kaynak dökümü]({link(REPORTS / 'immersion-review.json')})

Son kaydedilmiş sahneyle hazırlıklı rota baştan sona tamamlandı. Kamera kaydında {camera['Frames']} çizilmiş karede sıfır projeksiyon değişimi ve tanımlı eşiği aşan sıfır sıçrama var. Eşik: 90 ms altındaki karelerde 0,75 m konum veya 12 derece dönüş; açılıştaki kasıtlı kesme hariç.

Dokuz yangın hedefi gerçek ekran ışınlarıyla doğrulanan üretim etkileşimleriyle söndürüldü. Üç ekran oranında {data['fireScreenChecks']} bağlantı/yangın ekran kontrolü, {data['mapDrags']} harita sürüklemesi, dört finalde {data['reunionScreenChecks']} hedef ve {data['reunionFaceChecks']} yüz kontrolü geçti. El temasının en büyük aralığı solda {float(metrics['leftMax'])*1000:.2f} mm, sağda {float(metrics['rightMax'])*1000:.2f} mm; su başlangıcının başlığa uzaklığı {float(metrics['waterStartGap'])*1000:.2f} mm. Görsel inceleme bu sayısal kontrollere eşlik etti.

Alevin shader saati, parçacıklar ve karakter animasyonları duraklatmada duruyor ve devam ediyor. Windows açılışı ve Android paket/imza kontrolleri geçti. {preservation['checked']:,} başlangıç dosyası byte düzeyinde aynı; {data['saveValuesRestored']:,} kayıt değeri geri yüklendi. Yeni runtime C# yok.

Gerçek Android cihazında dokunma, FPS/bellek ve hedef yaş grubuyla 30 dakikalık oynanış doğrulaması yapılmadı; bu kabul koşulları açık. Teknik oynanış kaydı bunların yerine geçmez.

Sahne SHA256: `{scene_sha}`.
'''
(REPORTS / 'IMMERSION_REVIEW.md').write_text(body, encoding='utf-8')
delivery = REPORTS / 'REVIEW_DELIVERY.md'
prior = REPORTS / 'REVIEW_DELIVERY-stability-20260914.md'
if not prior.exists():
    prior.write_bytes(delivery.read_bytes())
delivery.write_text(body, encoding='utf-8')
print(json.dumps({key: data[key] for key in ('sceneSha256', 'fireScreenChecks', 'firesExtinguishedThroughScreen', 'mapDrags', 'screenDrivenReunions', 'newRuntimeCSharp')}, ensure_ascii=False))
