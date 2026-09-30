# Yan Yana — 14 Eylül, 00:28 inceleme sürümü

Paket: `ClientExports/YanYana/YanYana-Windows-Inceleme-20260914-002833.zip`. ZIP'i bir klasöre çıkarıp `YanYana/Oyna.cmd` dosyasını çalıştırın. Güncel akışı görmek için başlangıç ekranında **Yeni oyun** seçin. Dikey pencere açılır; oyun çevrimdışı çalışır ve build yalnız `YanYana_Adventure` sahnesini içerir.

Bu sürüm tamamlanmış, çocuklarla doğrulanmış 30 dakikalık oyun olarak sunulmuyor.

Efe'ye el ile karşılık verme veya çıkışı kontrol etmeye geçme artık ayrı seçimler. Efe desteklenmiş ve aile planı öğrenilmişse yardım masasına yürüyüp ağaç kartını yerleştiriyor, sonra yana çekiliyor. Oyuncu kalan aile portrelerini eşleştiriyor. Diğer yollarda iki bilgiyi oyuncu karşılaştırıyor. Masa çocuk yüksekliğine indirildi; kamera dönüşü ve kayıt sonrası giriş kilitleri düzeltildi.

Çanta fermuarı, iki askı, gerçek taşıma, ambalaj incelemesi, fener, radyo, mobilya yardımı, üç korunma hareketi, tahliye, yetişkin itfaiyeci/görevli rolleri ve karar tekrarları da bu sürümde bulunuyor.

Güncel baştan sona kayıtlar:

| Yol | Video | Sonuç |
|---|---|---|
| Hazırlıklı; Efe'ye destek; Efe'nin aile işaretine katkısı | `Recordings/physical-playthrough-20260914-001938.mp4` | Final 2, 201,9 sn |
| Eksik hazırlık; çıkışı kontrol etme; ekip desteği ve görevli alıcısı | `Recordings/physical-playthrough-20260914-002330.mp4` | Final 1, 66,0 sn |

Bu süreler çözümleri bilen otomasyona aittir. Gerçek yürüyüş, üretim EventSystem girişleri ve zamana yayılmış sürüklemeler kullanıldı; karar, envanter, yangın sağlığı veya pozlar test kodundan atanmadı. Kayıtlar normal hızdadır; çocuk ilk oynayış süresi veya eğlence kanıtı değildir. Komşu ve görevli finallerinin eski tam kayıtları ile başarısız/durdurulmuş denemelerin ayrımı `four-full-routes.md` içindedir.

`physical-sibling-support.txt` ve `physical-efe-family-contribution.txt` yanlış bırakma, ikinci parmak, çift duraklatma, yarım/tamamlanmış eylemi yükleme ve kamera dönerken giriş reddini kapsar. Aile testi izole giriş durumu kullanır; tam yol kayıtları bunu kullanmaz. Ayrıntı: `sibling-integration.md`.

Sahne 132 native ScriptMachine ve 34.586 grafik birimi içeriyor. Yeni C# yalnız Editor klasöründedir; yeni runtime C# yazılmadı. 109 kısa sentetik Türkçe konuşma yereldir. Karakterler onaylanan proje modellerinden ayrı kaynaklarla türetilmiştir; sıfırdan yontulmuş yedi model olarak tanımlanmaz. Düzenlenebilir sanat kaynakları `ArtDirection/YanYana` altındadır.

Windows build'i sıfır hata/uyarıyla oluştu; bağımsız açılış ve NavMesh başlangıcı geçti. Paketin CRC/SHA256 denetimi tamamlandı. Bağımsız oyuncuda yalnız açılış sınandı; tam etkileşim yolları Unity Editör'de sınandı. Başlangıçta ölçülen Windows çalışma kümesi 692.162.560 bayt (yaklaşık 660 MiB); bu tek örnek Android bellek ölçümü veya cihaz kabulü değildir ve optimizasyon takibine eklendi.

Başlangıçtaki 10.443 proje dosyası aynı kaldı. Build sırasında URP/TMP'nin yazdığı altı eski dosya tam çalışma kopyalarıyla geri kondu. Yeni macera klasörleri dışındaki yeni Visual Scripting editör önbellekleri doğrulanmış kopyalarıyla arşivlendi; beklenmedik yeni dosya kalmadı.

Kalanlar: gündelik başlangıç/taşıma öğretisi, Yusuf'un yaş/baston sunumu, altı arka plan varyasyonu, bütün parmak/nesne temasları ile animasyon ve efektlerin son görsel incelemesi. En az sekiz hedef yaş oyuncusunda 28–35 dakika medyanı, anlama ve eğlence; gerçek telefon dokunması, Android build ve cihaz performansı henüz doğrulanmadı. Kapsam: `ArtDirection/YanYana/PHYSICAL_ADVENTURE_STATUS.md`.
