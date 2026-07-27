# Story 03 — Revize Oynanabilir Senaryo

## Sahnenin amacı

Bu perde hazırlık listesinin sınavı değildir. Oyuncu, tanıdığı ev bir anda hareket etmeye başladığında Can'la birlikte doğru davranışı fiziksel olarak uygular. Story 01 ve Story 02 kararları dekoratif metin değil, sarsıntı sırasında ve sonrasında görülen sahne sonuçlarıdır.

Hedef süre: **9–10,5 dakika**

## Tempo ilkesi

- Sakin açılış süre kapısıyla bekletilmez.
- Deprem başlamadan geri sayım, tahmin, alarm veya öğretici uyarı verilmez.
- Aktif güçlü sarsıntı yaklaşık 40–50 saniyedir.
- Sarsıntı üç farklı fiziksel faz taşır; tek bir rumble altında 90 saniye beklenmez.
- Deprem sonrası dört ayakkabı ve üç kardeş kontrol hotspot'u yerine 4–5 bütüncül sekans vardır.
- Oyuncu yalnız güvenlik açısından zorunlu anlarda hareket edemez.

## 0. Sıradan aile anı — 0:00–1:10

### Görüntü

Akşamüstü. Story 02'den sonra aynı ev:

- Çanta giriş rafındadır.
- Gardırop ve raf sabitlemeleri görünür.
- Aile planı panoda durur.
- Can'ın oyuncak arabası halının kenarındadır.
- Radyo alçak dolapta düşük sesle çalar.

Anne ve baba kadraj dışında mutfak tarafındadır. Oyuncu onları görmez fakat kapı arkasından tabak ve konuşma sesi gelir.

### Oynanış

Deniz serbestçe yürüyebilir. Zorunlu dört inceleme yoktur. Açılışın tek omurgası, Can'ın oyuncağında birbirini takip eden iki fiziksel eylemdir: kaçan tekeri arabaya takmak ve arabayı masanın altından Can'a geri göndermek.

Can masanın yakınında oyuncak arabasının tekerini arar.

**Can:** “Teker masanın altına kaçtı.”

Oyuncu masanın kenarına yaklaşarak tekeri doğrudan arabaya takar, ardından arabayı Can'a geri gönderir. Bu kısa kardeş oyunu güvenli masanın konumunu doğal biçimde tanıtır; ekranda dört maddelik ilerleme sayacı oluşmaz.

Radyo biraz cızırtı yapar.

**Anne, uzaktan:** “Deniz, radyoyu biraz kısar mısın?”

Radyo ve aile planı aynı anda erişilebilir iki isteğe bağlı çevre anıdır. Oyuncu radyonun gerçek düğmesini çevirirse ses azalır fakat tamamen kesilmez; Can'ın plan çizimine dokunursa iki kardeş buluşma alanının yanına küçük bir güneş ekler. Bu iki an depremi başlatmak için tamamlanmak zorunda değildir.

**Can:** “Ben büyüyünce kendi radyom olacak.”

**Deniz:** “Önce oyuncaklarının bütün tekerlerini bul.”

Can gözlerini devirir. Gündelik ambiyans birkaç saniye kendi hâlinde akar. Oyuncu hâlâ hareket edebilir.

## 1. İlk darbe — 1:10–1:22

### Başlangıç

Deprem dört görevin tamamlanmasına bağlı değildir. Kardeşlerin oyuncak araba anından sonra görünmez bir dramatik pencere çalışır; radyo veya aile planıyla etkileşilmiş olsun ya da olmasın, sahne başlangıcından yaklaşık 60–72 saniye sonra aniden başlar. Oyuncuya geri sayım, yaklaşan tehlike işareti veya “hazır ol” hedefi gösterilmez.

İlk 1–2 saniye:

- Radyo sesi bozulur.
- Oyuncak araba kendi kendine birkaç santimetre ilerler.
- Bardak dolapta tıkırdar.
- Asılı lamba küçük salınım yapar.

Ardından ilk sert yatay darbe gelir.

### Girdi

Uzun mesafe NavMesh hareketi kapanır. Oyuncu yalnız kol mesafesindeki Can'a ve güvenli masa bölgesine etkileşim gönderebilir.

Can donar ve pencere yönüne bakar.

**Can:** “Deniz?”

Oyuncu Can'ın kendisine dokunur.

Deniz göz hizasına yaklaşır.

**Deniz:** “Bana bak. Yanımda kal.”

Can başını Deniz'e çevirir. `SiblingCalmed` kaydedilir.

### Gecikme sonucu

Oyuncu kısa güvenlik aralığında tepki vermezse tavandan hafif toz düşer ve Deniz otomatik olarak Can'a bir adım yaklaşır. Ekranda cezalandırıcı mesaj çıkmaz.

**Deniz:** “Can, buraya!”

Etkileşim yeniden açılır. Yakın checkpoint korunur.

## 2. Güvenli masaya geçiş — 1:22–1:34

### Eylem

Masa, Can ve Deniz aynı `RISK` kadrajındadır. Oyuncu masanın güvenli tarafına dokunur; iki çocuk kısa mesafeyi birlikte geçer.

### Yanlış kapı seçimi

Oyuncu aktif sarsıntıda kapıya dokunursa:

- Kapı sertçe kasaya vurur.
- Üstten küçük sıva parçası kapı önüne düşer.
- Deniz Can'ın önüne kolunu koyup geri çekilir.

**Deniz:** “Hayır. Sarsıntı bitmeden çıkmıyoruz.”

Çocuklar yaralanmaz; kamera doğrudan masaya geri döner.

### Yanlış pencere seçimi

Oyuncu pencereye dokunursa:

- Çerçeve gıcırdar.
- Perde savrulur.
- Camda ince bir çatlak görünür fakat patlama olmaz.
- Can irkilir; Deniz onu pencerenin tersine çeker.

**Deniz:** “Camdan uzak.”

Ana güvenli rota bütün varyantlarda açık kalır.

## 3. Çök–Kapan–Tutun — 1:34–2:00

### Kamera

`COVER` kamerasında şunların tamamı görünür:

- Masa tablasının koruyucu yüzeyi.
- Deniz ve Can'ın baş/ense pozları.
- Oyuncunun tutacağı masa ayağı.
- Arka planda dolap veya raf sonucu.

Kamera masa ayağının içine girmez ve yalnız bacak göstermez.

### Faz A — Çök

Oyuncu Can'ın yanında alçak koruma noktasında basılı tutar. Deniz ve Can dizlerinin üzerine geçer.

**Deniz:** “Çök.”

### Faz B — Kapan

İkinci darbede oyuncu Can'ın baş/omuz bölgesinde basılı tutar. Deniz bir koluyla kendi başını, diğer koluyla Can'ın ensesini korur.

**Deniz:** “Başını koru.”

### Faz C — Tutun

Oyuncu masa ayağı üzerinde basılı tutar. Tutuş alanı görünür UI dairesi değil, masa ayağındaki hareket ve Deniz'in uzanan eliyle anlatılır.

**Deniz:** “Tutun.”

Can masa ayağını iki eliyle kavrar.

## 4. Sarsıntının fiziksel fazları — 2:00'a kadar

Aktif deprem toplamda yaklaşık 40–50 saniye sürer. Oyuncu bu sürenin tamamında aynı noktaya pasifçe basmaz; faz değişimlerinde tutuşu ve Can'ı koruma eylemini yeniler.

### Faz 1 — Yatay hareket

- Oyuncak araba halı boyunca yuvarlanır.
- Radyo dolabın üzerinde kayar.
- Lamba doğu–batı yönünde sallanır.
- Kapı kasaya vurur.

### Faz 2 — Eşya sonuçları

`WardrobeSecured` varsa:

- Kayışlar gerilir.
- Dolap birkaç santimetre oynayıp yerine döner.

Yoksa:

- Dolap kontrollü Timeline ile yan geçide devrilir.
- Toz çıkar.
- Ana masa–çıkış hattı kapanmaz.

`ShelfSecured` varsa:

- Raf yerinde kalır.
- Yalnız hafif bir kitap düşer.

Yoksa:

- Raf güvenli taraftan uzağa yatar.
- Birkaç hafif eşya zemine düşer.
- Oyunculara parça isabet etmez.

### Faz 3 — Elektrik kesintisi ve son darbeler

- Ana ışık iki kez titrer.
- Radyo kesilir.
- Son sert darbede pencere çatlağı büyür.
- Ardından darbeler seyrekleşir ve çevresel hareket sönümlenir.

Kamera impulse sonrası başlangıç bestesine döner; kalıcı eğik açı bırakmaz.

## 5. Sessizlik ve kardeş kontrolü — 2:00–3:00

### Ses

Rumble aniden sıfırlanmaz. Düşük frekans çekilir; kısa tinnitus tonu, toz, gevşek metal ve nefes kalır.

Oyuncu birkaç saniye kontrol almadan önce iki çocuğun nefesi duyulur.

### Eylem

Oyuncu Can'ın omzuna dokunur ve tek bütüncül kontrol hareketinde basılı tutar:

- Can'a seslenir.
- Yüzüne bakar.
- Kollarını hareket ettirmesini ister.
- Ayağa kalkmadan önce çevreyi dinler.

**Deniz:** “Can, beni duyuyor musun?”

**Can:** “Duyuyorum.”

**Deniz:** “Bir yerin acıyor mu?”

Can ellerini ve ayaklarını kısa biçimde oynatır.

**Can:** “Hayır. Annem?”

Deniz kapı yönüne döner.

**Deniz:** “Anne! Baba!”

Engelin arkasından, boğuk fakat anlaşılır ses gelir:

**Anne:** “İyiyiz! Buradaki yol kapandı.”

Kısa bir eşya kayma sesi duyulur. Anne devam eder:

**Anne:** “Sarsıntı durduysa birbirinizi kontrol edin. Ayakkabılar, çanta, merdiven. Toplanma alanında buluşacağız.”

**Baba:** “Birlikte kalın!”

Can kapıya doğru kalkmaya çalışır; Deniz kolundan çekmez, önüne geçip göz hizasına iner.

## 6. Ayakları ve rotayı koruma — 3:00–4:15

### Eylem 1 — Cam sınırı

Kamera zemini, ayakkabıları ve kırık camın sınırını aynı planda gösterir. Oyuncu güvenli halı kenarına dokunur.

`BagFlashlight` ve `BagBatteries` varsa Deniz feneri henüz çantayı almadan giriş rafının dış cebinden çıkarabilir; ışık zemine yönelir.

Yoksa zayıf acil ışık iki saniyede bir yanar. Karanlık rota geçilebilir fakat oyuncu durup ışığın yanmasını bekleyerek zemini okumalıdır.

### Eylem 2 — Ayakkabı

Oyuncu Deniz'in ayakkabı çiftini doğrudan güvenli alana çeker ve tek bir aşağı hareketle giyme Timeline'ını başlatır. Dört ayrı sol/sağ ayakkabı hotspot'u yoktur.

Can kendi ayakkabısını giymeye çalışır; bağcık elinden kaçar.

**Can:** “Ellerim titriyor.”

Oyuncu Can'ın bağcığı üzerinde kısa süre basılı tutar.

**Deniz:** “Ben de korkuyorum. Birini ben, birini sen.”

Can ikinci bağı kendisi tamamlar.

### Cam ramak kala

Oyuncu ayakkabısız cam sınırına dokunursa bir parça ayakkabının önünde kayar; Deniz ayağını geri çeker.

**Deniz:** “Önce ayakkabı.”

Yaralanma, kan veya puan kaybı gösterilmez.

## 7. Çanta ve çıkış — 4:15–5:35

### Çanta

Oyuncu giriş rafındaki aynı çantaya yürür. Çanta görünür ve erişilebilirdir.

`BagReady` varsa:

- İki askı doğru ayardadır.
- Deniz tek hareketle omzuna geçirir.

Eksikse:

- Çanta alçak zemindedir.
- Deniz kısa bir ek hareketle askıları açar.
- Oyun kilitlenmez.

Can'ın konfor eşyası dış cepte görünür. Can ona uzanır fakat çantayı açmaya çalışmaz.

**Can:** “Arabam burada.”

**Deniz:** “Sen de buradasın. Hadi.”

### Çıkış

Oyuncu kapı çevresini ışıkla/acı ışıkla kontrol eder. Kapı kolunu doğrudan yana çeker.

`ExitCleared` varsa:

- Kapı tam açılır.
- Çanta hiçbir şeye takılmaz.

Eksikse:

- Hafif kutu kapının bir kısmını tutar.
- Deniz ayağıyla tekme atmaz; kutuyu elleriyle güvenli yana iter.
- Geçiş birkaç saniye uzar ama açık kalır.

## 8. Ayrılık kararı — 5:35–6:55

Kapı açılınca ebeveynlerin bulunduğu taraf görünmez; yalnız kapalı/engelli iç koridor ve ters yöndeki açık apartman koridoru görülür.

Can iç tarafa bakar.

**Can:** “Onları bırakıyoruz.”

Deniz hemen “hayır” demez. Can'ın göz hizasına iner.

**Deniz:** “Onlar kendi yolunda. Biz de plandaki yoldayız.”

Can başını sallar fakat hareket etmez.

Oyuncu aile planı panosundaki toplanma simgesine veya çantadaki aynı renkli işarete dokunur. Kamera işaret ile açık koridoru aynı planda bağlar.

Ebeveynlerin sesi devrilmiş dolabın arkasından geldiğinde oyuncu sese veya görünmez bir hedefe dokunmaz. Dolaba da müdahale etmez. Kapının açıkta kalan gerçek kenarına üç kez vurur. Vuruşlar ahşap kapı sesi üretir; üçüncü vuruştan sonra kapı aralığından sıcak ışık görünür ve Deniz “Biz iyiyiz, Can yanımda” diye seslenir. Baba ancak bu fiziksel sinyalin ardından artçı sarsıntıyı dinlemelerini söyler.

**Deniz:** “Nerede buluşacaktık?”

**Can:** “Parkın yanındaki levhada.”

**Deniz:** “Bizi orada arayacaklar.”

Can Deniz'in elini tutmaz; onun yanında yürümeye başlar. Bu, bağımsızlığının küçük işaretidir.

## 9. Koridor — 6:55–8:20

### İlk geçiş

Koridor evden daha karanlık ve daha soğuk renktedir. Oyuncu:

- Eşiği kontrol eder.
- Tavandan yeni düşme sesi olmadığını dinler.
- Can'ın yanında kaldığını görür.
- Merdiven yönündeki acil işarete ilerler.

Fener varsa oyuncunun baktığı yüzey aydınlanır. Yoksa acil ışık altında kısa duraklar gerekir.

Uzakta komşu kapıları ve baston sesi duyulur fakat Nermin henüz görünmez.

### Rol değişimi

Kapıya verilen üç vuruşun ardından ebeveyn cevabı gelirken tavandaki ince toz çizgisi hareket eder ve ahşap gıcırtısı Can'ın bulunduğu tarafta duyulur. Can otomatik tepki animasyonuyla iç duvarı gösterir; oyuncu uyarıyı başlatmak için Can'a veya bir görev düğmesine dokunmaz. Deniz bir adım daha atmak üzereyken Can durur.

**Can:** “Deniz. Dur.”

Kamera Can'ın baktığı sallanan koridor lambasını gösterir.

**Can:** “Yine titreşiyor.”

Deniz bu kez Can'ın sözünü dinler.

**Deniz:** “Haklısın. Burada.”

Oyuncu iki çocuğun güvenli iç duvar tarafındaki tutunma noktasında basılı tutar. Artçı kısa ve düşük şiddetlidir. Merdiven kapısı artçı sırasında açılmaz.

## 10. Tahliyeye geçiş — 8:20–9:20

Artçı tamamen durur. Çocuklar birkaç saniye yeni hareket sesi dinler.

**Can:** “Şimdi mi?”

**Deniz:** “Şimdi.”

Oyuncu merdiven kapısına doğru yürür. Kamera arkadan yükselerek:

- Çocukları alt üçlüde,
- Merdiven kapısını üst hedefte,
- Arızalı asansör panelini yan çevre ayrıntısı olarak

aynı kadraja alır.

Koridorun arkasından Anne'nin sesi son kez zayıf duyulur:

**Anne:** “Birlikte kalın!”

Can cevap verir:

**Can:** “Birlikteyiz!”

`CorridorReached` kaydedilir. Story 04 ayrı bir özet ekranıyla değil, aynı koridor ambiyansı üzerinden başlar.

## Checkpoint eşlemesi

| Hikâye anı | Checkpoint |
|---|---|
| İlk sert darbe | `QuakeStart` |
| Can Deniz'e odaklandı | `SiblingCalmed` |
| İki çocuk masa altında tutundu | `UnderCover` |
| Sarsıntı durdu, kardeş kontrolü başladı | `PostQuake` |
| Koridor artçısı tamamlandı | `CorridorReached` |

## Ramak kala dili

### Aktif sarsıntıda kapı

- Kapı kasaya vurur.
- Küçük sıva parçası düşer.
- Deniz geri çekilir.
- “Sarsıntı bitmeden çıkmıyoruz.”

### Pencere

- Perde savrulur.
- İnce çatlak büyür.
- Deniz ve Can camdan uzaklaşır.
- “Camdan uzak.”

### Ayakkabısız cam

- Cam parçası ayağın önünde kayar.
- Deniz geri basar.
- “Önce ayakkabı.”

Her ramak kala:

- Yaralanmasızdır.
- 3–5 saniyeyi geçmez.
- Aynı beat'in güvenli başlangıcına döner.
- Güvenli seçeneği kamera kadrajında görünür bırakır.

## Sarsıntıyı azalt seçeneği

Seçenek açıkken:

- Cinemachine impulse genliği düşer.
- Kamera rotasyonu azaltılır.
- Ekran üzerindeki hızlı parallax sınırlanır.

Korunanlar:

- Eşya animasyonları.
- Lamba salınımı.
- Toz ve döküntü.
- Ses katmanları.
- Güvenlik için gerekli zamanlama ve oyuncu eylemleri.

## Silinecek mevcut tempo yapıları

Yeni içerik yerleşip kronometreli test tamamlandığında aşağıdaki alanlar süre üretmek için kullanılmaz:

- `introMinimumDuration = 60`
- `quakeMinimumDuration = 90`
- `minimumCompletionDuration = 480`
- `corridorWarningDuration = 30`

Koridor finalinde aynı dört altyazıyı süre dolana kadar döndüren coroutine kaldırılır. Sahne yalnız oynanan beat'ler tamamlandığında ilerler.

## Diyalog kuralı

- Deprem sırasında tam cümleli eğitim anlatımı yoktur.
- Deniz kısa fiiller kullanır: “Bak”, “Yanımda kal”, “Çök”, “Başını koru”, “Tutun”.
- Can korkar fakat çaresiz değildir; koridor artçısını ilk fark eden odur.
- Anne ve baba çocuklara gaz/elektrik müdahalesi yaptırmaz.
- Tahliye emri yalnız sarsıntı durduktan ve çevre kontrol edildikten sonra gelir.
