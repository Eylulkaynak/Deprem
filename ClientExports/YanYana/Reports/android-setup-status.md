# Android kurulumu — 14 Eylül 2026

**Android araçları kuruldu ve çalıştırılarak doğrulandı.** Unity **6000.0.58f2**, revizyon `92dee566b325` korunuyor. Başka bir Unity sürümü kurulmadı.

Kurulu bileşenler: Android Build Support, OpenJDK **17.0.9+9**, Android NDK **r27c**, CMake **3.22.1**, SDK Build Tools **34.0.0**, Platform Tools **34.0.5**, SDK platformları **34/35/36** ve Command-line Tools **16.0**. Java, Clang, CMake, AAPT2 ve ADB sürüm komutları başarılı çıktı. SDK lisansları kurulum sırasında kaydedildi. Ayrıntılı kanıt: `android-toolchain-verification.json`.

Kurulum, kullanıcının PC başında verdiği açık kurulum yetkisiyle tamamlandı. Hub **3.20.0** önce eski UAC hatasını tekrar gösterip kapandı. Resmî Unity CLI **1.0.0-beta.9**, Windows paket yöneticisinden kuruldu; bu sürüm Hub'ın kurulum veritabanında eksik `writer_kind` sütunu nedeniyle dosyaları yerleştiremedi. Veritabanına müdahale edilmedi.

Unity'nin imzalı Android kurucusu doğrudan çalıştırıldı; imzası ve Unity modül manifestindeki MD5 değeri doğrulandı. Unity/Google sunucularından indirilmiş SDK, NDK ve JDK arşivleri doğrulanarak kurulu Editörün `Editor/Data/PlaybackEngines/AndroidPlayer` klasörüne yerleştirildi. Bütün ZIP girdilerinin CRC kontrolü yapıldı; mevcut resmî özetler ayrıca kontrol edildi. Platform Tools 34.0.5 ve Platform 35'in bu revizyonu güncel Google indeksinde bulunmadığından bu iki arşiv için bağımsız indeks özeti doğrulaması raporlanmıyor. Arşiv kaydı `.codex_tmp/yanyana_implementation/android-stage-current.json` içinde.

[Unity'nin bileşen kurulum belgesi](https://docs.unity3d.com/6000.0/Documentation/Manual/InstallingUnity.html) ve [resmî Unity CLI belgesi](https://docs.unity.com/en-us/unity-cli/use-unity-cli) izlenen kurulum yollarını açıklıyor. Hub'ın eski başarısız iş kaydı, kurulu dosyaların veya çalıştırma kontrollerinin yerine geçmez; kurulumun kanıtı yukarıdaki doğrulama raporudur.

Android build, `.codex_tmp/yanyana_android_20260914-173827` altındaki bağımsız proje kopyasında **başarıyla tamamlandı**: 0 hata, mevcut `OnMouse_` işleyicileriyle ilgili 1 mobil performans uyarısı. Kaynak projenin platformu, sahneleri ve kayıt ayarları değiştirilmedi. Yalnız `YanYana_Adventure` sahnesi derlendi. İlk denemede Unity'nin eski özel SDK yolu kullanılıyordu; son build sırasında kurulu Editörün araçları seçildi ve işlem sonunda önceki ortak araç tercihleri geri yüklendi (`android-tool-preferences-restoration.txt`).

APK: `YanYana-Android-Inceleme-20260914-175712.apk`, 111.644.870 bayt. ARM64 IL2CPP içeriği, Android 8/API 26 alt sınırı, API 35 hedefi, ZIP bütünlüğü ve imzası doğrulandı. Ayrıntı ve kurulum bilgisi: [Android inceleme paketi](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Reports/ANDROID_REVIEW.md). Build sonucu `android-build.txt`, paket doğrulaması `android-package.json` içindedir.

Kullanıcı şu anda Android telefonu bulunmadığını belirtti. ADB de bağlı cihaz görmedi. Gerçek dokunma, uygulamayı arka plana alma ve 30 dakikalık FPS/p95/bellek kabulü henüz yapılmadı.
