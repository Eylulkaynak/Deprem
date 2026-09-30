# Yan Yana — 13 Eylül, 23:10 inceleme sürümü

Paket: `ClientExports/YanYana/YanYana-Windows-Inceleme-20260913-231055.zip`. ZIP'i bir klasöre çıkarıp içindeki `Oyna.cmd` dosyasını çalıştırın. Dikey pencere açılır; başlangıçta **Yeni oyun** seçilebilir. Paket çevrimdışı çalışır ve yalnız `YanYana_Adventure` sahnesini içerir.

Bu sürüm tamamlanmış, çocuklarla doğrulanmış 30 dakikalık oyun olarak sunulmuyor. Hazırlık, korunma, tahliye, yetişkin itfaiyeci ve yardım görevlisi rolleri, dört buluşma yolu, kayıt ve karar tekrarı oynanabilir durumdadır.

Son eklemeler: ambalajları çevirip seçme; fermuarı iz boyunca çekme; iki askıyı ayrı ayarlama; çantayla kapıya yürüme; su ve yiyeceğin çantadan veya masadan gelmesi; on karar girişine kronolojik dönüş. Çanta kapanmadan bırakılan eşyalar sonraki bölümde oyuncunun yanında varsayılmaz.

Normal hızdaki güncel tam kayıtlar:

- Hazırlıklı yol: `Recordings/physical-playthrough-20260913-230107.mp4`, 192,8 saniye, aile planı finali.
- Eksik hazırlık ve ekip desteği: `Recordings/physical-playthrough-20260913-230549.mp4`, 64,1 saniye, alternatif güzergâh finali.

Bu süreler çözümleri bilen otomasyona aittir. Kararlar, envanter, yangın sağlığı veya pozlar test kodundan atanmadı. Nesnelerde üretim EventSystem girişleri, gerçek yürüyüş ve rol değişimleri kullanıldı. Çocuk ilk oynayış süresi veya eğlence değerlendirmesi değildir. Komşu ve görevli finallerinin önceki tam kayıtları ve sürüm sınırları `four-full-routes.md` içindedir.

Kanıt dosyaları:

| Konu | Rapor |
|---|---|
| Fermuar, askılar, gerçek taşıma, iptal ve kayıt | physical-backpack-trial.txt |
| Ambalajlar, eksik/uygun malzeme ve kısmi yardım kaydı | physical-supply-comparison.txt |
| Açık çantanın sonraki yardıma etkisi | physical-unclosed-bag.txt |
| Yeni karar tekrarları, kronoloji ve bağlamsal ipuçları | physical-new-decision-replays.txt |
| Tekrar menüsünün üç dikey oranı | aspect-replay-960.txt, aspect-replay-1170.txt, aspect-replay-1200.txt |
| Windows build, açılış ve paket bütünlüğü | windows-build.txt, windows-startup.log, windows-package.json |
| Eski dosyalar ve yeni dosya kapsamı | build-preservation.txt, new-files-preservation.json |

Yeni C# yalnız `Assets/YanYana/Editor` içindedir; oyuncu akışı native Visual Scripting ve mevcut bileşenlerdedir. Yeni modellerin Blender kaynakları `ArtDirection/YanYana` altındadır. Yedi karakter onaylanan proje karakterlerinden ayrı kaynaklarla türetilmiştir; sıfırdan yontulmuş yedi model değildir. 98 kısa sentetik Türkçe konuşma yerel olarak bulunur.

Mevcut 10.443 dosya başlangıç özetiyle aynı kaldı. Windows build'i sıfır hata/uyarıyla tamamlandı, bağımsız açılış ve NavMesh başlangıcı geçti. Tam etkileşim yolları Unity Editör'de sınandı; Windows oyuncusunda yalnız başlangıç denetlendi.

Kalan işler: ilk gündelik taşıma öğretisi, Efe iletişiminin daha geniş dalları, Yusuf'un yaş/baston sunumu, altı arka plan varyasyonu ve kapsamlı son animasyon/efekt incelemesi. En az sekiz hedef yaş oyuncusunda 28–35 dakika medyanı, anlama ve eğlence; gerçek telefon dokunması, Android build ve cihaz performansı henüz doğrulanmadı. Güncel kapsam `ArtDirection/YanYana/PHYSICAL_ADVENTURE_STATUS.md`, boş katılımcı kayıtları `PlaytestKit` içindedir.
