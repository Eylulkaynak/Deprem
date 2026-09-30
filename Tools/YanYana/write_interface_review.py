"""Write a review that distinguishes UI checks, route checks and actual device evidence."""
from pathlib import Path
import datetime
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
reports = ROOT / 'ClientExports/YanYana/Reports'
scene = ROOT / 'Assets/YanYana/Scenes/YanYana_Adventure.unity'
sha = hashlib.sha256(scene.read_bytes()).hexdigest()
ui = (reports / 'interface-input-qa.txt').read_text(encoding='utf-8-sig')
reunion = (reports / 'physical-reunion-screen-targets.txt').read_text(encoding='utf-8-sig')
route = (reports / 'physical-recorded-route-2.txt').read_text(encoding='utf-8-sig')
for name, content, complete in [('interface-input-qa.txt', ui, 'PASS complete'),
                                ('physical-reunion-screen-targets.txt', reunion, 'PASS complete'),
                                ('physical-recorded-route-2.txt', route, 'COMPLETE Ending=2')]:
    assert complete in content and 'FAIL ' not in content, name
    assert (reports / name).stat().st_mtime > scene.stat().st_mtime, name
windows = json.loads((reports / 'windows-package.json').read_text(encoding='utf-8'))
android = json.loads((reports / 'android-package.json').read_text(encoding='utf-8'))
assert windows['sceneSha256'] == android['sceneSha256'] == sha
assert android['passed']
preservation = json.loads((ROOT / '.codex_tmp/yanyana_implementation/preservation-report.json').read_text(encoding='utf-8-sig'))
assert preservation['unchanged']
restoration = (reports / 'adventure-save-restoration.txt').read_text(encoding='utf-8-sig').strip()
assert restoration.startswith('Restored and verified ')
recording = max((ROOT / 'ClientExports/YanYana/Recordings').glob('*.mp4'), key=lambda p: p.stat().st_mtime)
assert recording.stat().st_mtime > scene.stat().st_mtime and recording.stat().st_size > 100000
inventory = json.loads((reports / 'scene-inventory.json').read_text(encoding='utf-8'))
base = ROOT.as_posix()
screens = ROOT / 'ClientExports/YanYana/Screenshots/interface'
manifest = dict(checkedAt=datetime.datetime.now().astimezone().isoformat(), sceneSha256=sha,
               interfaceRaycasts=ui.count('PASS screen raycast'), framedTitleFaces=ui.count('PASS title face'),
               reunionRaycasts=reunion.count('PASS raycast'), reunionFaces=reunion.count('PASS framed'),
               originalFilesUnchanged=preservation['checked'], saveRestoration=restoration,
               preparedRouteRecording=str(recording), windows=windows['archive'], android=android['apk'],
               runtimeCSharpAdded=0, realDeviceTestCompleted=False, targetAgePlaytestCompleted=False)
(reports / 'interface-review.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
text = f'''# Deprem — yenilenen arayüz

Açılış, gerçek Ada ve Efe modellerinin bulunduğu bir mahalle avlusuna taşındı. Soluk tam ekran kart ve portre kolajı kaldırıldı. Büyük slogan yerine oyun adı, kısa karakter satırı ve tek ana eylem var.

![Unity Play açılışı]({screens.as_posix()}/title-960.png)

Açılışta projenin mevcut DEPREM logosu kullanılıyor. Diğer başlıklarda Lilita One, konuşma ve görevlerde Lexend kullanılıyor. Türkçe görev metinleri Lexend'in tam Türkçe karakter setiyle çiziliyor; Lilita'nın eksik karakterleri için Lexend desteği var. Font lisansları `Assets/YanYana/UI/Fonts/Licenses/` içinde. Arayüz mürekkep yeşili, sıcak beyaz ve sarı kullanıyor. Menü karakterleri resim değil; Animator ile hareket eden sahne modelleri. Menü kapandığında açılış dünyası kapanır, kamera oyuna kesmeyle geçer.

Oyun içi hedef ve konuşma alanları küçültüldü; duraklatma, rol geçişi, sonuç ve tekrar ekranlarının yazıları ve düğmeleri aynı stile getirildi. Menülerin arkasında HUD görünmez ve dokunma almaz.

![Unity Play oyun içi arayüz]({screens.as_posix()}/gameplay-960.png)

## Kontrol edilenler

- 540×960, 540×1170 ve 540×1200 çözünürlüklerinde açılış ve HUD. Metin taşması yok; iki açılış karakterinin yüzleri kadrajda.
- Üretim EventSystem üzerinden {manifest['interfaceRaycasts']} arayüz hedef kontrolü. Yeni oyun, duraklatma, geri dönüş ve kayıt yükleme geçti.
- Yeni sahnede hazırlıktan son buluşmaya kadar bir tam hazırlıklı rota tamamlandı. [Normal hızlı teknik kayıt]({recording.as_posix()}). Bu kayıt çocukların ilk oynama süresi değildir.
- Dört finalin buluşma etkileşimleri üç ekran oranında tamamlandı: {manifest['reunionRaycasts']} hedef, {manifest['reunionFaces']} yüz kontrolü. Bu, dört farklı tam başlangıç rotasının yeniden test edildiği anlamına gelmez.
- {preservation['checked']} başlangıç dosyası aynı. Yeni runtime C# yok. Kayıt, testten önce yedeklendi ve sonra doğrulanarak geri getirildi.
- Windows derlemesi ve bağımsız başlangıcı geçti. Android ARM64 IL2CPP APK derlendi; {len(android['checks'])} paket/imza kontrolü geçti.

Sahnede {inventory['scriptMachines']} ScriptMachine, {inventory['graphUnits']} yerleşik grafik birimi var. Düzenlenebilir arayüz ve açılış sahnesinin üretim kaynağı: [YanYanaInterfaceArtDirection.cs]({base}/Assets/YanYana/Editor/YanYanaInterfaceArtDirection.cs). Bu dosya yalnız Editor içindir. Sahne yeniden üretildiğinde yeni görünüm korunur.

## Güncel inceleme dosyaları

- [Windows inceleme paketi]({Path(windows['archive']).as_posix()})
- [Android APK]({Path(android['apk']).as_posix()})
- [Arayüz giriş kontrolleri]({base}/ClientExports/YanYana/Reports/interface-input-qa.txt)
- [Dört finalin ekran kontrolleri]({base}/ClientExports/YanYana/Reports/physical-reunion-screen-targets.txt)

Her iki paket bu sahneden üretildi: `{sha}`.

Gerçek Android cihaz ve hedef yaş oyuncu testleri yapılmadı. FPS, bellek, 28–35 dakika ve çocukların anlama ölçütleri hâlâ bu testleri gerektiriyor. Bu teslim arayüz yenilemesini ve teknik inceleme sürümlerini kapsıyor.
'''
(reports / 'INTERFACE_REVIEW.md').write_text(text, encoding='utf-8')
(reports / 'REVIEW_DELIVERY.md').write_text(text, encoding='utf-8')
art = ROOT / 'ArtDirection/YanYana/UI'
art.mkdir(parents=True, exist_ok=True)
(art / 'INTERFACE_DIRECTION.md').write_text(text, encoding='utf-8')
print(json.dumps(manifest, ensure_ascii=False, indent=2))
