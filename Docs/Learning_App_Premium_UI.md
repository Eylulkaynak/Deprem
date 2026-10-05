# KTMMOB İMO öğrenme arayüzü revizyonu

Çalışma dizini `D:\DepremIMO\Deprem`, dal `MergeBranch`, Unity `6000.0.58f2`.

Özgün birleşme görevi `01a0f40e-bc28-7503-b8c5-536a7da78084` ile uygulama öncesinde desteklenen görev mesajlaşması üzerinden doğrudan koordinasyon kuruldu. Görev, entegrasyonun tamamlandığını, aktif düzenlemesi olmadığını ve UI alanlarının devredildiğini doğruladı. Mevcut untracked `Assets/LearningApp` entegrasyonu üzerinde çalışıldı; commit/reset/clean yapılmadı.

## Uygulanan tasarım

- Resmi iki dilli KTMMOB İMO logosu yalnızca ana öğrenme sayfasının kaydırılabilir alt bölümünde gösterilir; sabit üst başlıkta ve diğer ekranlarda yer almaz. Üst başlık sayfa adını ve açıklamasını gösterir. Kaynaklar `Assets/LearningApp/Resources/LearningApp/Brand/SOURCES.md` içinde.
- Açık zemin, kurumsal lacivert, yeşil ilerleme, sıcak ödül ve seri tonları; mevcut Nunito yazı ailesi ve ders/oyun çizimleri.
- Öğrenme yolunda üç derslik bölüm başlıkları, tamamlanan/mevcut/kilitli adımlar, gerçek kayıttan gelen XP/seri/yıldız göstergeleri.
- Kaldığı derse veya sıradaki açık adıma bağlanan ana görev düğmesi; mevcut kayıt/checkpoint davranışı korunur.
- Büyük dokunma alanları, görselli dört sekmeli alt gezinme, kısa ekran geçişleri ve düğme basma durumları.
- 3D macera ve kısa oyun kartları, ders cevap geri bildirimi, net vektör yıldızlı ödül ekranı ve profil kartı.
- Logo dokularının oranı korunur; mipmap/sıkıştırma ve power-of-two yeniden ölçekleme marka kaynakları için kapatılır.

Ana değişiklikler: `LearningStyles.uss`, `LearningAppController.cs`, yeni `LearningPresentation.cs`, `LearningBrandImporter.cs`, `LearningPresentationReview.cs` ve `Brand` kaynakları. `LearningCatalog.cs`, `LearningProgress.cs`, `LearningGameBridge.cs`, `LearningMiniGames.cs`, mevcut 3D sahneler ve sahne listesi bu görsel görevde değiştirilmedi. UI iskeleti ve başlangıç sahnesi mevcut entegrasyon üzerinden çalışır.

## Tekrarlanabilir doğrulama

1. `Tools > Deprem App > Review > Play App`.
2. `Tools > Deprem App > Review > Run Runtime Verification`: ayrı test kaydı, dört sekme, dersler, sekiz mini oyun, üç 3D sahneye giriş/dönüş ve ilerlemenin korunması.
3. `Tools > Deprem App > Review > Premium UI Screenshots and Layout`: ayrı test kaydı, gerçek Game View görüntüleri, 1080×1920 / 1080×2340 / 750×1334 / 1536×2048 çözünürlükleri, logo ve menü görselleri, dokunma hedefleri, başlık/kaydırma alanı/alt menü sınırları, görev CTA'sı, ders önizlemesi, doğru cevap ve +30 XP ödülü. Önceki kayıt kaynağı ve Game View seçimi sonunda geri yüklenir.
4. EditMode filtresi: `Deprem.Learning.Tests.LearningAppTests`.

Sonuçlar `ClientExports/DepremApp/PremiumUI/Reports`, gerçek ekran görüntüleri `Before` ve `After` altındadır. Görüntüler Unity `ScreenCapture.CaptureScreenshot` çıktısıdır; çizilmiş mockup veya tarayıcı önizlemesi değildir.

Karşılaştırma için `Before/education-1080x1920.png` önceki entegrasyonun sıfır ilerleme ekranı, `After/education-1080x1920.png` yeni tasarımın sıfır ilerleme ekranıdır. `Before/education-live.png` bu görevin düzenleme öncesinde bizzat aldığı canlı görüntüdür; onun ilerlemesi `After/education-progress.png` ile karşılaştırılabilir.

Bu doğrulamalar Windows Unity Editor içindir. Android/iOS oyuncu derlemesi, fiziksel cihazdaki çentik/klavye davranışı, mağaza yayını ve cihaz performans ölçümü yapılmadı. Entegrasyon notlarındaki Android Build Support eksikliği bu görevde giderilmedi.

## Tamamlanan son QA

- Unity script derleme yanıtı başarılı; `Reports/compile-response.json`.
- EditMode **10/10 geçti**, 0 hata/atlanan test; son turdaki test logları boş. `Reports/editmode-response.json` içindeki `testCount` paket/suite düğümlerini de sayar; yaprak test sayısı 10'dur.
- Son PlayMode turunda dört sekme, ders yanlış cevap/tekrar/checkpoint/tamamlama, yetişkin modu, sekiz mini oyun ve üç 3D sahnenin her birinden kayıt korunarak dönüş geçti. Hata/istisna yok: `Reports/runtime-verification.txt`.
- Dört çözünürlük, CTA, önizleme, doğru yanıt görünümü ve +30 XP ödülü geçti: `Reports/layout.txt`.
- `Delivery/01-deprem-once.png` ve `Delivery/02-deprem-sonra.png` aynı 30 XP / 1 ders durumunun canlı önce/sonra görüntüleridir. Diğer dört teslim görüntüsü oyun kartları, ödül ekranı, 3D macera ve test sonunda eğitime dönmüş 220 XP / 24 yıldız profilidir.

Library'nin mevcut becerisindeki desteklenen toplu yükleme yolu denendi; yerel ortam hazırlama işlemini sunamadığı için yükleme başarısı veya Library kimliği doğrulanmadı. Teslimler yerel PNG olarak korunur; indirme adresi uydurulmadı.
