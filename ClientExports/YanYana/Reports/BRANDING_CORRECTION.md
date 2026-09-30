# Deprem — isim ve logo düzeltmesi

Oyunun mevcut **DEPREM** adı ve `Assets/Story/UI/Brand/DepremLogo.png` logosu açılışa geri getirildi. Yeni 3D mahalle, karakterler, yazı tipleri ve arayüz düzeni korundu. Android uygulamasının görünen adı ve inceleme paketlerinin isimleri de Deprem olarak düzeltildi.

![Unity Play açılışı](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Screenshots/interface/deprem-title-960.png)

- [Windows inceleme paketi](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Deprem-Windows-Inceleme-20260914-191458.zip): arşivi çıkarıp `Deprem/Oyna.cmd` dosyasını açın.
- [Android APK](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Android/Deprem-Android-Inceleme-20260914-191054.apk)

Unity Play'de 540×960 açılış görüntüsü kontrol edildi. Windows derlemesi sıfır hata ve uyarıyla tamamlandı; bağımsız oyuncunun başlangıcı doğrulandı. Android paketinde uygulama adı dahil dokuz paket ve imza kontrolü geçti. 10443 başlangıç dosyası byte düzeyinde aynı; yeni runtime C# eklenmedi.

İç sahne adı, kayıt anahtarları ve uygulama kimliği değişmedi. Bu düzeltmede yalnız marka sunumu değişti. Önceki oyun akışı, dört final ve üç ekran oranı kanıtları `interface-review.json` içindeki `0134fbf5e79dca01e532e283cbb3c0b1a8832ed73d3478b20c57941d26d27a3d` sahnesine aittir; isim düzeltmesi için bu uzun kontroller yeniden yapılmış olarak raporlanmıyor. Mevcut sahnenin SHA256 değeri `6950edaa3a2eb06666a0ef394b0a469edba23f87877eb8c78232bff6c5203aec`.

Gerçek Android cihazı ve hedef yaş oyuncu doğrulamaları önceki rapordaki açık koşullar olarak devam ediyor.
