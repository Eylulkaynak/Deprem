# Arkadaşlar ve çevrimiçi yarış

Uygulamada **Arkadaşlar** sekmesi ve Oyunlar ekranında **Arkadaşınla yarış** girişi var. Sekiz kısa oyunun çocuk ve yetişkin sürümleri iki oyunculu çevrimiçi yarış destekler. 3D hikâye/maceralar mevcut tek oyunculu akışlarını kullanır.

## Oyuncu akışı

1. Arkadaşlar sekmesini aç; hesabın seçili yerel profile göre otomatik bağlanır.
2. Arkadaş kodunu paylaş veya arkadaşının koduyla istek gönder. Karşı taraf isteği kabul eder.
3. Arkadaşındaki **Yarışa davet et** düğmesiyle bir oyun seç. Alternatif olarak **Yarış** ekranından oda aç ve oda kodunu paylaş.
4. İkiniz de **Hazırım** düğmesine basın. Aynı sunucu turu ve beş saniyelik geri sayımla oyun başlar.
5. Oynarken rakibin puanı ve ilerlemesi güncellenir. Önce puan, eşit puanda sunucunun kaydettiği bitirme süresi sonucu belirler; 0,1 saniye içindeki eşit sonuçlar beraberliktir.
6. **Sıralama** ekranında herkesi veya arkadaşlarını; tüm zamanları veya son yedi günü; bütün oyunları veya tek oyunu seçebilirsin. Çocuk/yetişkin sonuçları ayrıdır. Her oyun için en iyi tamamlanmış yarış puanı toplanır; aynı oyunu tekrar oynamak toplam puanı şişirmez.

**Bu cihazdaki aile profilleri** düğmesi eski yerel profil seçicisini açar. Dersler, hazırlık planları ve kişisel bilgiler çevrimiçi API'ye yüklenmez. Sunucuya oyuncu adı ve oyun hamleleri gönderilir.

## Bu bilgisayarda başlatma

Proje kökünden PowerShell:

```powershell
./Tools/LearningApp/Online/start-online.ps1
```

Python 3.11+ yeterlidir; yerel çalışmada ek Python paketi gerekmez. Sunucu arka planda `127.0.0.1:8765` üzerinde çalışır. Unity'de `Tools > Deprem App > Open Native App` ve Play ile aç. Varsayılan istemci adresi bu yerel sunucudur.

Yerel veritabanı `Tools/LearningApp/Online/data/online.sqlite3`, günlük ve PID dosyaları aynı `data` klasöründedir; Git'e dahil edilmez. Unity hesap bilgisi `learning-progress-v1.json` yanındaki `learning-online-v1.json` dosyasına yazılır. Her yerel profil ve sunucu adresi ayrı hesap kullanır. Bu dosyanın silinmesi önceki çevrimiçi hesaba erişimi kaybettirir; taşıma/yedekleme sırasında koru.

Aynı ağda iki geliştirme istemcisi için:

```powershell
./Tools/LearningApp/Online/start-online.ps1 -BindAddress 0.0.0.0
```

Her iki istemcide **Arkadaşlar > Bağlantı ayarı** üzerinden sunucu bilgisayarının LAN adresini, örneğin `http://192.168.1.20:8765`, gir. İşletim sisteminin güvenlik duvarında bu bağlantıya izin verilmesi gerekebilir. `localhost` farklı cihazlarda farklı bilgisayarlara işaret eder. HTTP bağlantıları Unity Editor ve geliştirme derlemelerine açıktır; yayın derlemesi HTTPS adresi kullanır.

## İnternet üzerinden yayın

Bu çalışma herkese açık bir sunucuya dağıtım yapmaz. Farklı ağlardaki oyuncuların bağlanması için internetten erişilen kalıcı bir HTTPS sunucusu gerekir. İstemci varsayılanını `Assets/LearningApp/Resources/LearningApp/OnlineConfig.json` içindeki `baseUrl` alanından ayarla; cihazdaki Bağlantı ayarı bu varsayılanı değiştirebilir.

Linux sunucusunda Docker kurulumu:

```sh
docker compose -f Tools/LearningApp/Online/compose.yaml up -d --build
```

Compose, SQLite verisini `online-data` biriminde tutar ve portu yalnız sunucu loopback adresine açar. HTTPS reverse proxy'yi `127.0.0.1:8765` adresine yönlendir. Sağlık kontrolü `GET /health` sonucunda `service: deprem-online` ve `version: 1` döner. Konteyner tek Gunicorn worker ve sekiz thread kullanır; SQLite işlemleri bir süreç içindeki kilitle seri yapılır. Birden fazla worker/replica için önce paylaşımlı veritabanı ve oda eşzamanlaması eklenmelidir.

## Yarış ve bağlantı kuralları

- Odalar iki kişiliktir; arkadaş daveti belirli kişiye ayrılır. Oda erişimi her istekte doğrulanır.
- Bekleyen oda beş dakikada, oyun beş dakikada sona erer. Bağlantı kopmasında 60 saniye yeniden bağlanma aralığı vardır. Oynayan kişi ayrılırsa tamamlamış rakibin sonucu korunur.
- Ortak tur sunucuda mevcut içerik kataloğundan hazırlanır. Hafıza dizisi, soru seçenekleri ve yakalama eşyalarının akışı aynı turda aynıdır.
- Sunucu çanta, ayırma, eşleştirme, soru, tehlike, sıralama ve hafıza hamlelerini tur içeriğine göre doğrular. Puanı istemci göndermez; sunucu `max(100, 1000 - hata * 75)` ile tamamlanmış oyun puanını hesaplar.
- Yakalamada ortak akış, eşya kimliği, tekrar ve doğuş zamanı sunucuda doğrulanır; çanta ile eşyanın kesişimi istemcide hesaplanır. Tam fizik doğrulaması yapılmaz. Tur içeriği istemciye gönderildiğinden bu sürüm turnuva seviyesinde hile koruması sunmaz.
- Hamleler sıralı numarayla ve idempotent biçimde aktarılır. Kesilen bağlantı aynı hamleyi ikinci kez puanlandırmaz. İlerleme, kabul edilmiş eşyalar ve oda üyeliği tekrar bağlanmada sunucudan okunur.
- Hesap anahtarı 256 bit rastgele değerdir, API'de bearer başlığıyla gönderilir ve veritabanında yalnız SHA-256 özeti tutulur. Arkadaş kodu giriş anahtarı değildir. Bu sürüm e-posta/şifre ve cihazlar arası hesap kurtarma arayüzü içermez.

## Doğrulama

Sunucu testleri:

```powershell
python -m unittest discover -s Tools/LearningApp/Online -p test_online.py -v
```

Testler sekiz oyunun iki yaş modunu, gerçek iki HTTP istemcisini, arkadaş onayını, özel davet erişimini, yeniden denemeyi, sunucuda puan hesaplamayı, veritabanından tekrar açmayı, sıralama filtrelerini ve zaman aşımını kapsar.

Unity EditMode filtreleri: `Deprem.Learning.Tests.LearningOnlineTests` ve mevcut `Deprem.Learning.Tests.LearningAppTests`.

Unity çalışma zamanı kontrolü için önce ayrı test sunucusunu başlat:

```powershell
./Tools/LearningApp/Online/start-online.ps1 -Port 8766 -DatabasePath .codex_tmp/learning-online/runtime.sqlite3
```

Sonra Play modundayken `Tools > Deprem App > Review > Run Online Verification` menüsünü seç. Kontrol, test sunucusu `127.0.0.1:8766` ve ayrı geçici kayıtlarla iki Unity HTTP istemcisini çalıştırır; kullanıcının eğitim kaydını ve canlı veritabanını kullanmaz.

### 5 Ekim 2026 sonucu

- Sunucu: **12/12 test geçti**; 16 oyun/mod kombinasyonu ve iki gerçek HTTP istemcisi dahil.
- Unity EditMode: **23/23 test geçti**; 12 mevcut uygulama testi ve 11 çevrimiçi sistem testi. XML: `ClientExports/DepremApp/Reports/online-editmode-results.xml`.
- Çalışma zamanı: iki Unity istemcisiyle arkadaş isteği/onayı, canlı puan, sahne yeniden açıldıktan sonra yarışa devam, 16 oyun/mod kombinasyonunun UI düğmeleriyle tamamlanması, ortak sonuç, arkadaş sıralaması ve arkadaş çıkarma doğrulandı. Hata veya istisna oluşmadı. Rapor: `ClientExports/DepremApp/Reports/online-runtime-verification.txt`.
- Portre ekran görüntüleri: `ClientExports/DepremApp/Preview/Online/`. Arkadaş ekranındaki aile kartı sonrasında listenin altına taşındı; canlı yarış, lobi ve sonuç düzenleri doğrulanan sürümle aynıdır.
- Çalışma zamanı kontrolü, aynı bilgisayardaki farklı işlerin Unity editörünü kullanması nedeniyle eğitim uygulamasının geçici Unity kopyasında yapıldı. 3D sahneler bu yarış kontrolünde açılmadı. Android/iOS oyuncu derlemesi, Docker konteyneri ve internetteki bir HTTPS dağıtımı bu çalışmada doğrulanmadı.

Teknik başvurular: [UnityWebRequest](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Networking.UnityWebRequest.html), [SQLite](https://docs.python.org/3/library/sqlite3.html), [Gunicorn](https://gunicorn.org/).
