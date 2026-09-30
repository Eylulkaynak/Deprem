# Deprem — kamera, zemin ve efekt düzeltmeleri

Üst üste binen avlu, sahanlık ve yol yüzeyleri Blender kaynağında ayrıldı. Görünen zemin düzeltildi; mevcut yürünebilir alan ve çarpışma işaretleri korundu. Yakın yüzey taramasında bu zeminlere ait çakışma kalmadı.

Oyun kameraları aynı ortografik projeksiyonda çalışıyor. Geçişler 1,05 saniyelik yumuşak eğri kullanıyor; takip kamerasındaki ayrı yön düzelticinin oluşturduğu salınım kaldırıldı. Sarsıntı dışındaki dikey takip yumuşatıldı. Geçiş sırasında nesnelere yeni dokunma gitmiyor.

Harita duvarın içinden çıkarıldı, Ada kenarda duruyor ve taş geri düğmesiyle örtüşmüyor. Masaya ulaşmış karakteri birkaç milimetrelik fark nedeniyle bekleten yaklaşma koşulu da düzeltildi; konum ataması veya ışınlanma eklenmedi.

Alev, duman, toz, su darbesi, ıslak yüzey ve kısa söndürme buharı kendi materyal ve efektlerimizle düzenlendi. Yangın önizlemesinden etkileşime geçerken oluşan boş kareler giderildi. Efekt, animasyon ve karakterler duraklatmada birlikte durup devam ediyor.

## Güncel paketler

- [Windows inceleme paketi](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Deprem-Windows-Inceleme-20260914-203147.zip) — çıkarıp `Deprem/Oyna.cmd` dosyasını açın.
- [Android APK](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Android/Deprem-Android-Inceleme-20260914-202352.apk) — ARM64, Android 8 ve sonrası, yerel inceleme imzası.
- [43 saniyelik gerçek oynanış kesiti](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Recordings/Deprem-kamera-ve-efektler-20260914.mp4) — normal hızdaki ikinci tam teknik kayıttan iki kesit.

![Harita, 540×960](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/map-stability-20260914-202522/map-960.png)

## Kontrol kanıtı

- İki hazırlıklı teknik rota baştan sona tamamlandı. Son tam rota kamera kaydında 18232 çizilmiş kare, sıfır projeksiyon değişimi ve sıfır eşik aşan sıçrama var.
- Son sahnenin açılış ve harita kaydında 2872 karede aynı iki sayaç sıfır. Sıçrama eşiği: 90 ms altındaki karelerde 0,75 m konum veya 12 derece dönüş; açılış menüsünden dünyaya kasıtlı kesme hariç.
- Son sahnede üç dikey ekran oranında 15 harita sürüklemesi gerçek ekran ışınıyla doğrulandı; dört finalde 48 hedef ve 72 yüz kadrajı kontrolü geçti.
- Windows derlemesi ve bağımsız oyuncu başlangıcı; Android derlemesi, paket bütünlüğü ve dokuz paket/imza kontrolü geçti.
- 10.443 başlangıç dosyası byte düzeyinde aynı. 3.016 macera kayıt değeri geri yüklendi. Yeni runtime C# yok; sahne grafikleri, Cinemachine ve Editor araçları kullanıldı.

İkinci tam rota ve efekt duraklatma kaydı, son harita kadrajı ve masaya yaklaşma düzeltmesinden önce alındı. Son iki değişiklik için açılış/harita yeniden oynandı ve final ekranları tekrar doğrulandı. Bunlar hedef yaş oyuncusu veya fiziksel Android cihazı testinin yerine geçmez; 30 dakikalık süre ve cihaz FPS/bellek kabul koşulları açık kalıyor.

Sahne SHA256: `4f61ce6c8bdc31e87a66eb419db070e60ac4dad7f60853a286cef21fabc1e564`. Ayrıntılı ölçümler ve dosya hashleri: [visual-stability-review.json](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Reports/visual-stability-review.json).
