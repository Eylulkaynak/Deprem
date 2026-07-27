# Deprem — Kronometreli Kör Oynanış Protokolü

Bu protokol, rebuild rotasının gerçek ilk oynayış süresini ve görevlerin sahne üzerinden anlaşılıp anlaşılmadığını ölçer. Etkileşim sayısı, Timeline süresi ve otomatik PlayMode testleri süre kabulü yerine geçmez.

## Test kurulumu

- Android Development Build, dikey ekran ve dokunmatik cihaz kullanılır.
- Ana rota `Story_Rebuild_MainMenu` → Story 01 → Story 02 → Story 03 → Story 04 şeklindedir.
- Testçi `YENİ HİKÂYE` ile başlar. Önceden sahne, görev listesi veya doğru hareket gösterilmez.
- Yalnız çökme, softlock veya cihaz arızasında test durdurulur. Görev çözümü için sözlü yönlendirme verilmez.
- Her testten önce yerel kayıt temizlenir; sarsıntı azaltma seçiminin durumu forma yazılır.

## Örneklem

- İlk kapı: projeyi daha önce görmemiş en az 5 testçi.
- Hedef yaş doğrulaması: mümkün olduğunda 9–14 yaş grubundan en az 3 testçi ve gerekli veli/gözetmen izni.
- Ayrıca bir yetişkin erişilebilirlik testi yapılır; bu sonuç hedef yaş medyanına karıştırılmaz.

## Kronometre noktaları

| Ölçüm | Başlangıç | Bitiş | Hedef |
|---|---|---|---:|
| Story 01 | Oda kontrolü oyuncuya geçtiğinde | Çanta çıkış rafına bırakıldığında | 8–10 dk |
| Story 02 | İlk rota testi açıldığında | Temiz rota ikinci kez kanıtlandığında | 8–10 dk |
| Story 03 | Sakin aile açılışı başladığında | Kardeşler koridora ulaştığında | 9–11 dk |
| Story 04 | Koridor kontrolü başladığında | Aile birleşmesi tamamlandığında | 10–12 dk |
| Tam rota | `YENİ HİKÂYE` seçildiğinde | Final davranış raporu açıldığında | 35–45 dk |

Menüde geçirilen süre ayrıca yazılır ve perde oynanış sürelerine eklenmez. Uygulama odağı kaybolursa kronometre durdurulur.

## Gözlem formu

Her testçi için şu alanlar kaydedilir:

- cihaz, çözünürlük/oran ve sarsıntı azaltma durumu;
- perde başlangıç/bitiş zamanları;
- 30 saniyeden uzun her duraksama ve oyuncunun o anda baktığı nesne;
- yanlış/ramak kala seçimi, checkpoint tekrarı ve ulaşılamayan dokunuş sayısı;
- gözlemcinin müdahale ettiği an ve müdahalenin tam cümlesi;
- kamera duvar kesmesi, hedef kaybı veya nesnenin ekran dışında kalması;
- altyazının okunmadan kapanması ya da oyuncunun altyazı sırasında yürüyebilmesi;
- hazırlık kararlarının Story 03/04'te oyuncu tarafından fark edilip edilmediği;
- finalden sonra “Deniz neden bu eşyayı aldı?” ve “Deprem sırasında neden çıkışa koşmadı?” cevapları.

## Kabul kapısı

- Tam rota medyanı 35–45 dakikadır.
- Her perde medyanı kendi hedef aralığındadır; hiçbir testçi zorunlu bekleme nedeniyle süre doldurmaz.
- Testçilerin en az %80'i dışarıdan görev çözümü almadan ilerler.
- Hiçbir flag kombinasyonu, yanlış seçim veya checkpoint dönüşü softlock üretmez.
- En az iki hazırlık kararının fiziksel sonucu testçilerin çoğu tarafından kendiliğinden fark edilir.
- Aktif sarsıntıda çıkışa koşulmaması, sarsıntı sonrası ayakkabı/çanta kontrolü ve merdiven kullanımı final sorularında doğru açıklanır.

Kapı geçilmezse önce 30 saniyeyi aşan duraksamalar sınıflandırılır: görünürlük/kamera, etkileşim affordance'ı, rota, dil veya içerik eksikliği. Süreyi yapay beklemeyle uzatmak çözüm kabul edilmez.

## Ekran oranı ve performans turu

Kör içerik testinden ayrı olarak aynı rota en az bir 9:16 ve bir 9:19.5 cihaz/emülatör profilinde oynanır. Safe area, altyazı ve bağlamsal dünya vurguları kontrol edilir. Snapdragon 778G sınıfı gerçek cihazda Development Build Profiler ile FPS, GC allocation, bellek ve sıcak sahne geçişi ölçülmeden performans kapısı geçmiş sayılmaz.
