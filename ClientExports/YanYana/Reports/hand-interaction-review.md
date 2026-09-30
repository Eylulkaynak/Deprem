# El ve nesne etkileşimleri — teknik inceleme

Bu rapor Editor'daki uygulama ve görüntü kontrollerini anlatır. Hedef yaş oyuncu, gerçek telefon dokunması veya 30 dakikalık eğlence/süre kabulü değildir. Sayısal temas, görünür mesh ve tüm hareket zincirinin incelenmesinin yerine geçmez.

## Uygulanan değişiklikler

- Ada, Efe ve İdil'in her iki eli için `Loop`, `Cylinder`, `Soft`, `Pinch` şekilleri: karakter başına sekiz native blend shape. İncelenen el bölgeleri ilgili el kemiğine bağlandı. Ada ve İdil'in kol yenlerindeki ağırlık geçişleri de düzenlendi; Efe'nin kol yenleri kaynak ağırlıklarını kullanıyor.
- Avuç merkezi ile bilek kemiği arasındaki fark artık kol hedeflerinde hesaba katılıyor. Önkol döndürülüyor; bilek bağımsız çevrilerek kol yüzeyinden koparılmıyor. Native Update, aktif kavrama dışındaki şekilleri sıfırlıyor; duraklatma pozu koruyor.
- Oyun kutusunun gerçek sapları, kutu döndürme ve bırakma hareketleriyle ilişkilendirildi. Geniş ve dar yol korunuyor. Masaya yürünebilir sınırın dışından yaklaşma denemesi testte takıldı; erişilebilir ahşap yerleştirme tahtasıyla yüzey el mesafesine alındı ve tam rotada tekrar geçti.
- Hortum başlığı Blender'da iki elle tutulabilen enine sapla yeniden modellendi. Su çıkışı modelin gerçek ağzında; besleme hortumu gerçek arka bağlantıda. Konum omuz yüksekliğinden hesaplanıyor. Gövde nişanı projenin mevcut yönlendirme bileşeninden, son kavrama native sahne grafiğinden geliyor.
- Hazırlıkta kullanılan fenerin ayrı taşıma görünümü: açılan sap ve öne bakan başlık. Pil/çanta/kayıt kararları aynı fener anahtarlarını kullanıyor. Işık gerçek mercek konumundan veriliyor. Efe'nin küçük oyuncağı da elindeki tutma noktasına bağlandı.
- Kardeşlerin ortak el noktası boy farklarına göre yükseltildi. Efe'nin aile kartında temas hedefi kartın kenarına taşındı.

Yeni player/runtime C# yoktur. İlgili C# dosyaları yalnız `Assets/YanYana/Editor/` altında sahne ve kaynak üretir. Runtime'da Visual Scripting, Transform, SkinnedMeshRenderer ve projenin değiştirilmemiş mevcut kol çözücüsü kullanılır.

## Ölçümler ve kapsam

| Kontrol | Sonuç | Sınır |
|---|---|---|
| Nötr mesh korunumu | Ada/Efe/İdil'de en büyük fark 0,00000047 m altında | Üretilen türevlerin referans duruşu; bütün animasyonların kalitesi değildir |
| İki elli hortum | Altı nişan yönü ve iki elde, seçili ayarda en büyük hata 0,00076 m | Editor'da poz örneklemesi; `hose-reach.csv` |
| Çalışan hortum pozu | Yakın plan örneğinde sol 0,00066 m, sağ 0,00096 m | Tek runtime örneği; bütün hareket süresinin maksimumu değildir |
| Fener ve küçük oyuncak | 24 duruş/yürüyüş örneğinde her karakterde 0,00020 m altında | `carried-prop-reach.txt` |
| Kutu yerleştirme | Tam rota örneğinde sol 0,00018 m, sağ 0,00006 m | `intro-geometry.txt`; görünür son yüzey ayrıca incelendi |
| Kardeşe destek | Ada 0,0045 m, Efe 0,0001 m; iptal, ikinci parmak, çift duraklatma ve yarım/tamamlanmış kayıt sonrası geçiş geçti | `physical-sibling-support.txt`; sonraki kol-yeni düzenlemesi tam rota içinde yeniden çalıştırıldı |
| Düzenlenebilir kaynaklar | 13 karakterin tek iskeleti, nötr meshleri ve kol deformasyonu geçti; üç karakterin sekizer shape key'i doğrulandı | `editable-character-sources.json`, `editable-rig-pose.json` |

Tam rotaların son sürüm kayıtları ve build bilgisi `REVIEW_DELIVERY.md` dosyasındadır. Başarısız yaklaşma denemesi `physical-recorded-route-2-table-approach-failed-050511.txt` dosyasında tutulmuştur; başarı raporuna dönüştürülmemiştir.

## Kaynaklar ve açık görsel işler

Unity el meshleri: `Assets/YanYana/Characters/Interaction/`. Bölge, yön ve temas verileri: `ArtDirection/YanYana/Characters/Interaction/`. Blender shape key'leri: `ArtDirection/YanYana/Characters/ApprovedStyle/{Ada,Efe,Idil}.blend`. Yeni nesneler: `ArtDirection/YanYana/Props/{HoseNozzle,FlashlightTravel,IntroTrayDock}.blend`.

Karakter gövdeleri kullanıcının onayladığı proje modellerinin ayrı uyarlamalarıdır; yeni başlık/sap/yerleştirme tahtası Blender'da özgün üretilmiştir. Bütün parmak araları, kol-yeni siluetleri, sekans geçişleri, efektler ve farklı ekranlardaki kamera örtüşmeleri için son görsel kabul henüz tamamlanmadı. Bu çalışma tüm görsellerin kusursuz olduğu iddiasıyla teslim edilmez.
