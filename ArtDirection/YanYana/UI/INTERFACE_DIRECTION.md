> Güncel oyun adı **Deprem**. [İsim düzeltmesi ve güncel paketler](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Reports/BRANDING_CORRECTION.md). Aşağıdaki arayüz ve oynanış kontrolleri isim düzeltmesinden önceki sürüme aittir.

# Deprem — arayüz yenileme kaydı

Açılış, gerçek Ada ve Efe modellerinin bulunduğu bir mahalle avlusuna taşındı. Soluk tam ekran kart ve portre kolajı kaldırıldı. Büyük slogan yerine oyun adı, kısa karakter satırı ve tek ana eylem var.

![Unity Play açılışı](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/interface/title-960.png)

Başlıklarda Lilita One, konuşma ve görevlerde Lexend kullanılıyor. Türkçe görev metinleri Lexend'in tam Türkçe karakter setiyle çiziliyor; Lilita'nın eksik karakterleri için Lexend desteği var. Font lisansları `Assets/YanYana/UI/Fonts/Licenses/` içinde. Arayüz mürekkep yeşili, sıcak beyaz ve sarı kullanıyor. Menü karakterleri resim değil; Animator ile hareket eden sahne modelleri. Menü kapandığında açılış dünyası kapanır, kamera oyuna kesmeyle geçer.

Oyun içi hedef ve konuşma alanları küçültüldü; duraklatma, rol geçişi, sonuç ve tekrar ekranlarının yazıları ve düğmeleri aynı stile getirildi. Menülerin arkasında HUD görünmez ve dokunma almaz.

![Unity Play oyun içi arayüz](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/interface/gameplay-960.png)

## Kontrol edilenler

- 540×960, 540×1170 ve 540×1200 çözünürlüklerinde açılış ve HUD. Metin taşması yok; iki açılış karakterinin yüzleri kadrajda.
- Üretim EventSystem üzerinden 17 arayüz hedef kontrolü. Yeni oyun, duraklatma, geri dönüş ve kayıt yükleme geçti.
- Yeni sahnede hazırlıktan son buluşmaya kadar bir tam hazırlıklı rota tamamlandı. [Normal hızlı teknik kayıt](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Recordings/physical-playthrough-20260914-184540.mp4). Bu kayıt çocukların ilk oynama süresi değildir.
- Dört finalin buluşma etkileşimleri üç ekran oranında tamamlandı: 48 hedef, 72 yüz kontrolü. Bu, dört farklı tam başlangıç rotasının yeniden test edildiği anlamına gelmez.
- 10443 başlangıç dosyası aynı. Yeni runtime C# yok. Kayıt, testten önce yedeklendi ve sonra doğrulanarak geri getirildi.
- Windows derlemesi ve bağımsız başlangıcı geçti. Android ARM64 IL2CPP APK derlendi; sekiz paket/imza kontrolü geçti.

Sahnede 144 ScriptMachine, 41479 yerleşik grafik birimi var. Düzenlenebilir arayüz ve açılış sahnesinin üretim kaynağı: [YanYanaInterfaceArtDirection.cs](C:/Users/Gokturk/Documents/GitHub/Deprem/Assets/YanYana/Editor/YanYanaInterfaceArtDirection.cs). Bu dosya yalnız Editor içindir. Sahne yeniden üretildiğinde yeni görünüm korunur.

## Güncel inceleme dosyaları

- [Windows inceleme paketi](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/YanYana-Windows-Inceleme-20260914-190016.zip)
- [Android APK](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Android/YanYana-Android-Inceleme-20260914-184348.apk)
- [Arayüz giriş kontrolleri](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Reports/interface-input-qa.txt)
- [Dört finalin ekran kontrolleri](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Reports/physical-reunion-screen-targets.txt)

Her iki paket bu sahneden üretildi: `0134fbf5e79dca01e532e283cbb3c0b1a8832ed73d3478b20c57941d26d27a3d`.

Gerçek Android cihaz ve hedef yaş oyuncu testleri yapılmadı. FPS, bellek, 28–35 dakika ve çocukların anlama ölçütleri hâlâ bu testleri gerektiriyor. Bu teslim arayüz yenilemesini ve teknik inceleme sürümlerini kapsıyor.
