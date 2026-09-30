# Yan Yana — ilk oynayış gözlemi

Bu form yalnız `YanYana_Adventure` içindir. Projenin eski Story sahnelerinin süre ölçümleri bu maceranın kabulüne eklenmez. Teknik otomasyon kayıtları da oyuncu gözlemi sayılmaz.

## Oturum

En az sekiz, oyunu ilk kez gören 8–12 yaş oyuncusuyla aynı aday sürüm oynanır. Kimlik yerine P01–P08 gibi kodlar kullanılır. Oynanan paketin SHA256 değeri ve cihaz bilgisi kaydedilir. Pencere/fare testi, gerçek telefon/dokunma testi olarak işaretlenmez.

**Yeni oyun** seçilir. Gözlemci doğru yolu veya çözümü önceden göstermez. Başlangıç açıklaması: “Bu mahallede Ada ve Efe ile oynayacaksın. Etrafına bakabilir, eşyaları deneyebilirsin.” Oyuncu durmak isterse oturum durdurulur ve tamamlanmadı olarak kaydedilir.

Kronometre Ada'nın ilk kontrolü açıldığında başlar, aile buluşması sonrası **Senin yolun** açıldığında biter. Duraklama ve gözlemcinin oyun dışı açıklaması birer zaman aralığı olarak kaydedilir; çakışan aralıklar iki kez çıkarılmaz. Oyuncunun düşünmesi, çevreyi incelemesi ve yanlışını kendi düzeltmesi oynanış süresidir. Oyun içi ipucu ve ekip desteği dışarıdan yardım sayılmaz.

İlk üç temel hareket ayrı gözlenir: zemine dokunarak yürüme, gündelik kutuyu alma, kutuyu işaretli yere yerleştirme. Oyuncu her birini gözlemci açıklaması olmadan tamamladıysa bağımsız olarak işaretlenir. Bir hareket için dışarıdan çözüm verildiyse cümle ve an kaydedilir; o hareket bağımsız sayılmaz.

Her bölümde şu gözlemler yazılır: oyuncunun neyi denediği; 30 saniyeden uzun kararsızlığın başladığı nesne; görünmeyen veya tutmayan hedef; anlaşılmayan jest; tekrar etmek istediği olay; bırakmak istediği an. Sorun çözülürse denenen çözüm de yazılır. Bu duraksamalar süreyi uzatan başarılar olarak değerlendirilmez.

## Oyun sonu

Önce açık sorular sorulur:

1. “Başta yaptığın bir şey daha sonra neyi değiştirdi?”
2. “Başka hangi hareketinin sonucunu gördün?”
3. “Bir kez daha oynasan neyi farklı denersin?”
4. “En çok hangi yeri oynamak istedin? Nerede sıkıldın veya zorlandın?”

En az üç ayrı doğru neden–sonuç bağlantısı oyuncunun kendi ifadesiyle yazılır. Oyuncuya doğru kelimeleri söyleyip tekrar ettirmek kanıt sayılmaz. Aile planı, fener, çanta, mobilya, kardeş, komşu, yol ve uzman yardımı arasından kendi fark ettikleri kaydedilir.

## Sonucu değerlendirme

`ClientExports/YanYana/Playtests/observations.template.json` dosyasının kopyası doldurulur. Tamamlanmamış oturumlar silinmez. Testi başlatmış hedef yaş oyuncularının tamamı bağımsız etkileşim oranının paydasına girer. Süre kabulü için en az sekiz tamamlanmış ilk oynayış gerekir; yarıda bırakma ayrıca açık sorun olarak kalır.

`Tools/YanYana/assess_first_play.py <doldurulmuş.json>` gözlenen veriden rapor üretir. Boş alanları sıfır veya başarı kabul etmez. Aynı sürümde en az sekiz tamamlanmış oturum, 28–35 dakika medyanı, en az %80 bağımsız temel etkileşim ve her oyuncunun en az üç doğru bağlantısı ayrı ayrı kontrol edilir. Sekiz oyuncuda %80 için en az yedi bağımsız oyuncu gerekir.

Eğlence, kamera kalitesi ve anlatımın anlaşılması gözlem notlarıyla değerlendirilir; yalnız bu sayıları geçmek bunların otomatik onayı değildir. Süre kısa ise yeni oynanabilir problem gerekir. Süre uzunsa görünürlük, anlaşılma ve tekrar yükü incelenir. Bekleme eklenmez.

Gerçek Android dokunması, arka plana alıp dönme ve 30 dakikalık p95/bellek ölçümü bu oturum raporundan ayrı tutulur. Hedefler: 60 FPS, p95 ≤20 ms, bellek <650 MB.
