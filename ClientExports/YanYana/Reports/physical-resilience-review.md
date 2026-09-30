# Kayıt, duraklatma ve dokunma — 13 Eylül 2026

Bu rapor Unity Play Mode ve üretim EventSystem işleyicileri üzerindeki otomatik kontrolleri kapsar. Gerçek telefon ve çocuk kullanılabilirlik değerlendirmesini kapsamaz.

| Kontrol | Gözlenen sonuç | Ayrıntı |
|---|---|---|
| Korunmanın üç hareketi | Geçti | Ada'yı aşağı sürükleme, sağ eli başa, sol eli masa ayağına taşıma; `Protected=1`, `CoverStage=4`, `Phase=2` |
| Yürüyen ziyaretçide iki kez duraklatma | Geçti | Konum sıçramadı; devamda yol sürdü; üç kişi varış yerine ulaşmadan yardım kamerası ilerlemedi |
| Doğrulanmış kartı yeniden sürükleme | Geçti | Tamamlanan kart yuvasında kaldı |
| Yarım ikinci kart sürüklemesini duraklatma | Geçti | Yarım kart başlangıç yerine döndü; tamamlanan ilk kart korunmuştu |
| Aile doğrulamasında yeniden yükleme | Geçti | İlk kart tamamlanmış, ikinci kart tamamlanmamış kaldı; ikinci kart yerleştirilerek akış sürdü |
| 540 × 960, 540 × 1170, 540 × 1200 aile masası | Geçti | Yakın kamera yatay içeriği korudu; metin taşması raporlanmadı. Safe area masaüstü viewport'unda sınandı |
| Hazır radyo alternatifi | Geçti | Hazırlanıp çantaya yerleştirilen radyo doğrudan resmî yayını sağladı |
| Eksik radyo alternatifi | Geçti | Belirsiz frekansta dinleme ilerletmedi; görevli alıcısı ayarlanıp yayın dinlenince aile bilgisine geçildi |
| Tek yangın odağını söndürüp yeniden yükleme | Geçti | Üretim girdisiyle sönen odak sönük kaldı; kalan iki odakla devam edilerek grup tamamlandı |
| Hortuma ikinci parmak girdisi | Geçti | İlk parmağın nişanı ve kontrol sahipliği korundu |
| Önceki karara kronolojik dönüş | Geçti | Harita → fener → radyo sırasından fenere dönüldü; aile planı kaldı, fener ve sonraki radyo sonucu sıfırlandı, yalnız sonraki giriş geçersizleşti |

Ham kayıtlar `physical-cover-gestures.txt`, `physical-aid-walk-pause.txt`, `physical-family-cancellation.txt`, `aspect-family-*.txt`, `physical-broadcast.txt`, `physical-fire-save-reload.txt` ve `physical-temporal-replay.txt` dosyalarındadır.

Önceki hatalar korunmuştur: ilk tam kayıt kapı NavMesh güncelleme sırası yüzünden, ikinci kayıt yardım kamerası geçişi sırasında erken giriş yüzünden kesildi. İlgili üretim kodları düzeltildikten sonra hazırlıklı yolun tamamı yeniden geçildi. `physical-native-spray.txt` içindeki eski Game View klavye/fare kuyruğu deneyi başarısızdır; çalışan EventSystem dokunma grafiği testinin yerine kullanılamaz.

Tüm kayıt noktaları, eşzamanlı gerçek parmaklar, telefon arka plan/ön plan geçişi ve farklı cihazların sistem çentikleri için kapsamlı kanıt henüz yoktur. Test alanını erken açan iki eski fixture hatası da çocuk oynayışı veya üretim mekaniği başarısızlığı diye sınıflandırılmamalıdır; hazırlanan fixture'ın aktif sahneye geçmesi beklenerek kontroller tekrarlandı.
