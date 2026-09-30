# DEPREM — kamera ve itfaiye sunumu

Keşif kamerası yaklaşık 24 dereceye, itfaiye kamerası yaklaşık 14 dereceye indirildi. Oyun kameraları aynı perspektif sisteminde; takipte sabit yön ve yumuşatma kullanılıyor. Yardım alanındaki uzun yakınlaşmanın geçiş süresi ayrıca düzenlendi.

Hortum başlığı, bağlantı ağzı, vana ve kısa bağlantı hortumu Blender’da yeniden üretildi. Arka tutamak ve öndeki destek kavraması ayrı. Su hattı doğrudan başlığın ucundan başlıyor; besleme hortumu yumuşak bir eğriyle zemine iniyor. Hacimli alev, küçük kıvılcım, duman ve söndürme sonrası buhar aynı sahneye bağlı. Hazırlık sırasında sokak yangınları görünmüyor.

Etkileşim düğmeleri yakın planların üst tarafına taşındı; çanta çalışma yüzeyi kadraja alındı. DEPREM adı ve mevcut görsel arayüz korunuyor.

- [Normal hızda oyun görüntüsü](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Recordings/Deprem-kamera-hortum-20260915.mp4)
- [Windows paketi](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Deprem-Windows-Inceleme-20260915-013738.zip)
- [Android APK](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Android/Deprem-Android-Inceleme-20260915-012748.apk)
- [Unity sahnesi](C:/Users/Gokturk/Documents/GitHub/Deprem/Assets/YanYana/Scenes/YanYana_Adventure.unity)
- [Ayrıntılı test ve kaynak dökümü](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Reports/immersion-review.json)

Son kaydedilmiş sahneyle hazırlıklı rota baştan sona tamamlandı. Kamera kaydında 7358 çizilmiş karede sıfır projeksiyon değişimi ve tanımlı eşiği aşan sıfır sıçrama var. Eşik: 90 ms altındaki karelerde 0,75 m konum veya 12 derece dönüş; açılıştaki kasıtlı kesme hariç.

Dokuz yangın hedefi gerçek ekran ışınlarıyla doğrulanan üretim etkileşimleriyle söndürüldü. Üç ekran oranında 40 bağlantı/yangın ekran kontrolü, 15 harita sürüklemesi, dört finalde 48 hedef ve 72 yüz kontrolü geçti. El temasının en büyük aralığı solda 0.50 mm, sağda 0.40 mm; su başlangıcının başlığa uzaklığı 0.00 mm. Görsel inceleme bu sayısal kontrollere eşlik etti.

Alevin shader saati, parçacıklar ve karakter animasyonları duraklatmada duruyor ve devam ediyor. Windows açılışı ve Android paket/imza kontrolleri geçti. 10,443 başlangıç dosyası byte düzeyinde aynı; 3,016 kayıt değeri geri yüklendi. Yeni runtime C# yok.

Gerçek Android cihazında dokunma, FPS/bellek ve hedef yaş grubuyla 30 dakikalık oynanış doğrulaması yapılmadı; bu kabul koşulları açık. Teknik oynanış kaydı bunların yerine geçmez.

Sahne SHA256: `b180da44ad7525a1562b08f47aee55a5cc16f64601bdf29034f2f5b24a6d3231`.
