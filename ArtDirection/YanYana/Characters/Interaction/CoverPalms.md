# Korunma sırasında avuç teması

Ada ve Efe'nin başı koruyan ellerinde açık el yüzeyi kullanılır. Hedef bilek eklemi değildir; el kaynağındaki avuç merkezi baş yüzeyindeki bir noktaya yerleşir. Masa eli mevcut Soft şekliyle kavrar. Bu çalışma yeni bir runtime bileşeni veya yeni karakter mesh'i eklemez.

`YanYanaCoverReachQA` düzenlenmiş sahneyi değiştirmeden çocukların kopyalarını çömelmiş pozda ölçer. Baş kemiğine ağırlıklandırılmış gerçek mesh vertex'lerinden yüzey adayları çıkarır; el erişimi ve avuç yönünü birlikte karşılaştırır. Ham sonuçlar `ClientExports/YanYana/Reports/cover-palm-reach.csv` içindedir. Seçilen adayların tek poz ölçümleri Ada için yaklaşık 0,19 mm, Efe için 0,05 mm'dir; bu değerler tek başına oynanış kabulü değildir.

Sahne üreticisi her baş kemiğinin altında iki avuç hedefi oluşturur. Hedefin konumu başın yüzeyini, ileri ekseni avucun bakacağı iç yönü tanımlar. Kemiğin kaynak ölçeği santimetre olduğu için yerel konumların sayısal değerleri dünya metrelerinden farklıdır. Ada'nın ikinci hedefi de sol el için ayrıca ölçüldü; tek pozda yaklaşık 0,27 mm hata verdi. Dirsek yönleri karakterin yönüyle döner. Artçı sırasında iki el için ayrı hedef bulunur.

Masa ayağı hedefleri ayağın çocuğa bakan tarafına doğru az miktarda kaydırılmıştır; Efe'nin kısa kolu için hedef, ayağın merkezinden çocuğa doğru 3 cm kaydırılmış erişilebilir bölümdedir. Masa ve karakter boyutları değiştirilmez.

Merdivene geçmeden Ada durur, Efe yanına yürüyerek gelir. Artçı, aralarındaki mesafe 95 cm'nin altına geldikten sonra başlar. Bu sırada sahne grafiği mevcut yürüme bileşenini kullanır; Efe taşınmaz veya ışınlanmaz. Yakın kadraj iki çocuğun başını takip eder ve korunma başladıktan sonra sürükleme yönergesi kaldırılır.

Taşınan çantanın sırt pedi ceketin gerçek bir sırt vertex'ini izler. Aynı bone ağırlıkları sahnede native TransformPoint/TransformVector bağlantılarıyla uygulanır; ped ile giysi arasında 5 mm açıklık bırakılır. Çanta yönünü yüzey normali ve gövde ekseni belirler. Sadece taşınan modelin yönü çevrilerek askılar sırta bakacak şekilde yerleştirilir; hazırlık masasındaki çanta değişmez. Seçilen vertex ve ped ölçüsü `backpack-surface-binding.txt` raporundadır.

Asıl doğrulama, yeniden açılmış sahnedeki üç korunma hareketinin ve artçının normal hızlı kaydıyla yapılır. Görüntüler ve anlık avuç hata ölçümleri `ClientExports/YanYana/Reports/presentation-review.md` dosyasında listelenir. Hedef yaş çocuk gözlemi ve gerçek cihaz kabulü ayrıca gereklidir.
