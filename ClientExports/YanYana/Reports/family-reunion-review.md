# Aile buluşmaları — 14 Eylül 2026

Sahne: `Assets/YanYana/Scenes/YanYana_Adventure.unity`, 09:07 sürümü.
SHA256: `7f5489c855add426bdd7af90882431a25acec59816b173ff900035086b972c83`.
142 ScriptMachine, 41.453 yerleşik grafik birimi; yeni runtime C# yok.

## Düzeltilen davranış

Önceki yakın kameralarda yardım masası, tabela veya ağaç aileyi örtüyordu. Bazı yaklaşma adımlarında ebeveynler ekran dışındaydı; hedefe koddan doğrudan olay gönderen test bunu yakalamıyordu.

Her final artık yol işareti, aileye yaklaşma ve selamlaşma için üç ayrı sahne kamerası kullanıyor. Ana alana çapraz yürüyüş boyunca bakılıyor; ebeveynler karşı kenara sıkışmıyor. Görevli desteği finalinin buluşması ağaç ile yardım masasının arasındaki açık alana taşındı. Yardım noktası etiketleri kendi bölümleri bittikten sonra kapanıyor.

Komşu finalindeki yakın kamera ayrıca genişletilip Yusuf'a doğru kaydırıldı. 09:09 tam rotasında beş katılımcı da kadraj kontrolünü geçti; Yusuf'un yüzü yatayda %16 konumundaydı. Görselde yüzü, bastonu ve ayakları birlikte görüldü.

Ada aileye ulaştığında Efe kendi hareket bileşeniyle yanındaki yere geliyor. Aile işareti ancak kardeş de geldikten sonra açılıyor. Efe bu harekette ışınlanmıyor. Selamın kol pozu gerçek Animator üzerinden kalibre edildi; el yüzün yanında okunuyor. Avuç/el ve baş kemiği ölçümleri `family-wave-calibration.csv` dosyasında. Baş kemiği, kafa modelinin en üst noktası olarak değerlendirilmedi.

Aileye varış tamamlanmış bir eylem olarak otomatik kaydoluyor. Bu kayıt yüklendiğinde hedef aile işareti oluyor. Selamlaşma sırasında duraklatıp kayıttan dönüldüğünde kısa selam yeniden oynuyor ve aynı final tamamlanıyor. Kamera geçişi sırasında giriş kapalı; parmak bırakılmadan yeni eylem açılmıyor.

`physical-reunion-resume.txt`, bu sahnede üç kontrolü de geçti: ek kayıt çağrısı olmadan varış kaydı; doğru hedefle yükleme; duraklatılmış selamdan aynı finale devam. Aile yanındaki çocuklar arası ölçülen uzaklık 0,838 metreydi.

## Ekrandaki hedeflerle kontrol

`physical-reunion-screen-targets.txt`: dört final × üç etkileşim adımı × üç ekran oranı. 540×960, 540×1170 ve 540×1200 boyutlarında 36 ayrı durum kaydedildi. Test önce gerçek kameranın ekran noktasında EventSystem ışın taraması yapıyor; ilk isabetin doğru tıklama/sürükleme bileşenine gitmesini arıyor. Ardından bu doğrulanmış noktadan eylemi gerçekleştiriyor. 48 ışın kontrolü ve dört tamamlanmış final geçti.

Yaklaşma ve selamlaşma sırasında Ada, Derya ve Emre'nin yüz konumları için 72 ek kadraj kontrolü geçti. Yüz merkezleri yatayda %8–92, dikeyde %27–84 içindeydi. Bu kontrol bütün bedenin veya arka plandaki herkesin görünürlüğünü tek başına kanıtlamaz.

Görsel örnekler:

- [Yeni Yolda Birlikte — aile yanında](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/reunion-screen-20260914-092123/ending-1-step2-960.png)
- [Söz Verdiğimiz Yerde — yaklaşma](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/reunion-screen-20260914-092123/ending-2-step1-1170.png)
- [Komşu Eli — yaklaşma](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/reunion-screen-20260914-092123/ending-3-step1-960.png)
- [Komşu Eli — Yusuf dahil tam rota buluşması](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/presentation-20260914-090920/ending-3-7-step3.png)
- [Sesimizi Duydular — aile yanında](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/reunion-screen-20260914-092123/ending-4-step2-960.png)

Bu görüntüler karar değişkenlerinin kurulduğu bağımsız final testlerinden gelir. Önceki bölümlerin tamamlandığı oynayışlar değildir; arka plandaki yangın ve diğer durumlar bu nedenle tam rota kayıtlarıyla değerlendirilmelidir.

Bazı PNG ekran görüntülerinde portre çizilmedi; Image etkin, rengi/şeffaflığı doğru ve sprite referansı mevcut. Bu gözlem önceki 08:50 sahnesinin sabit boyuttaki gerçek kaydı üzerinde ayrıca incelendi: `physical-playthrough-20260914-085511.mp4` dosyasının 204–221 saniyeleri arasındaki 510 karenin hiçbirinde portre alanı boş değildi. 17 ayrı video karesi çıkarıldı ve örnekler görsel olarak da incelendi. `hud-portrait-video-review.json` ölçüm kapsamını kaydeder. Bu sonuç yalnız incelenen buluşma aralığı için geçerlidir; bütün oyunun her karesini onaylamaz.

## Sınırlar

Test girdileri Editör otomasyonudur. Gerçek telefon dokunması, çocukların hedefleri yardımsız anlaması veya 30 dakikalık ilk oynayış süresi bu raporla doğrulanmış değildir. Tam yol, güncel build, kayıt eşleştirmeleri ve korunma raporu ayrı teslim belgelerindedir.
