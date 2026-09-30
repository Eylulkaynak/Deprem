"""Write current review notes only after the fresh package and route checks exist."""
from pathlib import Path
import argparse,json,re,datetime
p=argparse.ArgumentParser();p.add_argument('--prepared-seconds',required=True);a=p.parse_args()
root=Path(__file__).resolve().parents[2];reports=root/'ClientExports/YanYana/Reports';base=root.as_posix()
package=json.loads((reports/'windows-package.json').read_text());scene=json.loads((reports/'scene-inventory.json').read_text())
for route in (2,3):
    if f'COMPLETE Ending={route}' not in (reports/f'physical-recorded-route-{route}.txt').read_text():raise RuntimeError('Route incomplete')
if 'PASS=True' not in (reports/'resident-route-observation.txt').read_text():raise RuntimeError('Resident observation incomplete')
memory=(reports/'windows-startup-memory.txt').read_text(encoding='utf-8-sig');match=re.search(r'^WorkingSet64\s*:\s*(\d+)',memory,re.M)
memory_bytes=int(match.group(1)) if match else None
if memory_bytes is None:raise RuntimeError('Startup memory observation missing')
archive=Path(package['archive']).as_posix();stamp=datetime.datetime.now().strftime('%d %B %Y, %H:%M')
def link(label,rel):return f'[{label}]({base}/{rel})'
prepared=link('hazırlıklı tam oynanış','ClientExports/YanYana/Recordings/physical-playthrough-20260914-034118.mp4')
neighbor=link('komşu tam oynanış','ClientExports/YanYana/Recordings/physical-playthrough-20260914-033806.mp4')
review=f'''# Yan Yana — güncel Windows inceleme sürümü

[{Path(archive).name}]({archive}) dosyasını çıkarıp **YanYana/Oyna.cmd** dosyasını çalıştırın. Başlangıçta **Yeni oyun** seçin. Paket yalnız YanYana_Adventure sahnesini içerir; çevrimdışı çalışır.

Bu sürüm, çocuklarla doğrulanmış ve tamamlanmış 30 dakikalık oyun olarak sunulmuyor.

Sekiz bölümde gerçek taşıma/yerleştirme, çanta düzenleme, fener ve radyo denemeleri, yetişkin yardımı, korunma, tahliye, İdil'in yangın müdahalesi, Bora'nın yardım noktası ve dört farklı aile buluşması bulunuyor. Hazırlık eksikleri yardım gerektiren güvenli alternatiflere dönüşüyor. Yeni runtime C# yok; mevcut bileşenler ve sahne grafikleri kullanılıyor.

Son ekleme: Yusuf'un ağarmış saç/kaşı, gözlüğü, bıyığı, kıyafeti ve baston kavraması; altı mahalleli varyasyonu; sekiz özgün Blender aksesuarı. Karakterler, kullanıcının onayladığı proje bedenlerinin ayrı uyarlamalarıdır. Yedi ana karakter ve altı komşunun düzenlenebilir Blender kaynakları, UV'leri ve tek eşleşen iskeletleri doğrulandı. {link('Karakter ve baston raporu','ClientExports/YanYana/Reports/resident-integration.md')}.

Son sahneyle {prepared} {a.prepared_seconds} saniyede final 2'ye; {neighbor} 142,0 saniyede final 3'e ulaştı. İkisi de normal hızdaki, çözümleri bilen teknik otomasyondur; çocukların ilk oynayış süresi değildir. Diğer iki finalin önceki tam kayıtları {link('yol raporunda','ClientExports/YanYana/Reports/four-full-routes.md')} sürümleriyle birlikte yer alıyor. Son komşu turunda baston/el açıklığı en fazla 7,51 mm, p95 2,68 mm; zemine gömülme görülmedi.

Windows derlemesi, bağımsız açılış ve NavMesh kontrolü geçti. Tam giriş yolları Unity Editör'de sınandı; bağımsız oyuncuda tam elle tur yapılmadı. ZIP'in {package['files']} dosyası ve CRC/SHA256 bütünlüğü doğrulandı. Başlangıçtaki {package['originalFilesUnchanged']:,} dosya aynı; yeni macera alanları dışında beklenmedik yeni dosya yok. Paket ve sahne hash'leri windows-package.json içindedir.

Windows açılışındaki tek çalışma kümesi örneği {memory_bytes:,} bayt ({memory_bytes/1048576:.1f} MiB). Bu, Android veya 30 dakikalık bellek ölçümü değildir.

Kalan kabul işleri: diğer ayrıntılı parmak kavramaları ve tüm animasyon/efekt/kameraların son incelemesi; sekiz 8–12 yaş oyuncusuyla süre, anlama ve eğlence gözlemi; gerçek Android dokunması, arka plana geçiş ve performans ölçümleri. 28–35 dakika medyanı, 60 FPS/p95 ve 650 MB cihaz hedefleri henüz doğrulanmadı. {link('Ayrıntılı durum','ArtDirection/YanYana/PHYSICAL_ADVENTURE_STATUS.md')}.
'''
(reports/'REVIEW_DELIVERY.md').write_text(review,encoding='utf-8')
status=f'''# Yan Yana — fiziksel maceranın güncel durumu

Bu dosya son yerel inceleme sürümünü açıklar. Tamamlanmış 30 dakikalık çocuk oyunu kabulü verilmiş değildir. En güncel paket ve doğrulama kapsamı: {link('inceleme teslimi','ClientExports/YanYana/Reports/REVIEW_DELIVERY.md')}.

## Oynanabilir akış

- Gündelik taşıma bulmacası: Efe'den hafif kutuyu alma; dar geçitte yön değiştirme veya çevreden dolaşma; masada hizalayıp bırakma.
- Aile planında bağlantılı rota kurma; fenerde kapak/pil yönü/anahtar; radyoda yayın bulma; su ve yiyeceğin ambalajını inceleme.
- Dokuz malzemeyi döndürüp çantanın alanına yerleştirme; fermuar yolunu izleme; iki askıyı ayarlama ve çantayı gerçek yürüyüşle taşıma.
- Raf ve dolap için yetişkinin sırayla yürüyerek çalışması; çocukların yalnız hafif geçiş engellerini taşıması.
- Üç korunma hareketi; Efe'yi kontrol etme; çalışan fener veya erişilebilir acil ışık; güvenli çıkış ve artçıda yeniden durup korunma.
- Yusuf'a sorup hafif engelini açma veya görevliye bildirme; cephe tehlikesini bildirme; güvenli tarafta İdil'e kontrol geçişi.
- İdil ile bağlantı/vanayı hazırlama, üç konuma yürüme, dokuz alev odağına su yönlendirme; gerektiğinde ekip desteği.
- Bora ile üç ziyaretçiyi ihtiyaç masalarına yönlendirme; ayrı su ve yiyecek teslimi; hazırlanmış radyo veya görevli alıcısı; aile bilgisini doğrulama. Desteklenen ve planı öğrenen Efe'nin kayıtlı kararlara bağlı fiziksel katkısı.
- Dört öncelikli final: alternatif alan, öğrenilmiş aile noktası, komşu yardımı veya resmî bilgiyle buluşma. Ayrı yürüyüşler, kamera düzenleri, neden–sonuç kartları ve ziyaret edilen on karar noktasına dönüş.

## Sahne, sanat ve koruma

`Assets/YanYana/Scenes/YanYana_Adventure.unity` bağımsızdır. {scene['scriptMachines']} native ScriptMachine, {scene['graphUnits']:,} graph unit; yeni yazılmış runtime C# sayısı sıfır. Editor altındaki C# yalnız sahne/model üretimi ve test aracıdır. Mevcut hareket/etkileşim bileşenleri değiştirilmedi. Kayıtlar Deprem.YanYana.v1 alanındadır; yeniden kullanılan mini oyun profili ayrıdır.

Yedi ana karakter, son görünüş yönlendirmesiyle projenin onaylanan bedenlerinden ayrı uyarlamalardır. Altı mahalleli kendi kıyafet ve aksesuar varyasyonlarıyla sahnededir. Yusuf'a yaşlı görünüşü ve ayrı baston eli eklendi. On üç düzenlenebilir Blender kaynağında eşleşen tek armature, skin ağırlıkları, UV ve materyaller bulunur. İki üst kol ve sağ önkol için 39 kaynak poz denetimi geçti. Kaynaklar sıfırdan yontulmuş on üç beden olarak tanımlanmaz.

Ortam, etkileşim nesneleri, kutu/minder/sehpa, açılmış ambalajlar, çanta varyantları ve sekiz yeni aksesuar kendi Blender kaynaklarına sahiptir. Üç giysi/saç maskesi kendi geometri verimizden Blender'da üretildi; eski albedo dosyaları değişmedi. Türkçe sentetik konuşmalar ve etkileşim sesleri yerel dosyalardır. Rol portresi, kısa hedef/konuşma, 10/25/45 saniyelik ipucu, azaltılmış sarsıntı ve ses/altyazı seçenekleri bulunur.

Başlangıçtaki {package['originalFilesUnchanged']:,} dosya byte karşılaştırmasında aynı. Yeni macera alanları dışında beklenmedik yeni dosya yok. Unity build sırasında yazılan eski ayarlar/fontlar, kullanıcının baştaki çalışma kopyalarıyla eksiksiz korunur. 19 kullanılmayan yeni görsel deney dosyası yalnız referans/baseline kontrolünden sonra doğrulanmış arşive alındı.

## Kanıt ve sınırları

- {prepared}: {a.prepared_seconds} saniye, final 2; {neighbor}: 142,0 saniye, final 3. Son sahnenin tam teknik yollarıdır; karar/envanter/yangın sağlığı test kodundan atanmaz. Üretim pointer olayları kullanılır, yürüme için mevcut hareket bileşeni çağrılır. Gerçek telefon dokunması veya çocuk ilk oynayışı değildir.
- Baston son komşu turunda 3.251 örnekte en fazla 7,51 mm, p95 2,68 mm kavrama açıklığıyla geçti; zemine gömülme yok. Önceki başarısız denemeler `resident-integration.md` içinde açıkça ayrılmıştır.
- Önceki anlamlı kontroller: üç dikey oran; giriş nesnesine gerçek raycast; pil yönü, paket karşılaştırması, çanta alanı/fermuar/askı/taşıma; kaydetme/yeniden yükleme; iptal ve çoklu parmak sahipliği; yardımdaki gerçek varışlar; dört final ve kronolojik tekrar. Ham raporlar `ClientExports/YanYana/Reports` altındadır. Her rapor kendi sürüm ve kapsamıyla değerlendirilir.
- Son bağımsız Windows derlemesi/açılışı/NavMesh ve ZIP bütünlüğü geçti. Windows oyuncuda yalnız açılış sınandı; tam elle tur tamamlanmadı. Tek açılış çalışma kümesi {memory_bytes/1048576:.1f} MiB; Android bellek hedefinin kanıtı değildir.

## Tamamlanmamış kabul işleri

- Ada, Efe ve İdil'in ayrıntılı parmak kavrama şekilleri; bütün animasyon, efekt ve kameraların son görsel incelemesi.
- Bağımsız oyuncuda tam elle oynayış; bütün kayıt noktalarında ve gerçek telefonda eşzamanlı dokunma, arka plan/ön plan kontrolü.
- Sekiz hedef yaş oyuncusuyla ilk oynayış, anlama ve eğlence gözlemi. 28–35 dakika medyanı ve en az %80 bağımsız ilk etkileşim doğrulanmadı. Test kiti hazır; katılımcı kayıtları not_run durumundadır. Süreyi yapay beklemeyle uzatma uygulanmaz.
- Android Build Support ve bağlı cihaz bu makinede yok. Android'de 30 dakika, gerçek dokunma, 60 FPS hedefi, p95 ≤20 ms ve bellek <650 MB henüz ölçülmedi.
'''
(root/'ArtDirection/YanYana/PHYSICAL_ADVENTURE_STATUS.md').write_text(status,encoding='utf-8')
print('Updated current review and status for',Path(archive).name)
