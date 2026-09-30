"""Publish accurate local review notes after the isolated build/package checks."""
from pathlib import Path
import argparse,json,re,datetime,hashlib
p=argparse.ArgumentParser()
for name in ('prepared','neighbor','official','alternative'):
    p.add_argument('--'+name+'-recording',required=True);p.add_argument('--'+name+'-seconds',required=True)
a=p.parse_args();root=Path(__file__).resolve().parents[2];reports=root/'ClientExports/YanYana/Reports';base=root.as_posix()
package=json.loads((reports/'windows-package.json').read_text(encoding='utf-8'));inventory=json.loads((reports/'scene-inventory.json').read_text())
scene=root/'Assets/YanYana/Scenes/YanYana_Adventure.unity'
if hashlib.sha256(scene.read_bytes()).hexdigest()!=package['sceneSha256']:raise RuntimeError('Package scene does not match the current scene')
route_manifest=[]
for route,name in ((2,'prepared'),(3,'neighbor'),(4,'official'),(1,'alternative')):
    report=reports/f'physical-recorded-route-{route}.txt'
    if f'COMPLETE Ending={route}' not in report.read_text() or report.stat().st_mtime<scene.stat().st_mtime:raise RuntimeError('A current-scene route is incomplete')
    recording=root/'ClientExports/YanYana/Recordings'/getattr(a,name+'_recording')
    if not recording.is_file() or recording.stat().st_mtime<scene.stat().st_mtime:raise RuntimeError('Missing current-scene recording')
    route_manifest.append(dict(ending=route,recording=recording.as_posix(),seconds=float(getattr(a,name+'_seconds').replace(',','.')),sceneSha256=package['sceneSha256'],report=report.as_posix(),scope='Normal-speed technical automation in Unity Editor; not target-age or device evidence.'))
(reports/'current-route-recordings.json').write_text(json.dumps(route_manifest,ensure_ascii=False,indent=2),encoding='utf-8')
def link(label,path):return f'[{label}]({base}/{path})'
archive=Path(package['archive']).as_posix();recordings='ClientExports/YanYana/Recordings/'
prepared=link('Hazırlıklı tam rota',recordings+a.prepared_recording);neighbor=link('Komşu tam rotası',recordings+a.neighbor_recording)
official=link('Görevli desteğiyle tam rota',recordings+a.official_recording);alternative=link('Alternatif alan tam rotası',recordings+a.alternative_recording)
memory=(reports/'windows-startup-memory.txt').read_text(encoding='utf-8-sig');match=re.search(r'^WorkingSet64\s*:\s*(\d+)',memory,re.M)
if not match:raise RuntimeError('Startup memory sample missing')
memory_bytes=int(match.group(1));stamp=datetime.datetime.now().isoformat(timespec='minutes')
android_note=link('Android kurulum durumu','ClientExports/YanYana/Reports/android-setup-status.md')
toolchain_record=reports/'android-toolchain-verification.json'
if toolchain_record.exists() and json.loads(toolchain_record.read_text(encoding='utf-8')).get('passed'):
    android_note='Android araçları kuruldu ve çalıştırılarak doğrulandı. '+android_note
android_record=reports/'android-package.json'
if android_record.exists():
    android=json.loads(android_record.read_text(encoding='utf-8'));apk=Path(android['apk'])
    if android.get('passed') and android.get('sceneSha256')==package['sceneSha256'] and apk.is_file() and hashlib.sha256(apk.read_bytes()).hexdigest()==android['sha256']:
        android_note=f"Android inceleme APK'sı hazır: [{apk.name}]({apk.as_posix()}) ({android['bytes']/1048576:.1f} MiB). ARM64 IL2CPP içeriği, paket manifesti ve imzası doğrulandı. "+link('Android inceleme ve kurulum bilgisi','ClientExports/YanYana/Reports/ANDROID_REVIEW.md')+'. Gerçek cihaz denemesi yapılmadı; kullanıcı şu anda Android telefonu olmadığını belirtti.'
review=f'''# Yan Yana — inceleme sürümleri

[{Path(archive).name}]({archive}) dosyasını çıkarıp **YanYana/Oyna.cmd** dosyasını çalıştırın. Başlangıçta **Yeni oyun** seçin. Bağımsız paket yalnız `YanYana_Adventure` sahnesini içerir ve çevrimdışı çalışır.

Sekiz bölüm, üç oynanabilir rol ve dört umutlu final bulunuyor. Hazırlık, taşıma, eşya kullanımı, korunma, tahliye, yangın müdahalesi ve aile buluşması sahne içindeki etkileşimlerle ilerliyor. Bu sürüm, çocuklarla doğrulanmış tamamlanmış 30 dakikalık oyun olarak sunulmuyor.

Son değişiklikler: dört final için yol, yaklaşma ve buluşma kameraları; aile yanında Efe'yi bekleme; düzeltilmiş aile selamı; aileye varışın otomatik kaydı ve yarım selamlaşmadan devam. Üç ekran oranında görünen hedeflerin EventSystem ışınlarıyla kontrolü eklendi. {link('Aile buluşması incelemesi','ClientExports/YanYana/Reports/family-reunion-review.md')}. Önceki kıvrılan alevler, açık efekt duraklatma, kalıcı raf/dolap devrilmesi, baş yüzeyinde avuç teması, artçı öncesi birlikte bekleme ve ceketin sırt yüzeyini izleyen çanta korunuyor. {link('Önceki efekt ve kamera incelemesi','ClientExports/YanYana/Reports/presentation-review.md')}, {link('El ve nesne raporu','ClientExports/YanYana/Reports/hand-interaction-review.md')}. Düzenlenebilir Blender kaynakları saklanmıştır. Karakter gövdeleri onaylı proje uyarlamalarıdır.

Güncel sahnede dört tam yol tamamlandı: {prepared} ({a.prepared_seconds} s), {neighbor} ({a.neighbor_seconds} s), {official} ({a.official_seconds} s), {alternative} ({a.alternative_seconds} s). Bunlar normal hızda, çözümleri bilen teknik otomasyon kayıtlarıdır; çocukların ilk oynayış süresi veya eğlence doğrulaması değildir. {link('Dört yol raporu','ClientExports/YanYana/Reports/four-full-routes.md')}.

Windows derlemesi, bağımsız açılış ve NavMesh başlangıcı geçti. Tam giriş yolları Unity Editör'de sınandı; bağımsız oyuncuda tam elle tur yapılmadı. ZIP'in {package['files']} dosyası ve CRC/SHA256 bütünlüğü kontrol edildi. Başlangıçtaki **{package['originalFilesUnchanged']} dosya değişmedi**. Yeni runtime C# yok; sahnede {inventory['scriptMachines']} grafik makinesi bulunuyor.

Windows açılışındaki tek çalışma kümesi örneği {memory_bytes/1048576:.1f} MiB. Bu, Android veya 30 dakikalık bellek ölçümü değildir.

Kalan kabul koşulları: gerçek oyuncularla animasyon, efekt, kamera ve etkileşimlerin son kabulü; sekiz 8–12 yaş oyuncusuyla ilk oynayış, anlama ve eğlence gözlemi; 28–35 dakika medyanı ve %80 bağımsız ilk etkileşim; gerçek Android dokunması, arka plan/ön plan ve 30 dakikalık performans oturumu. Gerçek cihaz ölçümü yapılmadı. Süre yapay beklemelerle uzatılmadı.

Gerçek oyuncu gözlemi için {link('Yan Yana ilk oynayış protokolü','ArtDirection/YanYana/FIRST_PLAY_PROTOCOL.md')} ve {link('boş gözlem formu','ClientExports/YanYana/Playtests/observations.template.json')} hazır. Bu dosyalar sonuç içermez. Doldurulan veriyi değerlendiren araç `Tools/YanYana/assess_first_play.py` dosyasındadır; boş değerler başarı sayılmaz.

{android_note}

Güncelleme: {stamp}.
'''
(reports/'REVIEW_DELIVERY.md').write_text(review,encoding='utf-8')
status=f'''# Yan Yana — uygulama durumu

Güncel inceleme paketi ve çalıştırma bilgisi: {link('Windows inceleme sürümü','ClientExports/YanYana/Reports/REVIEW_DELIVERY.md')}.

Sahne: `{base}/Assets/YanYana/Scenes/YanYana_Adventure.unity`. Sekiz bölüm, üç oynanabilir rol, dört final; {inventory['scriptMachines']} ScriptMachine ve {inventory['graphUnits']} yerleşik grafik birimi. Yeni runtime C# sayısı: 0. Başlangıçtaki {package['originalFilesUnchanged']} dosya aynı.

Fener/pil ve radyo hazırlığı; çanta yerleştirme, kapatma ve gerçek taşıma; iki geçişli oyun kutusu; sırayla yetişkin görevleri; üç korunma hareketi; kardeşe destek; tahliye ve komşu; dokuz yangın odağı; yardım ve bilgi doğrulama; dört buluşma yürüyüşü uygulanmış durumda. Eksikler alternatif güvenli çözümlerle ilerliyor. Kayıt anahtarları yeni maceraya ayrılmış durumda.

Son çalışma: aileye yürüyüş, yaklaşma ve selamlaşma için ayrı kadrajlar; Efe'nin aile yanında toplanması; yüzün yanında okunan selam hareketi; aileye varınca otomatik kayıt; kayıttan doğru hedefle devam. {link('Aile buluşması incelemesi','ClientExports/YanYana/Reports/family-reunion-review.md')}. Önceki kıvrılan alevler, yerleştirme/artçı kadrajları, baş yüzeyinde avuç teması, sırt yüzeyini izleyen çanta ve açık efekt duraklatma korunuyor. {link('Önceki efekt ve kamera incelemesi','ClientExports/YanYana/Reports/presentation-review.md')}. Yedi ana karakter ve altı komşunun Blender kaynakları, UV'leri, iskeletleri ve el shape key'leri doğrulandı. Kaynak karakter gövdeleri onaylı proje uyarlamalarıdır; özgün aksesuar/nesne üretimleri ayrıca saklanmıştır.

Güncel sahnede dört tam teknik kayıt: {prepared} ({a.prepared_seconds} s, final 2), {neighbor} ({a.neighbor_seconds} s, final 3), {official} ({a.official_seconds} s, final 4), {alternative} ({a.alternative_seconds} s, final 1). Bunlar hedef yaş oyuncu gözlemi değildir.

Windows build/startup ve arşiv bütünlüğü geçti. Android'de 60 FPS hedefi, p95 ≤20 ms, bellek <650 MB, gerçek dokunma ve 30 dakika oturum doğrulanmadı. Sekiz çocukla 28–35 dakika medyanı, anlama ve eğlence gözlemi yapılmadı. Gerçek oyuncularla görsel/etkileşim kabulü ve bağımsız oyuncuda tam elle tur açık kabul işleridir. {android_note} Bu nedenle tam 30 dakikalık oyun kabulü tamamlanmış değildir.
'''
(root/'ArtDirection/YanYana/PHYSICAL_ADVENTURE_STATUS.md').write_text(status,encoding='utf-8')
routes=f'''# Baştan finale teknik oynayışlar

Dört kayıt aynı güncel sahnede, son aile buluşması kameraları, kardeşin toplanması ve kayıttan devam düzeltmeleriyle tamamlandı. Önceki alev, nesne yerleştirme ve kalıcı deprem animasyonu bağlantıları korunuyor. Sahne SHA256: `{package['sceneSha256']}`. Kayıt eşleştirmeleri `current-route-recordings.json` dosyasındadır.

| Son | Son tam kayıt | Sonuç | Süre |
|---|---|---|---|
| 2 — Söz Verdiğimiz Yerde | {prepared} | Aile planı, kullanılabilir fener/radyo; `Ending=2`, `Reunited=1` | {a.prepared_seconds} sn |
| 3 — Komşu Eli | {neighbor} | Eksik hazırlık, acil ışık, görevli alıcısı ve Yusuf'la yürüme; `Ending=3`, `Reunited=1` | {a.neighbor_seconds} sn |
| 4 — Sesimizi Duydular | {official} | Komşuyu görevliye bildirme ve aile bilgisini doğrulama; `Ending=4`, `Reunited=1` | {a.official_seconds} sn |
| 1 — Yeni Yolda Birlikte | {alternative} | Ekip desteği ve alternatif güzergâh; `Ending=1`, `Reunited=1` | {a.alternative_seconds} sn |

Önceki kayıtlar ve başarısız denemeler korunmuştur. Son test çıktıları `physical-recorded-route-1.txt`–`physical-recorded-route-4.txt` dosyalarındadır; tarihli kopyaları da saklanmıştır.

Yollar gerçek Yeni Oyun düğmesinden başlar. Karar, envanter ve yangın sağlığı test kodundan atanmaz. Üretim EventSystem olayları, zamana yayılan sürüklemeler, sahnedeki gerçek yürüyüşler ve rol geçişleri kullanılır. Yürüme hedeflerinde mevcut hareket bileşeni çağrılır. Dört son kendi buluşma etkileşimiyle tamamlanmıştır.

Kayıtlar hızlandırılmamıştır; çözümleri bilen teknik otomasyondur. Hedef yaş oyuncularda 28–35 dakika medyanı, %80 bağımsız ilk etkileşim, anlama ve eğlence gözlemi yerine geçmez. Gerçek telefon ve bağımsız Windows oyuncusunda tam elle tur ayrı kabul koşullarıdır. Build ve korunma bilgisi `REVIEW_DELIVERY.md` dosyasındadır.
'''
(reports/'four-full-routes.md').write_text(routes,encoding='utf-8')
print('Updated review handoff:',Path(archive).name)
