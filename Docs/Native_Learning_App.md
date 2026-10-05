# Yerel Unity uygulaması

Başlangıç sahnesi: `Assets/LearningApp/Scenes/Deprem_App.unity`.

Uygulama artık Unity UI Toolkit ve C# ile çalışır. Açılışta eğitim yolu görünür; Oyunlar sekmesinden hem yerel kısa oyunlara hem de mevcut 3D sahnelere geçilir. Dersler ve tek oyunculu oyunlar internet bağlantısı gerektirmez. Arkadaşlar, çevrimiçi yarışlar ve sıralama ortak sunucuya bağlanır; WebView veya tarayıcı kullanılmaz.

## Kullanım

1. Unity 6000.0.58f2 ile projeyi aç.
2. `Tools > Deprem App > Open Native App` menüsünü seç ve Play'e bas.
3. Eğitim yolundan ders aç; üstteki Çocuk/Yetişkin düğmesiyle modu değiştir.
4. Oyunlar içinden Yan Yana, Deprem Hikâyesi veya 3D Pratik Alanı'nı aç.
5. 3D sahnenin sağ altındaki **Eğitime dön** düğmesi uygulamaya geri getirir.

`Deprem_App` Build Settings'te ilk sahnedir. Mevcut hikâye ve mini oyun sahneleri korunmuştur; `YanYana_Adventure` da listeye eklenmiştir.

## Aktarılan içerik ve davranış

- Kaynak uygulamadaki 21 çocuk eğitim ünitesi ve 28 yetişkin ders/test kartı.
- Bilgi kartı, doğru/yanlış geri bildirimi, üç can, tekrar deneme, sıralı ünite açma ve dersin kaldığı adımdan devam etmesi.
- Çantayı Hazırla, Eşyaları Ayır, Eşleri Bul, Doğru Davranışı Seç, Tehlikeleri Bul, Çanta Yakala, Doğru Sırala ve Sırayı Hatırla; iki yaş modunun içeriği.
- Çanta hazırlama ve ayırmada dokunma veya sürükleme; eşleştirme ve sıralamada sırayla dokunma; hafıza oyununda animasyonlu gösterim; yakalama oyununda parmakla hareket eden çanta.
- 16 rehber kartı, 26 başarım, 6 hazırlık planı alanı ve 12 kontrol listesi maddesi.
- Yerel aile profilleri, kişi başına ders/oyun/plan kaydı, cihaz içi sıralama, yıldızlar, XP, günlük seri ve yanlış cevaplar. Arkadaşlar sekmesinde arkadaş kodu/istekleri, iki kişilik çevrimiçi mini oyun yarışları ve sunucu sıralaması. Kurulum: `Learning_App_Online.md`.
- Müzik, efekt ve ses seviyesi ayarları; Türkçe font; dikey ekran ve güvenli alan düzeni.
- Türkçe kadın sesiyle çevrimdışı anlatım: çocuk dersleri, sorular, geri bildirimler ve oyun yönergeleri; yetişkin dersleri ve rehber kartlarında isteğe bağlı dinleme. Profil başına aç/kapat, otomatik okuma ve anlatıcı ses düzeyi. Ayrıntılar: `Learning_App_Narration.md`.

Bu, kaynak uygulamanın içeriklerini kullanan **yerel yeniden uygulamadır**. Phaser'ın piksel düzeni ve bütün geçiş animasyonları bire bir kopyalanmamıştır. Dersler ve tek oyunculu oyunlar çevrimdışı çalışır; Arkadaşlar, çevrimiçi yarışlar ve sıralama ortak bir sunucuya bağlanır. Aile profilleri Arkadaşlar içindeki ayrı düğmeden seçilir. Capacitor'ın işletim sistemi bildirim zamanlayıcısı taşınmamıştır; günlük seri uygulama içinde çalışır. Eski tarayıcı/Capacitor localStorage kayıtları Unity'ye otomatik aktarılmaz.

## Kayıt ve 3D oyunlar

Eğitim verisi `Application.persistentDataPath/learning-progress-v1.json` dosyasına yazılır. Önceki geçerli nesil `.bak` dosyasında tutulur; bozuk ana kayıt varsa yedek okunur. Ders tekrarı XP'yi tekrar kazandırmaz; en iyi oyun puanı ve yıldız sayısı düşmez. Profil değiştirmek diğer kişilerin verisini silmez.

3D oyunlar kendi mevcut kayıt sistemlerini kullanır. Eğitim profilini değiştirmek Yan Yana/Story kayıtlarını değiştirmez; 3D kayıtlar henüz aile profillerine ayrılmamıştır. Dönüş onayı bunu kullanıcıya bildirir. Sahne adları sabit bir izinli listeden çözülür; sahne yüklenirken ikinci geçiş engellenir.

## Dosyalar

- `Assets/LearningApp/Scripts`: yerel ekranlar, dersler, sekiz mini oyun, kayıt ve sahne geçişleri.
- `Assets/LearningApp/Resources/LearningApp`: içerik kataloğu, orijinal görseller, müzik, UXML/USS ve panel ayarı.
- `Assets/LearningApp/Editor`: sahne oluşturma, doğrulama, Android build menüsü, editör testleri ve çalışma zamanı kontrol araçları.
- `Tools/LearningApp/import-content.mjs`: kaynak projeden içeriği deterministik olarak çıkarır. Çalıştırma: `node Tools/LearningApp/import-content.mjs "C:/Users/ataor/Desktop/Deprem/portrait-puzzle-demo"`.

İçe aktarıcı masaüstündeki kaynak projeyi değiştirmez. JSON içerikleri Unity Inspector dışından düzenlenebilir; yeniden içe aktarma katalog ve ithal görsellerin üzerine yazar. C# ekranlarını veya kullanıcı kayıtlarını değiştirmez. SVG süslemeler yerine yerel UI stilleri kullanılmıştır.

## Doğrulama ve mobil derleme

- `Tools > Deprem App > Validate Native App`: içerik, görseller ve başlangıç sahnesi denetimi.
- EditMode test filtresi: `Deprem.Learning.Tests.LearningAppTests`.
- Play modundayken `Tools > Deprem App > Review > Run Runtime Verification`: ayrı bir geçici kayıtla gerçek UI düğmelerini, ders akışını, mini oyunları ve 3D gidiş/dönüşleri çalıştırır. Rapor `.codex_tmp/learning-app/runtime-report.txt` içine yazılır.
- `Tools > Deprem App > Build Android APK`: etkin sahneleri `Builds/DepremApp/DepremApp.apk` içine derler.

Bu bilgisayardaki Unity 6000.0.58f2 kurulumunda **Android Build Support modülü yok**. APK üretimi için aynı editör sürümüne Unity Hub üzerinden Android Build Support, SDK/NDK ve OpenJDK kurulmalıdır. Bilgisayarda ayrı Android SDK bulunması Unity modülünün yerini tutmaz. iOS için uygun Unity iOS desteği ve macOS/Xcode gerekir. Mağaza imzalama ve gerçek cihaz testi bu editör entegrasyonundan ayrı yapılmalıdır.

### 1 Ekim 2026 doğrulama sonucu

- EditMode: **10/10 test geçti**. XML sonuç: `ClientExports/DepremApp/Reports/EditMode.results.xml`.
- Yerel uygulamanın dört sekmesi, plan formu, kontrol listesi, rehber, başarımlar, ses ayarları ve yanlış cevap ekranı açıldı.
- Bir çocuk dersinde yanlış cevap, tekrar deneme, sahne yeniden açıldıktan sonra kaldığı adıma dönme ve tamamlama doğrulandı. Yetişkin moduna geçiş ve ders tamamlama doğrulandı.
- Sekiz mini oyunun tamamı gerçek UI düğmeleriyle; yakalama oyunu işaretçi olaylarıyla tamamlandı.
- Üç 3D girişin tamamı Oyunlar sekmesindeki düğmeyle açıldı; sahnedeki dönüş düğmesi ve onayı kullanılarak eğitim kaydı korunarak dönüldü. Zaman ölçeği normale döndü.
- Bu son çalışma zamanı turunda **hata veya istisna kaydı oluşmadı**. Rapor: `ClientExports/DepremApp/Reports/runtime-verification.txt`.
- 1080×1920 ve 1080×2340 portre görünümünde alt menü ekran içinde kaldı. Ekran görüntüleri: `ClientExports/DepremApp/Preview/`.
- Bunlar Windows üzerindeki Unity Editor sonuçlarıdır; Android/iOS cihaz ve oyuncu derlemesi henüz doğrulanmamıştır.
