"""Publish the camera/effects review only after the current build and focused checks pass."""
from pathlib import Path
import datetime
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
REPORTS = ROOT / 'ClientExports/YanYana/Reports'
SCENE = ROOT / 'Assets/YanYana/Scenes/YanYana_Adventure.unity'

def read(name):
    return (REPORTS / name).read_text(encoding='utf-8-sig')

def fields(name):
    return dict(line.split('=', 1) for line in read(name).splitlines() if '=' in line)

def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def link(path):
    return Path(path).resolve().as_posix()

scene_sha = digest(SCENE)
windows = json.loads(read('windows-package.json'))
android = json.loads(read('android-package.json'))
assert windows['sceneSha256'] == android['sceneSha256'] == scene_sha
assert android['passed'] and all(android['checks'].values())
assert digest(Path(windows['archive'])) == windows['sha256']
assert digest(Path(android['apk'])) == android['sha256']
for name in ('physical-map-screen-reachability.txt', 'physical-reunion-screen-targets.txt'):
    assert (REPORTS / name).stat().st_mtime >= SCENE.stat().st_mtime
    assert 'PASS complete' in read(name) and 'FAIL ' not in read(name)
assert 'PASS complete' in read('physical-effects-pause.txt')
assert 'COMPLETE Ending=2' in read('physical-recorded-route-2.txt')
assert 'Restored and verified 3016' in read('adventure-save-restoration.txt')
assert fields('android-tool-preferences-restoration.txt')['Restored'] == 'True'
camera = fields('camera-motion-latest.txt')
full_camera = fields('camera-motion-full-route-20260914.txt')
for capture in (camera, full_camera):
    assert capture['ProjectionChanges'] == capture['AbruptRenderedSteps'] == '0'
assert (ROOT / camera['Csv']).stat().st_mtime >= SCENE.stat().st_mtime
preservation = json.loads((ROOT / '.codex_tmp/yanyana_implementation/preservation-report.json').read_text(encoding='utf-8-sig'))
assert preservation['unchanged'] and preservation['checked'] == 10443
assert not json.loads(read('new-files-preservation.json'))['unexpectedNewPaths']
assert not [p for p in (ROOT / 'Assets/YanYana').rglob('*.cs') if 'Editor' not in p.relative_to(ROOT / 'Assets/YanYana').parts]
ground_names = ('Avlu', 'AnaYol', 'YanYol', 'Sahanlik', 'MahalleZemini', 'Basamak2')
ground_conflicts = [line for line in read('visual-stability-inspection.txt').splitlines() if line.startswith('SURFACE ') and any(name in line for name in ground_names)]
assert not ground_conflicts
map_report = read('physical-map-screen-reachability.txt')
map_folder = map_report.split('Screenshots=', 1)[1].splitlines()[0]
reunion = read('physical-reunion-screen-targets.txt')
video = ROOT / 'ClientExports/YanYana/Recordings/Deprem-kamera-ve-efektler-20260914.mp4'
assert video.exists()
data = {
    'checkedAt': datetime.datetime.now().astimezone().isoformat(),
    'sceneSha256': scene_sha,
    'windows': windows,
    'android': android,
    'fullPreparedRouteCamera': full_camera,
    'finalSceneOpeningAndMapCamera': camera,
    'mapRaycastsAndDrags': map_report.count('PASS actual raycast and drag'),
    'reunionScreenRaycasts': reunion.count('PASS raycast '),
    'reunionFaceChecks': reunion.count('PASS framed '),
    'screenDrivenReunions': reunion.count('PASS screen-driven reunion'),
    'groundOverlapCandidates': ground_conflicts,
    'effectsPausePassed': True,
    'originalFilesUnchanged': preservation['checked'],
    'adventureSaveValuesRestored': 3016,
    'newRuntimeCSharpFiles': 0,
    'preview': link(video),
    'mapScreenshots': map_folder,
    'limits': [
        'Full prepared route and effect pause were checked before the final map framing and intro-arrival-only adjustments; they are not presented as full routes of the final scene hash.',
        'The final scene was checked through the fresh opening, map at three portrait ratios, four isolated screen-driven reunion fixtures and both platform builds.',
        'Camera jump audit excludes the deliberate title/world cut and frames longer than 90 ms; it is not device FPS evidence.',
        'No physical Android device or target-age child session was available.'
    ]
}
(REPORTS / 'visual-stability-review.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
body = f'''# Deprem — kamera, zemin ve efekt düzeltmeleri

Üst üste binen avlu, sahanlık ve yol yüzeyleri Blender kaynağında ayrıldı. Görünen zemin düzeltildi; mevcut yürünebilir alan ve çarpışma işaretleri korundu. Yakın yüzey taramasında bu zeminlere ait çakışma kalmadı.

Oyun kameraları aynı ortografik projeksiyonda çalışıyor. Geçişler 1,05 saniyelik yumuşak eğri kullanıyor; takip kamerasındaki ayrı yön düzelticinin oluşturduğu salınım kaldırıldı. Sarsıntı dışındaki dikey takip yumuşatıldı. Geçiş sırasında nesnelere yeni dokunma gitmiyor.

Harita duvarın içinden çıkarıldı, Ada kenarda duruyor ve taş geri düğmesiyle örtüşmüyor. Masaya ulaşmış karakteri birkaç milimetrelik fark nedeniyle bekleten yaklaşma koşulu da düzeltildi; konum ataması veya ışınlanma eklenmedi.

Alev, duman, toz, su darbesi, ıslak yüzey ve kısa söndürme buharı kendi materyal ve efektlerimizle düzenlendi. Yangın önizlemesinden etkileşime geçerken oluşan boş kareler giderildi. Efekt, animasyon ve karakterler duraklatmada birlikte durup devam ediyor.

## Güncel paketler

- [Windows inceleme paketi]({link(windows['archive'])}) — çıkarıp `Deprem/Oyna.cmd` dosyasını açın.
- [Android APK]({link(android['apk'])}) — ARM64, Android 8 ve sonrası, yerel inceleme imzası.
- [43 saniyelik gerçek oynanış kesiti]({link(video)}) — normal hızdaki ikinci tam teknik kayıttan iki kesit.

![Harita, 540×960]({link(ROOT / map_folder / 'map-960.png')})

## Kontrol kanıtı

- İki hazırlıklı teknik rota baştan sona tamamlandı. Son tam rota kamera kaydında {full_camera['Frames']} çizilmiş kare, sıfır projeksiyon değişimi ve sıfır eşik aşan sıçrama var.
- Son sahnenin açılış ve harita kaydında {camera['Frames']} karede aynı iki sayaç sıfır. Sıçrama eşiği: 90 ms altındaki karelerde 0,75 m konum veya 12 derece dönüş; açılış menüsünden dünyaya kasıtlı kesme hariç.
- Son sahnede üç dikey ekran oranında {data['mapRaycastsAndDrags']} harita sürüklemesi gerçek ekran ışınıyla doğrulandı; dört finalde {data['reunionScreenRaycasts']} hedef ve {data['reunionFaceChecks']} yüz kadrajı kontrolü geçti.
- Windows derlemesi ve bağımsız oyuncu başlangıcı; Android derlemesi, paket bütünlüğü ve dokuz paket/imza kontrolü geçti.
- 10.443 başlangıç dosyası byte düzeyinde aynı. 3.016 macera kayıt değeri geri yüklendi. Yeni runtime C# yok; sahne grafikleri, Cinemachine ve Editor araçları kullanıldı.

İkinci tam rota ve efekt duraklatma kaydı, son harita kadrajı ve masaya yaklaşma düzeltmesinden önce alındı. Son iki değişiklik için açılış/harita yeniden oynandı ve final ekranları tekrar doğrulandı. Bunlar hedef yaş oyuncusu veya fiziksel Android cihazı testinin yerine geçmez; 30 dakikalık süre ve cihaz FPS/bellek kabul koşulları açık kalıyor.

Sahne SHA256: `{scene_sha}`. Ayrıntılı ölçümler ve dosya hashleri: [visual-stability-review.json]({link(REPORTS / 'visual-stability-review.json')}).
'''
(REPORTS / 'VISUAL_STABILITY_REVIEW.md').write_text(body, encoding='utf-8')
delivery = REPORTS / 'REVIEW_DELIVERY.md'
prior = REPORTS / 'REVIEW_DELIVERY-branding-20260914-1914.md'
if not prior.exists():
    prior.write_bytes(delivery.read_bytes())
delivery.write_text(body, encoding='utf-8')
print(json.dumps({k: data[k] for k in ('sceneSha256', 'mapRaycastsAndDrags', 'reunionScreenRaycasts', 'reunionFaceChecks', 'screenDrivenReunions', 'newRuntimeCSharpFiles')}, ensure_ascii=False))
