# Deprem — Android inceleme paketi

**Deprem-Android-Inceleme-20260915-012748.apk** — 108.4 MiB. Android 8.0 veya üzeri, ARM64 telefon/tablet için hazırlanmıştır. Tek sahne: `YanYana_Adventure`. Paket kimliği `com.yanyana.adventure.review`; yerel inceleme için Android debug sertifikasıyla imzalıdır.

APK derlemesi başarılı. Paket manifesti, ZIP bütünlüğü, ARM64 IL2CPP içeriği ve imzası kontrol edildi. SHA256: `de678b72b9374b6b48dc4e9e79a619a5a6b84fea68eae9fb55061865c6f663b6`. Build uyarı sayısı: 1. Kanıtlar `android-build.txt`, `android-package.json`, `android-apk-manifest.txt` ve `android-apk-signature.txt` dosyalarındadır.

Bu derlemede mevcut proje kodundaki `OnMouse_` işleyicileri için bir mobil performans uyarısı bulunuyor. Mevcut runtime dosyaları değiştirilmedi; bu uyarının gerçek cihazdaki etkisi henüz ölçülmedi.

APK'yı Android cihaza aktararak açabilirsiniz. Android kurulum izni isterse yalnız APK'yı açtığınız uygulamaya gerekli izni verin. USB ile test yapılacaksa SDK içindeki ADB ile kurulum komutu: `adb install -r "Deprem-Android-Inceleme-20260915-012748.apk"`.

Kaynak projede Android platformuna geçilmedi; derleme bağımsız kopyada yapıldı. Mevcut Windows inceleme paketi korunuyor. Bu APK'nın oyun sahnesi SHA256 değeri `b180da44ad7525a1562b08f47aee55a5cc16f64601bdf29034f2f5b24a6d3231`.

**Cihaz testi yapılmadı.** Kullanıcı şu anda Android telefonu olmadığını belirtti; bağlı cihaz yok. APK'nın cihazda açılması, gerçek dokunma, arka plan/ön plan, dört final ve 30 dakikalık FPS/p95/bellek ölçümü açık doğrulama koşullarıdır. Sekiz çocukla ilk oynayış ve 28–35 dakika medyanı da henüz doğrulanmadı.
