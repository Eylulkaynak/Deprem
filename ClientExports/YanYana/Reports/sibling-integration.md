# Efe iletişimi ve aile masası — 14 Eylül 2026

Sarsıntıdan sonra Efe'ye yaklaşmak artık bir kontrol seçimi açar. Oyuncu Ada'nın işaretli elini Efe'nin eline götürebilir veya çıkışı kontrol etmeye geçebilir. İki yol da `SiblingChecked` durumunu tamamlar; yalnız el ile karşılık verme `SiblingSupported=1` yazar. Eksik iletişim ilerlemeyi kilitlemez.

`SiblingSupported=1` ve `FamilyPlan=1` birlikte varsa, aile kaydı açıldığında Efe yardım masasına yürür. Ağaç kartını önce kaldırır, ardından masaya yerleştirir; `IdentityMarker` ve `EfeContributed` kaydedilir. Efe yana çekilir, oyuncu aile portrelerini kendisi eşleştirir. Koşullardan biri eksikse iki işaret de oyuncunun karşılaştırmasına bırakılır.

İçerik native Visual Scripting grafikleriyle kurulmuştur. Yeni runtime C# yoktur. Mevcut hareket ve kol çözümleyici bileşenleri değiştirilmeden kullanılır. Yürüyüşün yaklaşma mesafesi bu etkileşim için geçici ayarlanır ve geri yüklenir. Aile masasının yeni sahnedeki parçaları çocukların erişebileceği yüksekliğe indirilmiştir; eski proje varlıkları düzenlenmemiştir.

Doğrulama:

- `physical-sibling-support.txt`: gerçek oyuncak köprüsü ve üç korunma hareketinden sonra yanlış el bırakma, ikinci parmak, iki kez duraklatma, yarım eylemi yeniden yükleme, destek seçimi ve tamamlanmış seçimi yükleme geçti. Son ölçümde hedefe bilek uzaklığı Ada'da 0,0000 m, Efe'de 0,0017 m. Bu kemik ölçümü bütün parmak pozlarının görsel incelemesini tamamlamaz.
- `physical-efe-family-contribution.txt`: aile aşaması ve ön koşulları izole giriş durumu olarak sağlandı; Efe'nin yürüyüşü, kart hareketi, yana çekilmesi ve sonuçlar üretim grafiğinde oluştu. Yürürken ve yerleştirirken duraklatma; yarım/tamamlanmış eylemi yeniden yükleme geçti. Kamera dönerken yapılan sürükleme reddedildi; bu sırada çift duraklatma ve kayıt sonrası devam geçti. Son portre etkileşiminde kamera geçişi bitmiş, kontrol açık durumdaydı. Oyuncu portre işini ayrıca tamamladı. Bu rapor baştan sona oynayış değildir.
- `physical_efe_places_marker.png` ve `physical_family.png`: kaldırma hareketi ve Efe yana çekildikten sonra açık kalan çalışma yüzeyi incelendi. Önceki yüksek masadaki erişim/örtüşme sorunu düzeltilmiştir.
- 109 yerel sentetik Türkçe konuşma hazır; konuşulan metinlerin hiçbiri 12 kelimeyi aşmıyor.

Dokunma sırası düzeltmesi: kurulu StandaloneInputModule kaynak satırları 185–207 ve InputSystemUIInputModule satırları 738–759, parmak bırakma olayının sürükleme bitişinden önce geldiğini gösterir. Korunma ve askı grafiklerinde sürükleme devam ederken bırakma olayı sahipliği erken silmez. Entegrasyon sürükleyicisi de bu sırayı kullanır. Bu testler gerçek telefondaki dokunmayı doğrulamaz.

Güncel tam yol kayıtları `physical-recorded-route-*.txt` raporlarında, sürüm geçmişi `four-full-routes.md` dosyasındadır. Hedef yaş grubunda 28–35 dakika, anlaşılabilirlik, eğlence ve Android cihaz kabulü henüz doğrulanmamıştır.

14 Eylül'de baştan sona iki güncel tur tamamlandı: hazırlıklı yol `physical-playthrough-20260914-001938.mp4` (201,9 sn, final 2), eksik hazırlık ve ekip desteği yolu `physical-playthrough-20260914-002330.mp4` (66,0 sn, final 1). İlk turda Efe gerçek önceki seçimler sayesinde katkı verdi; ikinci turda iki aile bilgisini oyuncu eşleştirdi. Bu iki tam turda giriş durumu, karar, envanter, yangın sağlığı veya pozlar test kodundan atanmadı.
