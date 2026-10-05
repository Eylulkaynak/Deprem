# Mobil veli bağlantısı

Çocuk ve veli aynı mobil uygulamayı kullanır. Çocuk profili süreli kod oluşturur; veli kendi e-posta ve parolasıyla giriş yapıp kodu ekler. Veli birden fazla çocuğun ilerlemesini kendi telefonundan takip edebilir. Mevcut çevrimdışı ders ve oyun kayıtları kullanılır.

## Uygulama akışı

1. Çocuğun telefonunda **Profilim → Veli bağlantısı** ekranını açın. Arkadaşlar ekranındaki **Velime bağlan** düğmesi de aynı yere gider.
2. Ad ve eğitim ilerlemesini paylaşma seçeneğini işaretleyip **İlerlememi paylaş ve kod oluştur** düğmesine basın.
3. Velinin telefonunda **Arkadaşlar → Veli panelini aç** üzerinden veli hesabı oluşturun veya giriş yapın. Yetişkin modunda **Profilim → Veli bağlantısı** doğrudan veli girişini açar.
4. **Çocuk bağla** düğmesine basıp çocuğun 8 karakterli kodunu girin. Kod 10 dakika geçerlidir, yalnızca bir kez kullanılabilir. Yeni kod eskisini kapatır.
5. Çocuk kartından tamamlanan 21 çocuk dersini, kaldığı adımı, sekiz kısa oyunun puan ve yıldızlarını, çocuk etkinliklerinden kazanılan XP'yi ve tekrar önerilerini görün.

Veli paneli açıkken 30 saniyede bir yenilenir; elle yenileme de vardır. Her çocuk için en son eşitlenen zaman gösterilir. İnternet olmadığında son alınan rapor açıkça belirtilir. Cihazdaki mevcut seri ve son etkinlik kaydı profil genelindedir; ders, oyun ve tekrar konuları çocuk içeriğine göre filtrelenir. Tekrar önerileri geçmiş yanlış cevapların toplamına dayanır.

Çocuk uygulamasında bağlı velilerin adları ve bağlantıyı kaldırma düğmeleri görünür. **İlerleme paylaşımını kapat** tüm veli erişimini kaldırır. Veli de kendi çocuk bağlantısını veya veli hesabını silebilir. Öğrenme ilerlemesi cihazda korunur.

## Çevrimdışı çalışma ve mobil kayıt

- İnternet ve veli hesabı, ders ve kısa oyunları çalıştırmak için gerekmez.
- Paylaşım varsayılan olarak kapalıdır; eski kayıtlar otomatik olarak paylaşılmaz.
- Paylaşım açıkken yerel ilerlemenin özeti arka planda eşitlenir. Sayfa değişiklikleri ağ işlemini iptal etmez. Veri değişmediğinde tekrar gönderilmez; bağlantı hataları 10–120 saniyelik aralarla yeniden denenir.
- Çevrimdışı paylaşımı kapatma isteği cihazda saklanır. Yeni yüklemeler hemen durur; bağlı velilerin sunucu erişimi bağlantı geri geldiğinde kaldırılır. Ekran bekleyen işlemi belirtir.
- Çocuk cihazının yalnızca kendi ilerlemesine yetkili anahtarı, uygulamanın özel kayıt dizininde `learning-progress-v1.json.family.json` içinde saklanır. Ayrı profiller ayrı anahtar kullanır. Hizmet adresi değişirse eski anahtarlar yeni hizmete gönderilmez.
- Veli parolası ve oturum anahtarı dosyaya yazılmaz. Veli oturumu uygulama arka plana geçtiğinde kilitlenir; rapor belleği temizlenir.
- Sunucuya hazırlık planı, adres, telefon, verilen cevaplar veya yetişkin dersleri gönderilmez. `FamilySnapshot.FromProfile` izin verilen alanları çıkarır; API bunun dışındaki alanları reddeder.
- 3D hikâye ve Yan Yana kayıtları mevcut ortak sistemlerinde kaldığından bu rapor şu anda yerel eğitim dersleri ve sekiz kısa oyunla sınırlıdır.

## Sunucu bulunmayan geliştirme ortamı

`Services/Family/server.py` çalışan bir Python/SQLite API'dir. Geliştirmede ek Python paketi gerektirmez:

```powershell
python Services/Family/server.py --dev
```

Unity Editor, yayın adresi boşken `http://127.0.0.1:8787` kullanır. Bu adres yalnızca aynı bilgisayardaki geliştirme içindir. Mobil oyuncu sürümü HTTP kabul etmez; yapılandırılmamış sürüm veli özelliğinin henüz açılmadığını gösterir. Eğitim çalışmaya devam eder. Telefona `localhost` yazılması önerilmez.

API varsayılan veritabanını `Services/Family/data/family.sqlite3` altında tutar; bu dizin Git'e alınmaz. Testler geçici veritabanları kullanır. Sunucu parolaları ayrı rastgele salt ve 600.000 PBKDF2-HMAC-SHA256 turuyla, oturum/cihaz anahtarları ve kodları hash olarak saklar. Oturumlar 12 saat sonra biter. Başarısız giriş ve kod denemeleri sınırlanır; kod kullanımı ve erişim kaldırma işlemleri SQLite işlemleriyle sıralanır. Bir veli en fazla 12 çocuk, bir çocuk en fazla 5 veli bağlayabilir.

## Mobil yayın için kurulum

Kendi alan adına sahip bir HTTPS hizmeti Android ve iOS uygulamalarının ortak adresidir. Sunucu veya ücretli kaynak bu çalışma sırasında oluşturulmadı. Hazır Docker/Waitress/Caddy kurulumu:

1. Alan adının DNS kaydını hizmeti çalıştıracak makineye yönlendirin; 80 ve 443 portlarını açın.
2. `Services/Family/.env.example` dosyasını `.env` olarak kopyalayıp `FAMILY_DOMAIN` değerini gerçek alan adıyla değiştirin.
3. `Services/Family` dizininde `docker compose up -d --build` çalıştırın. Caddy HTTPS sertifikasını yönetir. API portu dışarı açılmaz; veritabanı kalıcı Docker biriminde saklanır.
4. Unity'de **Tools → Deprem App → Family → Service Configuration** menüsünden `https://gercek-alan-adi` adresini kaydedin. Kaynak ayar `Assets/LearningApp/Resources/LearningApp/FamilyConnection.json` içindedir; sunucuya özel gizli anahtar uygulamaya gömülmez.
5. Aynı adresle iki mobil cihazda uygulamayı derleyip bağlantı ve eşitleme akışını çalıştırın. SQLite yedeğini hizmet durdurularak veya SQLite backup API'siyle alın; çalışan veritabanının yalnızca ana dosyasını kopyalamak WAL içeriğini kaçırabilir.

Konteyner kullanılmazsa `python -m pip install -r Services/Family/requirements.txt` ardından `python Services/Family/server.py` ile Waitress çalıştırılabilir; HTTPS ters vekili ayrıca kurulmalıdır. `--dev` yalnızca loopback adresinde çalışır. Başka bir ters vekil kullanıldığında `--trusted-proxy` yalnızca o vekile göre tanımlanmalıdır; hazır Docker ağında sadece Caddy, API'ye dışarıdan erişim sağlar.

E-posta bu sürümde giriş kimliğidir. E-posta doğrulaması, unutulan parola için e-posta hizmeti ve push bildirim hizmeti henüz bağlı değildir. Bir çocuk profili tek cihaz kaynağı olarak bağlanır; yeni bir telefona tam oyun kaydı taşımak ayrı bir bulut yedekleme özelliğidir.

## Doğrulama

Sunucu testleri:

```powershell
python -m unittest discover -s Services/Family -p test_server.py -v
```

Gerçek Unity UI ve iki bağımsız cihaz istemcisiyle, geçici API/veritabanı ve açık projeye dokunmayan ayrı test projesinde doğrulama:

```powershell
python Tools/LearningApp/verify-family.py
```

Unity yolu farklıysa `--unity` argümanı kullanılabilir. Test projesi `.codex_tmp/family-isolated-*` altında tutulur. Sonuçlar ve gerçek portre ekran görüntüleri `ClientExports/DepremApp/Family/` klasörüne kopyalanır. Raporlar Editor doğrulamasıdır; fiziksel Android/iOS cihaz testi olarak sunulmaz.

5 Ekim 2026 doğrulaması: 12 sunucu testi ve 14 Unity testi geçti. Gerçek UI ile paylaşım izni, tek kullanımlık kod, iki bağımsız istemci, veli girişi ve rapor, arka planda ilerleme eşitleme, çevrimdışı kayıt ve bekleyen erişim kaldırma kontrol edildi. 1080×1920, 750×1334 ve 1080×2340 portre boyutlarında ekran görüntüleri alındı. Unity sonuçları `ClientExports/DepremApp/Family/EditMode.results.xml`, akış raporu `runtime-verification.txt` içindedir. Docker yayını ve fiziksel telefon doğrulaması henüz yapılmadı.

Kod: `LearningFamilyModels.cs`, `LearningFamilyService.cs`, `LearningFamilyViews.cs`. Yerel kayıt ve ders davranışları `LearningProgress.cs` içinde korunur. Veli hizmeti, mevcut çevrimiçi arkadaş/yarış hizmetinden ayrı bir API'dir; aynı HTTPS alan adında farklı yollarla da barındırılabilir.

Uygulama kararları için birincil kaynaklar: [OWASP parola saklama](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html), [Waitress ve ters vekil](https://docs.pylonsproject.org/projects/waitress/en/stable/reverse-proxy.html), [UnityWebRequest](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Networking.UnityWebRequest.html).
