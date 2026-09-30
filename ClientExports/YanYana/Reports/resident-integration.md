# Yusuf ve altı mahalleli — görsel entegrasyon

Yusuf'un onaylanan beden kopyasına ağarmış saç/kaş, özgün gözlük, bıyık, düğmeler ve ayrı baston kavrama yüzeyi eklendi. Selma, Mert, Gül, Deniz, Aslı ve Ozan için altı kıyafet/aksesuar varyasyonu sahneye yerleştirildi. Üçü yardım noktası oynanışında yürüyen ziyaretçi, üçü mahalle sakinidir. Hazır paket eklenmedi; önceki proje karakterleri değişmedi. Bunlar mevcut onaylı bedenlerin uyarlamalarıdır.

Sekiz yeni Blender aksesuarı ve üç geometri tabanlı giysi/saç bölgesi dokusu var. Shader mevcut UV'yi kullanır; ayrı GPU skin veri kanalı gerekmez. Yedi ana karakter ve altı komşunun düzenlenebilir Blender dosyalarında tek eşleşen armature, UV, skin ağırlıkları ve aksesuar bağlantıları bulunur.

Kaynak doğrulamasında 13/13 karakterin metre ölçeği, yere oturması, ağırlıkları, indeksleri ve doku referansları geçti. Blender'da iki üst kol ve sağ önkol ayrı ayrı döndürülerek 39 poz denendi; tüm karakterler geçti. Nötr değerlendirilmiş yüzey sapması en fazla 0,000000185 metreydi. Raporlar: `editable-character-sources.json`, `editable-rig-pose.json`. Bu denetim bütün oyun animasyonlarının kaliteli olduğu anlamına gelmez.

Bastonun adımı, zemine teması ve mevcut kol çözümleyicisi native Visual Scripting ile bağlandı. Elin 1.592 köşesine tutarlı el ağırlığı ve kıvrılmış kavrama geometrisi uygulandı. Önkolun bütünü döndürülerek el ile kolun ayrılması giderildi. Yeni runtime C# oluşturulmadı.

## Testlerin kapsamı

- 02:53:55 komşu rotası final 3'e ulaştı; bastonun zemine 27,5 mm girmesi gözlendi. Bu denemenin baston kontrolü başarısızdır.
- 03:18:16 komşu rotası final 3'e ulaştı; zemin teması düzeldi, bazı yürüyüş pozlarında kavrama açıklığı 76 mm'ye çıktı.
- 03:24:25 komşu rotası final 3'e ulaştı; hedef metni ve tamamlanan kutunun tekrar tetiklenmesi düzeltmesini içerir. Kavrama p95 değeri 0,8 mm olsa da en kötü pozda 72 mm açıklık sürdü. Bu deneme kavrama kabulü sayılmaz. Tanı, bastonun kolun en fazla katlanabildiği uzaklıktan daha yakına gelmesidir; sonraki sahnede basış noktası öne taşındı.
- 03:29:33 komşu rotası da final 3'e ulaştı; daha öne taşınan baston bazı pozlarda kolun erişimini aştı. En büyük 118 mm açıklık nedeniyle bu konum da reddedildi.

Ardından sahnedeki gerçek Yusuf rig'i ayrı önizleme sahnesinde ölçüldü: üç hareket hızında 72 animasyon örneği, üç ileri/geri basış ve üç zemin/yükselme değeri. Yan uzaklık 0,40 m, ileri uzaklık 0,16 m ve ±35 mm adım seçeneği 648 karşılaştırmada en fazla 7,7 mm hata verdi. `cane-reach-probe.csv` bütün adayların sonuçlarını saklar. Bu ölçüm gerçek tur gözleminin yerine geçmez.

**03:38:06 güncel komşu turu geçti.** 3.251 gözlem, 584 yürüyüş örneği; en büyük kavrama açıklığı 7,51 mm, p95 2,68 mm. Zemin açıklığı sayısal yuvarlama düzeyindeki sıfırdan 68,5 mm'ye kadar (basış/yükselme ve zemin geçişleri); zemine gömülme yok. Altı mahalleli bulundu, üç yardım ziyaretçisi kendi masasına ulaştı. `Ending=3`, `Reunited=1`. Normal hız uzman otomasyonu 142,0 saniye sürdü; çocuk oynayış süresi değildir. Kayıt: `physical-playthrough-20260914-033806.mp4`. Güncel duruş ve oyun kamerası görüntüleri `yusuf-pose-diagnostic.png` ve `residents-neighbor.png` dosyalarındadır.

Başarısız gözlemler numaralı `resident-route-observation-*` raporlarında korunur. Son sahnenin sonucu `resident-route-observation.txt`, ilgili tam oynanış `physical-recorded-route-3.txt` ve normal hız MP4 kaydıyla birlikte değerlendirilir. Ölçümlerde hikâye kararı, karakter konumu veya giriş enjekte edilmez; gözlemci gerçek teknik turun hareketlerini izler.

19 kullanılmayan yeni deney dosyası, mevcut kaynaklarda referansı ve başlangıç dosyaları arasında bulunmadığı kontrol edilerek doğrulanmış arşive alındı. Aktif kavrama yüzeyi ve modeller korunur. Ayrıntı: `wardrobe-experiment-archive.json`.

Çocuklarla ilk oynayış, 28–35 dakika medyanı, gerçek dokunma ve Android performansı bu raporun kapsamı dışındadır. Ada/Efe/İdil'in diğer ayrıntılı parmak kavramaları ve bütün sahnenin son görsel incelemesi ayrıca tamamlanmalıdır.
