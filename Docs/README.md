# Deprem — Hikâyeli Macera Üretim Paketi

Bu klasör, mevcut dört mini oyun sahnesini 35–42 dakikalık tek bir hikâyeli mobil maceraya dönüştürmek için hazırlanmış üretim kaynağıdır.

## Mevcut uygulama durumu — 22 Temmuz 2026

- `StoryHome_Shared.prefab` üretildi ve yapısal doğrulamayı geçti.
- `Story_Rebuild_MainMenu.unity` bağımsız hikâye girişi olarak üretildi:
  - `DEVAM ET` yerel JSON kaydındaki aktif perde/checkpoint'e döner; `YENİ HİKÂYE` hazırlık perdesinden temiz oturum başlatır.
  - Dört rebuild sahnesi `SONRAKİ PERDE` ile birbirine bağlanır; finalde davranış raporundan dört perdeli bölüm seçimi açılır.
  - Kalıcı `StoryGameManager` perde geçişlerinde aynı oturum kimliğini, hazırlık flag'lerini ve tamamlanan perde listesini korur.
  - Ana menü ve dört rebuild sahnesi yayın rotası olarak Build Settings'e bağlandı; eski sahne dosyaları referans olarak korunuyor.
- `Story_01_RebuildPreview.unity` ortak evde, doğrudan dünya etkileşimleri ve karanlık aile tatbikatı Timeline'ıyla üretildi:
  - Toplanma alanı, Melek Teyze iletişim kartı ve Can'ın düdük görevi üç ayrı fiziksel kartla aile planına yerleştirilir; dört kategori mobilyası sahnede fiziksel olarak açılır.
  - 13 eşya doğrudan çantaya sürüklenir. Mutfak dolabı açıldıktan sonra su şişesinin tarih etiketi şişenin üzerinde yana çevrilir; yardım dolabında kapalı sargı paketinin mührü doğrudan basılı tutularak kontrol edilir. Bu iki kontrol tamamlanmadan ilgili çanta eşyaları açılmaz.
  - Fener, radyo, düdük, su cebi, belge mührü ve ana fermuar da çantanın/nesnenin kendisi üzerinde işlevsel olarak kontrol edilir.
  - Can'ın konsolu çantayı gerçekten ağırlaştıran zorunlu bir kardeş çatışmasına dönüşür; oyuncu konsolu çantadan masaya geri sürükler ve dengelenen çantayı ikinci kez kaldırır.
  - Can'ın rahatlatıcı oyuncak arabası doğrudan ona verilir.
  - Karanlık prova Timeline'ı oyuncuya dış cebi fiziksel olarak açtırır; fener aile planına, güvenli masaya ve Can'a üç ayrı sahne hedefi üzerinden yöneltilir. Can bulunduktan sonra göğsündeki düdüğe üç kısa kez dokunulur.
  - Üç fener doğrultusu hazır Spot Light nesneleridir; yeni runtime component oluşturulmaz. Her hedef kendi bestelenmiş kadrajına geçer.
  - Toplam 40 sahne içi etkileşim ve 10 bestelenmiş Cinemachine kadraj bulunur.
  - Zorunlu altyazı/sahneleme süresi yaklaşık 149,5 saniyedir. Fiziksel arama, yürüme ve etkileşimlerle ilk oynayış üretim tahmini 8–10 dakikadır; kronometreli kör test henüz yapılmadı.
- `Story_02_RebuildPreview.unity` ortak evde üretildi:
  - İlk oyuncak araba testi üç gerçek engelde durur; ayakkabı, oyuncak kutusu ve paket doğrudan kendi saklama yerlerine taşınır.
  - Temizlenen kapı ikinci bir kontrol etkileşimiyle tekrarlanmaz; Nermin için kapıyı gerçekten açmak sonucu kanıtlar.
  - Nermin'in kapı, zarf, tahliye planı ve bastonuyla koridora yürüyüş akışı bulunur.
  - Eski üç maddelik risk kontrolü yerine raf ve dolap üzerinde aşağı sürüklenen iki fiziksel düşme alanı vardır; oyuncu Can'ın minder ve çizgi romanından oluşan okuma köşesini turuncu alandan güvenli ortak halıya taşır.
  - Story 03'te hazırlanmış durumda güvenli okuma köşesi yerinde kalır; hazırlanmamış rota varyantında eski köşede eğilmiş minder, hasarlı çizgi roman ve raf döküntüsü görünür.
  - Raf/dolap öncesi-sonrası karşılaştırması aynı kontrol noktasında yapılır.
  - Final rota kamerası oyuncak arabanın başlangıçtan eşiğe kadar bütün hattını gösterir; Can finalde arabayı yerden alıp cebine koyar.
  - Yetişkinin 8,2 saniyelik matkap çalışma sekansları.
  - 23 oynanabilir sahne içi etkileşim; 13 nesne kendi fiziksel hedef alanına doğrudan sürüklenir. Eski akıştan kalan iki ölü etkileşim sahneden çıkarıldı.
- `Story_03_RebuildPreview.unity` ortak evde bağımsız kalite kapısı olarak üretildi:
  - Açılış yalnız oyuncak tekerini arabaya takıp arabayı Can'a geri gönderme mikro-hikâyesini zorunlu tutar. Radyo ve aile planı aynı odada isteğe bağlı kalır; deprem dört maddelik görev dizisi tamamlandığı için başlamaz, görünmez 60–72 saniyelik dramatik pencere içinde aniden gelir.
  - Can'a dokunma, masaya geçme, başı koruma ve masa ayağını tutma üzerinden doğrudan Çök–Kapan–Tutun.
  - 48 saniyelik, üç fazlı sahne-authored deprem Timeline'ı; sabitlenmiş/sabitlenmemiş dolap ve raf sonuçları.
  - Sekiz deprem sonrası beat, fener/acil ışık ayrımı, doğrudan kapı açma ve beş koridor beat'i.
  - Story 01'de Can'a verilen rahatlatıcı oyuncak `BagComfortItem` üzerinden deprem sonrasında Can'ın elinde görünür ve kardeş kontrolü diyaloğunda geri çağrılır.
  - 27 sahne içi etkileşim; 5 nesne kendi fiziksel hedef alanına doğrudan sürüklenir.
  - On Cinemachine kadrajı; masa koruma planında iki çocuk, masa tablası ve tutulan ayak aynı kadrajda.
  - Koridor; Synty apartman kapıları, posta kutuları, paspaslar, saat, çerçeveler, devrilmiş kitaplık, hafif koli, merdiven kapısı, çıkış levhası ve iki ışık havuzuyla giydirildi. Temiz rota oyunvari zemin karoları yerine fiziksel olarak boş bırakılmış geçiş ve koyu koşu halısıyla okunur.
  - Soyut “sese dokun” adımı kaldırıldı: kapının açıkta kalan kenarına üç fiziksel vuruş ebeveyn cevabını ve sıcak ışığı üretir. Cevap sürerken Can tavandaki ilk tozu/gıcırtıyı Deniz'den önce fark edip işaret eder; son artçı eylemi Can'ın uyarısına cevap olarak oynanır.
- `Story_04_RebuildPreview.unity` bağımsız tahliye/final kalite kapısı olarak üretildi:
  - Açılıştaki görünür amber “koridoru dinle” şeridi kaldırıldı. Oyuncu normal dokun-yürüyle gerçek kapı eşiğini geçince sahne trigger'ı dinleme anını otomatik başlatır; Can tavanı, Deniz merdiven yolunu kontrol eder.
  - Koridor, merdiven, 10 saniyelik artçı tutunması, alt sahanlık, bina önü, kısa sokak ve toplanma alanı tek kesintisiz rotadır.
  - Nermin'e önce sorulur; karton kutu, hafif köpük ve baston kendi fiziksel hedeflerine sürüklenir. Ağır dolaba veya Nermin'in koluna müdahale edilmez.
  - Bina çıkışından sonra cepheden uzaklaşma ayrı bir dünya adımıdır. Sokakta turkuaz/mercan cevap şeritleri kullanılmaz; oyuncu cam sınırından açık yan kaldırıma sürünce tabela gıcırdar, cam kayar, toz dökülür ve Can güvenli kaldırımı işaret eder. Camlı kestirme güvenli ramak kala ile reddedilir.
  - Toplanma alanındaki on adımlı eşya kontrol listesi kaldırıldı. Levha yaklaşmayla okunur; Nermin'in yanında basılı tutma, ilk yardım setini/temiz bezi gerçek tedavi tepsisine sürükleme, düdük/aile işareti ve Anne'ye fiziksel yaklaşma olmak üzere dört zorunlu dünya etkileşimi vardır.
  - Hazırlanmış radyonun fiziksel düğmesi alana girişte frekansı yakalar, canlı ışığı yanar ve uzamsal yayın başlar; radyo yoksa görevli megafonu eşdeğer sonucu verir. Su, battaniye, iletişim kartı ve Story 01 konfor oyuncağı ayrı görev kuyruğu olmadan flag'e bağlı görünür sahne sonuçlarıdır.
  - Anne ve Baba sinyalden sonra çocuklara dönük biçimde kadraja fiziksel olarak girer. Oyuncu Anne'ye sahnede dokununca Deniz ayrı buluşma noktasına gerçekten yürür, Can takip eder ve görevli masasındaki fiziksel aile sayımı `2/4`ten `4/4`e geçer; completion paneli ancak bundan sonra açılır.
  - Toplanma finali dört zorunlu sahne içi etkileşime indirilmiştir. Geniş levha/final planından ayrı sağ çapraz bakım kamerası Deniz, Can, Nermin, görevli ve tedavi tepsisini aynı portre kadrajına alır; on dört Cinemachine kadrajı 9:16 ve 9:19.5 render ile denetlendi.
  - Bina önü, camlı kestirme, acil araç yolu ve toplanma alanı; iki mahalle cephesi, itfaiye aracı, sokak mobilyaları, açık yardım kanopisi, kalabalık ve sınır peyzajıyla tek okunabilir dış rota olarak giydirildi.
- Yerel `POLYGON Town` ve `POLYGON City Characters` paketleri karakter bağımlılıklarıyla import edildi:
  - Deniz `SchoolBoy`, Can `Son`, Anne `Mother`, Baba `Father`, Nermin `Grandma`, toplanma görevlisi `Paramedic`.
  - Materyallerin 37/37'si URP/Lit ve GPU instancing kullanır.
  - Kullanılmayan Town içeriği çıkarıldı; Story 03 apartman koridoru ve Story 04 dış rotası için seçilen 30 çevre modeli yaklaşık 1,4 MB'lık bağımsız `Assets/Story/Environment/SyntyTown` setinde korundu.
  - Dört rebuild sahnesinde Humanoid avatar, ayrı çocuk silueti ve animasyon retarget doğrulandı.
- `rubberduck / 100 CC0 SFX #2` paketi kaynak ve lisans kaydıyla import edildi:
  - 100 orijinal OGG dosyası `Assets/Story/Audio/ThirdParty/rubberduck_sfx100v2` altında korunur.
  - Dört rebuild sahnesindeki her dünya etkileşimi nesne türüne göre kapı, eşya, cam, metal, taş, ahşap, anahtar veya yaklaşma sesi kullanır.
  - Story 01–03 ayrı iç mekân tonuna, Story 04 uzak trafik ve dış ortam katmanına sahiptir.
  - Story 03 deprem Timeline'ında altı zamanlanmış metal, ahşap, moloz ve kapı darbesi bulunur.
- `Kenney Input Prompts` CC0 ikonları dört rebuild sahnesindeki bütün dünya etkileşimlerine bağlandı; dokunma, basılı tutma ve sürükleme jestleri kamera karşısında duran sahne-authored rozetlerle ayrılır.
- Build Settings yalnız yeni ana menü ve Story 01–04 rebuild sahnelerini paketler; mevcut eski Story 01–04 sahne dosyaları değiştirilmeden editör referansı olarak korunur.
- Ana menüden Story 01 → 02 → 03 → 04 geçişi; aynı kalıcı manager, perdeye özel checkpoint sıfırlaması, hazırlık sonuçları ve final bölüm seçimiyle baştan sona PlayMode testinden geçti.
- Dört rebuild sahnesindeki 40 bestelenmiş kameranın tamamı hem 540×960 (9:16) hem 540×1170 (9:19.5) deterministik dünya render'ıyla üretildi. Karanlık Story 01 fener hedefleri ayrıca üç ayrı 9:16 görüntüyle denetlendi.
- EditMode regresyon sonucu: **96/96 geçti**.
- PlayMode regresyon sonucu: **21/21 geçti**. Buna Story 01 aile planı kartının gerçek dünya hedefi, su tarihi/sargı mührü kapıları ve karanlık prova cep → pano → masa → Can → düdük zinciri de dahildir.
- Editör içi tanısal örneklemede Story 03 ve Story 04 için 240'ar karede p95 kare süresi sırasıyla 4,65 ve 5,04 ms ölçüldü. Editor toplam belleği 865,8–1197,1 MB aralığındaydı ve Test Runner her karede managed allocation üretti. Bunlar Android cihaz sonucu değildir; 60 FPS, sıfır sürekli GC ve 650 MB kapıları hâlâ gerçek Development Build üzerinde doğrulanmalıdır.
- 20 Temmuz 2026 cihaz kapısı denemesinde ADB bulundu fakat bağlı Android hedefi yoktu; kurulu Unity 6000.0.58f2 editöründe `AndroidPlayer` modülü de bulunmadı. Bu yüzden Android APK/profil sonucu üretilmiş gibi kabul edilmez.
- Story 01–04 için kronometreli kör oynanış ile gerçek cihaz dokunma testi hâlâ kalite kapısıdır; hedef süreler henüz doğrulanmış kabul edilmez.

## Önce okunacak üç belge

1. `Full_Game_Content_Audit.md`
   - Mevcut oyunun gerçek içerik ve süre denetimi.
   - Story 03'teki yapay süre kapıları.
   - Her perdenin korunacak ve değiştirilecek tarafları.
2. `Full_Game_Narrative_Bible.md`
   - Tema, karakter yayları, tekrar eden motifler ve dört perde omurgası.
3. `Full_Game_Production_Roadmap.md`
   - Ortak evden final sahnesine kadar üretim sırası ve kalite kapıları.

## Oynanabilir senaryolar

- `Story_01_Playable_Script.md`
- `Story_02_Playable_Script.md`
- `Story_03_Revised_Playable_Script.md`
- `Story_04_Playable_Script.md`

Bu belgeler zaman kodlu sahneleme, oyuncu eylemi, kısa diyalog, ramak kala ve checkpoint karşılıklarını içerir.

## Teknik üretim kaynakları

- `Full_Game_Beat_Production_Matrix.md`
  - Dört perdenin beat, kamera, ses/VFX ve flag matrisi.
- `Story_01_Production_Blueprint.md`
  - Story 01 için prefab, kamera, Timeline ve sahne hiyerarşisi planı.
- `Story_01_Conversion_Inventory.md`
  - Mevcut Story 01 sahnesinde neyin korunacağı, taşınacağı ve kaldırılacağı.
- `Story_01_Rebuild_Plan.md`
  - Story 01 dönüşümünün tasarım gerekçesi ve kapsamı.
- `Asset_Acquisition_Manifest.md`
  - Yerel asset kullanımı, karakter/ses seçenekleri ve lisans düzeni.
- `Blind_Playtest_Protocol.md`
  - 35–45 dakikalık tam rota ve perde süreleri için kronometre noktaları, gözlem formu ve kabul kapısı.

## Kilitlenmiş üretim ilkeleri

- Süre bekleme sayacıyla değil, oynanabilir olayla oluşur.
- Ortada “tıkla/kaydır” eylem düğmesi bulunmaz.
- Oyuncu sahnedeki gerçek nesnenin kendisiyle etkileşir.
- Story 01, Story 02 ve Story 03 aynı aile evini kullanır.
- Story 01 ve Story 02 kararları Story 03/04'te fiziksel sonuç üretir.
- Asansör güvenli seçenek olarak sunulmaz.
- Çocuklar ağır mobilya, matkap, gaz veya elektrik tesisatına müdahale etmez.
- Yeni runtime kod son çaredir; sahne, Animator, Timeline, UnityEvent ve mevcut manager'lar önceliklidir.
- Eksik hazırlık hiçbir kombinasyonda softlock üretmez.

## Tamamlanan asset kararları

1. Karakter: yerel Synty Town + City Characters kadrosu rebuild sahnelerine uygulandı.
2. Ses: büyük Sonniss arşivi yerine küçük ve izlenebilir CC0 yolu seçildi; `100 CC0 SFX #2` paketi sahnelere bağlandı.

Hikâye paketi, Nermin ve Can'ın oyuncak arabası rebuild önizlemelerinde uygulanmış üretim kararıdır.

## Kalan kalite kapıları

1. `Blind_Playtest_Protocol.md` ile en az beş ilk-oynayış oturumu kaydet; Story 01 için 8–10 dakika, tam rota için 35–45 dakika medyanını doğrula.
2. Aynı rotayı gerçek bir 9:16 ve 9:19.5 dokunmatik cihaz/emülatör profilinde oyna; render kanıtına ek olarak safe area, altyazı ve parmak hedeflerini doğrula.
3. Snapdragon 778G sınıfı Android Development Build'de FPS, sürekli GC allocation, bellek ve sıcak sahne geçişini Profiler ile ölç.
4. Android cihaz profili hızlı kapanış kapsamında ertelendi; aktif Build Settings rotası yeni hikâye sahneleridir, eski dört sahne yalnız referans olarak korunur.
