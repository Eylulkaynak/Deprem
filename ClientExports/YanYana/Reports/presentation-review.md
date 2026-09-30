# Yan Yana — efekt, kadraj ve korunma incelemesi

14 Eylül 2026. Güncel sahne SHA256: `1e1c299b3dcf5596db0929de89c1c60f776199da625af3778bd2855e59528272`. Aşağıdaki korunma görselleri ve temas örnekleri bu sahnede 07:32:19'da başlayan teknik turdan alınmıştır. Son değişiklik, Windows derlemesini engelleyen animasyon klibi erişimini yerleşik koleksiyon döngüsüyle değiştirdi. Güncel sahnedeki duraklatma testi ve Windows derlemesi geçti. Bu rapor tüm oyunun son görsel kabulü veya çocuklarla 30 dakikalık deneyim onayı değildir.

| Düzeltme | Kanıt ve sınır |
|---|---|
| Özgün kıvrılan alevler | `FlameTuft.blend`: üç ayrı hareket eden dil, dokuz düzenlenebilir mesh. Unlit renk katmanları; mevcut duman, su ve sönme durumlarıyla birlikte kullanılır. Hazır efekt paketi eklenmedi. |
| Yerleştirme kamerası | Ada/Efe'nin başları HUD altında kalmıyor. `physical-intro-framing.txt`: 540×960, 540×1170 ve 540×1200'de kutuya gerçek raycast, UI sınırları ve baş konumları geçti. Bu kontrol 05:51'deki kadraj değişikliğine aittir; sonraki değişiklikler o kamerayı değiştirmedi. |
| Efektleri açıkça duraklatma | Güncel sahnede 07:31:47'de tamamlanan `physical-effects-pause-073147.txt`: parçacık/klip zamanlarının donması, karakter animasyon hızının durması, çift duraklatma ve devam etme kontrolü. Klipler yerleşik `For Each` düğümleriyle dolaşılır; Windows için geçersiz AOT kodu üreten `get_Item` çağrısı kaldırıldı. |
| Raf ve dolabın devrilmesi | Devrilme animasyonları da kalıcı klip adıyla çağrılır. Önceki eksik hazırlık turlarının gözlemlerinde iki animasyonun sarsıntı boyunca oynadığı kaydedildi. Sabitlenen eşya aynı hareketi yapmaz. |
| Baş ve masa teması | Hedefler bilekten avuç içine taşındı. Baş hedefleri gerçek baş mesh'inden, masa hedefleri gerçek ayağın erişilebilir tarafından seçildi. Aşağıdaki değerler tamamlanmış korunma pozundaki örneklerdir; tüm animasyonun maksimum hatası olarak sunulmaz. |
| Artçıda birlikte kalma | Ada merdivene geçmeden bekler; Efe mevcut hareket bileşeniyle yanına yürür. Artçı 95 cm'den yakınken başlar. Yakın kamera başları izler; korunma başladıktan sonra sürükleme talimatı kalkar. |
| Çömelirken sırt çantası | Taşınan modelin askı yönü düzeltildi. Sırt pedi, ceketin 8010 numaralı vertex'inin aynı bone ağırlıklarını izler; hedef açıklık 5 mm'dir. Kaynak ölçüler `backpack-surface-binding.txt` içindedir. Fotoğrafta çanta gövdeyle eğilip sırta oturur. |

07:32:19'da başlayan hazırlıklı teknik turdaki tamamlanmış masa korunma örneği: Ada'nın baş avucu yaklaşık 0,19 mm, masa kavrama merkezi 0,04 mm; Efe'nin baş avucu 0,05 mm, masa kavrama merkezi 4,67 mm hedef hatası gösterdi. Artçıdaki dört avuç hedef hatası 0,02–0,27 mm; çocukların arası 94,5 cm. Bunlar kaynak avuç işaretlerinin hedefe uzaklığıdır; elin her mesh vertex'i için çarpışma veya yumuşak doku benzetimi değildir. Poz görüntüsüyle birlikte değerlendirilir.

- [Masa altında tamamlanan korunma](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/presentation-20260914-073215/quake-4.png)
- [Artçıda korunma ve sırt çantası](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/presentation-20260914-073215/aftershock.png)
- [Yeni alevler ve hortum kavrama pozu](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/fire1-pause-20260914-073147.png)
- [Yerleştirme kadrajı](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/intro-drop-960.png)
- [Ham poz ve efekt gözlemleri](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/presentation-20260914-073215/observations.txt)
- [Baş yüzeyi ve avuç ölçüm yöntemi](C:/Users/Gokturk/Documents/GitHub/Deprem/ArtDirection/YanYana/Characters/Interaction/CoverPalms.md)
- [Çanta bağlantı yöntemi](C:/Users/Gokturk/Documents/GitHub/Deprem/ArtDirection/YanYana/Props/BackpackCarry.README.md)
- [Alevin düzenlenebilir kaynağı](C:/Users/Gokturk/Documents/GitHub/Deprem/ArtDirection/YanYana/Props/FlameTuft.README.md)

Başarısız alias/fixture testleri ve önceki görsel denemeler tarihli raporlarla korunmuştur. Son tam tur sürümleri `four-full-routes.md` içinde tutulur. Yeni C# dosyaları yalnız `Assets/YanYana/Editor/` altındadır; oynatıcıya yeni runtime C# sınıfı eklenmedi.

Sekiz hedef yaş çocukla ilk oynayış, 28–35 dakika medyanı, %80 bağımsız ilk etkileşim, anlama ve eğlence gözlemi yapılmadı. Gerçek Android dokunması, 30 dakika oturum, p95 ≤20 ms ve bellek <650 MB ölçülmedi. Diğer animasyonların, yetişkin pozlarının ve tüm final kadrajlarının son görsel kabulü ile bağımsız Windows oyuncusunda tam elle tur ayrıca açık kalır.
