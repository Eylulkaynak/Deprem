# Baştan finale teknik oynayışlar

Dört kayıt aynı güncel sahnede, son aile buluşması kameraları, kardeşin toplanması ve kayıttan devam düzeltmeleriyle tamamlandı. Önceki alev, nesne yerleştirme ve kalıcı deprem animasyonu bağlantıları korunuyor. Sahne SHA256: `7f5489c855add426bdd7af90882431a25acec59816b173ff900035086b972c83`. Kayıt eşleştirmeleri `current-route-recordings.json` dosyasındadır.

| Son | Son tam kayıt | Sonuç | Süre |
|---|---|---|---|
| 2 — Söz Verdiğimiz Yerde | [Hazırlıklı tam rota](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Recordings/physical-playthrough-20260914-091228.mp4) | Aile planı, kullanılabilir fener/radyo; `Ending=2`, `Reunited=1` | 224.1 sn |
| 3 — Komşu Eli | [Komşu tam rotası](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Recordings/physical-playthrough-20260914-090927.mp4) | Eksik hazırlık, acil ışık, görevli alıcısı ve Yusuf'la yürüme; `Ending=3`, `Reunited=1` | 147.1 sn |
| 4 — Sesimizi Duydular | [Görevli desteğiyle tam rota](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Recordings/physical-playthrough-20260914-091634.mp4) | Komşuyu görevliye bildirme ve aile bilgisini doğrulama; `Ending=4`, `Reunited=1` | 144.9 sn |
| 1 — Yeni Yolda Birlikte | [Alternatif alan tam rotası](C:/Users/Gokturk/Documents/GitHub/Deprem/ClientExports/YanYana/Recordings/physical-playthrough-20260914-091931.mp4) | Ekip desteği ve alternatif güzergâh; `Ending=1`, `Reunited=1` | 85.8 sn |

Önceki kayıtlar ve başarısız denemeler korunmuştur. Son test çıktıları `physical-recorded-route-1.txt`–`physical-recorded-route-4.txt` dosyalarındadır; tarihli kopyaları da saklanmıştır.

Yollar gerçek Yeni Oyun düğmesinden başlar. Karar, envanter ve yangın sağlığı test kodundan atanmaz. Üretim EventSystem olayları, zamana yayılan sürüklemeler, sahnedeki gerçek yürüyüşler ve rol geçişleri kullanılır. Yürüme hedeflerinde mevcut hareket bileşeni çağrılır. Dört son kendi buluşma etkileşimiyle tamamlanmıştır.

Kayıtlar hızlandırılmamıştır; çözümleri bilen teknik otomasyondur. Hedef yaş oyuncularda 28–35 dakika medyanı, %80 bağımsız ilk etkileşim, anlama ve eğlence gözlemi yerine geçmez. Gerçek telefon ve bağımsız Windows oyuncusunda tam elle tur ayrı kabul koşullarıdır. Build ve korunma bilgisi `REVIEW_DELIVERY.md` dosyasındadır.
