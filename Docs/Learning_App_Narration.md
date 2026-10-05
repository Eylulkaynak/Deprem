# Türkçe kadın anlatıcı — 1 Ekim 2026

Yerel öğrenme uygulamasının sesli anlatımı tamamen **ElevenLabs / Nisa – Encouraging, Friendly and Soft / Eleven v4** sesine geçirildi. Her metinde `[gently]` yönlendirmesi kullanılıyor. **669 metin eşleşmesinin tamamı 668 benzersiz Nisa kaydına bağlıdır; etkin manifestte önceki ses sağlayıcısına ait kayıt yoktur.** Çalışma anında TTS hizmetine, API anahtarına veya internet bağlantısına ihtiyaç yoktur.

ElevenLabs kuyruğu ve üretim kanıtları `ClientExports/DepremApp/Narration/ElevenLabs` klasöründedir. `queue.json` 668 benzersiz klibi içerir; her kayıt metin ve ses ayarına göre sabit bir kaynak kimliği taşır. `checkpoint.json` üretimin tamamlanma durumunu, `Receipts` özgün dosya adlarını ve SHA-256 özetlerini, `Originals` indirilen sesleri saklar. Hesap değiştirilince tamamlanmış klipler yeniden üretilmez. API anahtarı projeye eklenmemiştir.

1 Ekim 2026 itibarıyla üretim ve içe alma **668/668 klip ile tamamlandı**. Son hesapta kalan 54 klip üretildi; sırada veya indirmesi bekleyen kayıt yoktur. İlk dört hesaptaki kullanılabilir krediler tüketildi; son 4.463 karakter beşinci hesapla tamamlandı. Yeni hesap veya API anahtarı değişimi gerekmiyor. Anahtar proje dışında kalır; uygulama yalnızca yerel ses dosyalarını kullanır.

Tamamlanan **668 MP3** çözümlendi, ses özetlerinin birbirinden farklı olduğu ve mono 44,1 kHz olduğu doğrulandı. Toplam yeni ses süresi **3083.3 saniye** (yaklaşık 51 dakika 23 saniye). Son içe alma sonrası Unity EditMode **12/12**, uygulama içi anlatım akışı **31/31** kontrol geçti. Güncel kanıtlar `ElevenLabs/audio-audit.json`, `runtime-report.txt`, `lesson.png` ve `settings.png` dosyalarındadır. `checkpoint.json` tamamlanmış durumu taşır.

## Kullanıcı deneyimi

- Çocuk modunda ders kartı, soru ve seçenekler otomatik okunur. Seçeneklerin okuma sırası ekrandaki sıradır; mini oyundaki rastgele sıralama da korunur.
- Yanıt verince eski ses kesilir ve açıklamaya geçilir. Yanlış yanıtın önünde “Birlikte bir daha düşünelim” duyulur. Ses bitmeden devam edilebilir.
- Sekiz mini oyunun yönergeleri seslendirilir. Hafıza oyunu ilk yönerge bitmeden örnek sırayı göstermez. Ses kapalıysa ses için beklemez.
- Derslerde ve oyunlarda **Dinle / Baştan dinle** ve **Durdur** bulunur. Rehber kartları ve yetişkin dersleri isteğe bağlı dinlenir.
- Konuşma sırasında arka plan müziği yumuşakça kısılır; konuşma bitince geri gelir. Genel ses ve anlatıcı ses düzeyi birlikte uygulanır.
- Sayfa değişimi, modal açılması ve 3D sahneye geçiş mevcut anlatımı durdurur. Uygulamanın arka plana alınması konuşmayı duraklatır.
- Profil → Ayarlar → Sesli anlatım bölümünde anlatıcıyı kapatma, çocuk modunda otomatik okumayı kapatma, ses düzeyi ve ses örneği bulunur. Tercihler kişi başına saklanır.
- Ayarlarda sesin yapay zekâyla üretildiği açıkça belirtilir.

## Metin ve ses üretimi

`Tools/LearningApp/narration-copy.json` çocuklar için yazılmış 21 ders girişini, sekiz oyun yönergesini ve hedefli metin düzeltmelerini içerir. Kısa cümleler, çocuğa doğrudan hitap, yetişkinden yardım isteme ve yargılamayan geri bildirim tercih edilir. Çocuğa araç kullanma veya ağır mobilya sabitleme görevi verilmez. Korkmanın normal olduğu belirtilir; korunma hareketi nefes egzersizinden önce gelir. Kısa mesajın mutlaka ulaşacağı veya sabitlenen eşyanın asla devrilmeyeceği söylenmez.

Bu metinler içerik yeniden içe alındığında da `import-content.mjs` tarafından uygulanır. Anlamlı içerik değişikliğinden sonra ses manifesti ve dosyaları yeniden üretilmelidir; eski ses metniyle eşleştirme yapılmaz.

```powershell
python -X utf8 Tools/LearningApp/import-elevenlabs.py status
# ElevenLabs web arayüzünde queue.json içindeki prompt ile aynı sesi üretip MP3 indir.
python -X utf8 Tools/LearningApp/import-elevenlabs.py import --index 0 --audio "C:/path/to/elevenlabs-export.mp3"
# Doğrulanmış web indirme kayıtlarını toplu içe almak için:
python -X utf8 Tools/LearningApp/import-elevenlabs.py import-web
```

`ffmpeg` ve `ffprobe` PATH üzerinde bulunmalıdır. `plan` ağ erişimi olmadan sıralı kuyruğu oluşturur. `import` ses dosyasını doğrular, ses yüksekliğini dengeler ve ancak bundan sonra manifesti atomik olarak günceller. İndirmesi eksik veya belirsiz olan kayıtlar tamamlanmış sayılmaz. Önceki manifest yedeklenir. Eski `generate-narration.py` ElevenLabs geçişi başladıktan sonra çalışmayı reddeder; yeni sesleri yanlışlıkla eski sağlayıcıyla değiştirmez. Hizmete yalnızca uygulamanın eğitim metinleri gönderilir; profil verileri gönderilmez.

API ile toplu devam etmek için `generate-elevenlabs.py` hazırlanmıştır. `--prepare` anahtar veya ağ bağlantısı istemez; `--preflight` yalnızca Nisa sesi ve kalan kotayı okur. Bu kontrol üretim yetkisini doğrulamaz: mevcut ücretsiz hesap Nisa ile API üretimine `402 payment_required` döndürmüştür. [ElevenLabs, ücretsiz planda Voice Library seslerinin API üretimini desteklemez](https://elevenlabs.io/docs/overview/capabilities/text-to-speech). Web üzerinden üretim ve API üzerinden geçmişteki hazır kaydı indirme doğrulanmıştır; `--recover-only` yeni üretim başlatmadan ilk eksik kayda kadar bu dosyaları içe alır. Diyalog geçmişindeki ses ve metin, üst seviye alanlar yerine `dialogue` dizisinden eşleştirilir. Uzun metinlerle yapılan geçmiş araması `400 invalid_parameters` döndürebildiğinden aramada ilk 100 karakter kullanılır; indirilecek kayıtta tam metin, ses ve model yine birebir doğrulanır. Aynı başlangıca sahip yanlış metin, farklı ses ve farklı modelin seçilmediği çevrimdışı kontrolle doğrulandı.

Üretim yetkisi olan bir hesapta API üretimi için açık bir `--max-characters` bütçesi gerekir. Anahtar `ELEVENLABS_API_KEY` ortam değişkeninden veya proje dışındaki `~/.codex/elevenlabs-api-key.txt` dosyasından okunur. Araç aynı ses/model/metinle eşleşen geçmiş kaydını önce kontrol eder. Sonucu belirsiz bir ücretli isteği otomatik tekrarlamaz. Hesabın bu sesi API üzerinden kullanma yetkisi yoksa başka sese geçmeden durur. Bütçe sınırı, ücretsiz geçmiş kaydı kullanımı ve belirsiz istek koruması çevrimdışı kontrollerle doğrulanmıştır; canlı API üretimi bu hesapta kullanılamamıştır.

```powershell
python -X utf8 Tools/LearningApp/generate-elevenlabs.py --preflight
python -X utf8 Tools/LearningApp/generate-elevenlabs.py --recover-only
```

- Katalog: `Assets/LearningApp/Resources/LearningApp/Narration/manifest.json`.
- 669 metin kaydı / 668 etkin benzersiz ses; sağlayıcı bilgisi her kayıtta tutulur.
- ElevenLabs: mono, 44,1 kHz, 128 kbps MP3; ses yüksekliği dengelenmiştir. Etkin manifestte tüm dosyalar ElevenLabs Nisa kaydıdır.
- Unity içe alma: Vorbis, bellekte sıkıştırılmış ses, ön yükleme kapalı. Oynatıcı bir klibin ses verisi hazır olduktan sonra çalar; aktif klip tamamlanınca bırakılır.
- ElevenLabs ses örneği: `ClientExports/DepremApp/Narration/elevenlabs-nisa-ornek.mp3`. Önceki `kadin-anlatici-ornek.mp3` eski sağlayıcıya aittir.

## Doğrulama

1. Unity'de `Assets > Refresh`.
2. `Tools > Deprem App > Narration > Configure Audio Imports`.
3. EditMode testi: `Deprem.Learning.Tests.LearningAppTests`.
4. `Tools > Deprem App > Review > Play App`.
5. `Tools > Deprem App > Narration > Verify Runtime`.

Ses kapsamı testi tüm dersleri, yanıtları, açıklamaları, oyun sorularını ve rehber metinlerini kontrol eder; 668 klibin Unity'de çözülebildiğini, süresini ve mono olduğunu doğrular. Eski kayıtların yeni tercih varsayılanlarıyla açılması ve profil ayrımı ayrıca test edilir. Çalışma zamanı kontrolü ayrı bir geçici kayıt kullanır, bitince önceki kayıt kaynağına döner. Sonuç ve gerçek Unity görüntüleri `ClientExports/DepremApp/Narration` klasöründedir.

Tüm ElevenLabs kayıtları tamamlandıktan sonra EditMode **12/12**, çalışma zamanı **31/31** kontrol geçti. Güncel ses çözümleme denetimi `ElevenLabs/audio-audit.json`, çalışma zamanı sonucu `runtime-report.txt` içindedir. Birden fazla klibin sırayla bitmesi, otomatik ses temizliği, bekleyen başlangıcın sayfa değişiminde iptali ve ses sıfırdayken hafıza oyununu bekletmeme kontrolleri korunur.

Kontroller Unity Editor içindir. Fiziksel Android/iOS cihazındaki hoparlör, kulaklık ve işletim sistemi ses kesintileri ayrıca cihazda denenmelidir.

## Kaynaklar

- [ElevenLabs: Üretilen kayıtların geçmişini listeleme](https://elevenlabs.io/docs/api-reference/history/list).
- [ElevenLabs: Eleven v4 ve model bilgisi](https://elevenlabs.io/docs/overview/models).
- [ElevenLabs ses üretim arayüzü](https://elevenlabs.io/app/speech-synthesis/text-to-speech).
- [Microsoft: Türkçe kadın sesi tanımı](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support).
- [edge-tts üretim istemcisi](https://github.com/rany2/edge-tts).
- [AFAD: Deprem anında korunma davranışları](https://www.afad.gov.tr/deprem-aninda-neler-yapmalisiniz).

