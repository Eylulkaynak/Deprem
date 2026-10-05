# Okumadan oynanabilen kısa oyunlar

Çocuk modunun sekiz yerel mini oyununda görev, cevap, ilerleme, tekrar ve çıkış için görsel karşılıklar vardır. Kısa oyun adları ve “Geri”, “Göster”, “Devam”, “Tekrar”, “Harika!” gibi yardımcı kelimeler korunur. Uzun açıklamalar azaltılır; arayüzü tüm yazılardan arındırmak hedef değildir. Yetişkin modunun açıklamaları korunur.

- Çanta: eşya → çanta örneği, hareketli el ve dolan ilerleme noktaları.
- Ayırma: çanta ve ev resimli iki hedef; dokunma veya sürükleme.
- Eşleştirme: kullanım amaçları bardak, yol, enerji, yardım, ışık, yara bandı, haber, ısınma ve maske simgeleriyle gösterilir.
- Güvenli davranış: durum resmi ve büyük resimli cevaplar. Kaynakta görseli olmayan seçenekler için vektör çizimler bulunur. Üç tehlikeyi sunup yalnız birini doğru kabul eden dış mekân sorusu, çocuk turunda bir güvenli alan ve iki tehlikeli alan seçeneğine dönüştürülür; katalog değiştirilmez.
- Tehlike bulma: örnek davranış üzerinde çarpı; bulunan tehlikeler çarpıyla işaretlenir.
- Sıralama: oklarla bağlı görsel sıra örneği.
- Hafıza: izlerken göz, sıra oyuncudayken el, yanan kartta el işareti; simgeli tekrar düğmesi. Çıkış onayı gösterimi duraklatır.
- Yakalama: ilk dokunuştan önce çantayı hareket ettiren el gösterimi; oyuncu dokununca düşen eşyalar başlar.

Doğru ve yanlış tepkiler şekillerle ayrılır; yalnız renk veya sese dayanmaz. Mevcut efekt sesleri destekleyicidir, yeni konuşma kaydı eklenmemiştir. Oyun içindeki yanlış seçimde çift olumlu/olumsuz ses çalınması giderilmiştir.

`LearningVisualPlay.cs` ve `VisualPlay.uss` görsel çocuk oyunlarını ayrı tutar. Ana arayüz tasarımıyla eşzamanlı çalışmada `LearningPresentation.cs` ve `LearningStyles.uss` üzerine yazılmaz. Kayıt kimlikleri, puan ve yıldız hesapları korunur.

Doğrulama menüsü: `Tools > Deprem App > Review > Visual Play Verification`. Ayrı bir kayıtla gerçek düğme ve işaretçi olaylarını kullanır; sesleri kapatır; çocuk/yetişkin oyunlarını, çocuk sorularının tümünü, 750×1334 ve 1536×2048 yerleşimlerini denetler. Önceki kayıt kaynağı ve ekran boyutu sonunda geri yüklenir. Rapor ve gerçek Game View görüntüleri `ClientExports/DepremApp/VisualPlay` içindedir.

Bu dosyalar sekiz yerel kısa oyun ve onların seçim ekranını kapsar. 3D sahnelerin devam çalışması `Visual_3D.md` içinde açıklanır. Eğitim dersleri bu değişikliğin kapsamı dışındadır. Okuma bilmeyen çocuklarla gözlemsel kullanılabilirlik testi, ikonların ve örneklerin bağımsız anlaşılabildiğini ayrıca doğrulamalıdır.

## 1 Ekim 2026 doğrulaması

- Sekiz çocuk ve sekiz yetişkin modu gerçek düğme/işaretçi olaylarıyla tamamlandı; ses kapalıydı.
- Çocuk quiz havuzunun bütün soruları, resimler ve vektör seçenekler kontrol edildi.
- 750×1334 ve 1536×2048 boyutlarında sekiz çocuk oyununun her birinde yalnız kısa yardımcı etiketlerin bulunması, görsellerin yüklenmesi, en az 44 birim dokunma alanları ve yatay taşma olmaması doğrulandı. 1080×1920 gerçek ekran görüntüleri de kaydedildi.
- Kısa kelimelerin geri eklenmesinden sonraki son koşum bütün oyun denetimlerini PASS ve akışı COMPLETE olarak kaydetti; çalışma anı hatası görülmedi.
- Bunlar Windows Unity Editor kontrolleridir. Mobil cihaz ve okuma bilmeyen çocuklarla kullanılabilirlik testi yapılmadı.
