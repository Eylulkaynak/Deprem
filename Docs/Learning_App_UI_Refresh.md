# Arayüz revizyonu · 1 Ekim 2026

Uygulamanın ortak UI Toolkit teması sıcak beyaz, petrol yeşili ve kayısı tonlarıyla yenilendi.

| Kullanım | Renk |
| --- | --- |
| Zemin | `#F7F8F4` |
| Ana metin | `#203E3B` |
| İkincil metin | `#657571` |
| Ana kart | `#204F46` |
| Birincil düğme | `#246858` |
| Ana kart düğmesi | `#F3C795` |
| Seçili yüzey | `#E8F0E7` |

Kalın alt kenarlar kaldırıldı; başlık, açıklama, kart ve düğme aralıkları düzenlendi. Üç ayrı renkli istatistik kartı tek şeritte birleştirildi. Alt gezinmede aynı ölçekte çizilen vektör ikonlar kullanıldı. Ders satırlarının tamamı düğme oldu; kilitli derslerin devre dışı kalması korundu. Profil, aile, form, ders, geri bildirim ve modal ekranları ortak paleti kullanıyor. Tamamlanan yolculuğun rozeti tekrar XP vadetmiyor.

Ana dosyalar:

- `Assets/LearningApp/Resources/LearningApp/LearningStyles.uss`
- `Assets/LearningApp/Scripts/LearningPresentation.cs`
- `Assets/LearningApp/Scripts/LearningAppController.cs`
- `Assets/LearningApp/Editor/LearningPresentationReview.cs`

Aynı çalışma alanındaki çocuk oyunlarını sadeleştirme değişiklikleri korunmuştur. Oyunların ayrı `VisualPlay.uss` dosyası üzerindeki etkileşim kuralları değiştirilmedi; ortak temada oyun menüsü kartlarının yüzey renkleri eşlendi.

Doğrulama menüsü: `Tools > Deprem App > Review > Premium UI Screenshots and Layout` (Play modunda). Ayrı bir test kaydı kullanır; sonunda önceki kayıt kaynağını ve Game View çözünürlüğünü geri yükler. 1080×1920, 1080×2340, 750×1334 ve 1536×2048 düzenleri; dört sekme; ders satırı ve ana düğmeden önizleme; doğru cevap; tamamlama; 30 XP ve tekrar rozeti kontrol edilir.

Güncel sonuçlar `ClientExports/DepremApp/ForestUI/Reports/layout.txt`, gerçek Unity ekran görüntüleri `ClientExports/DepremApp/ForestUI/After/` klasöründedir. Eski `PremiumUI` görselleri korunmuştur. Script derlemesi başarılıdır; üç mevcut YanYana/TMP eskime uyarısı vardır. Kontroller Unity Editor üzerinde yapılmıştır; fiziksel mobil cihaz testi yapılmamıştır.
