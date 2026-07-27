# Deprem — Tam Oyun Üretim Yol Haritası

## 1. Üretim stratejisi

Tam oyun aynı anda dört sahnede yeniden yapılmayacaktır. Önce ortak ev ve güçlendirilmiş Story 01 kalite kapısı tamamlanır. Story 01 hedef kaliteyi taşımıyorsa aynı sistem Story 02–04'e kopyalanmaz.

Yeni runtime kod son çare olacaktır. Öncelik sırası:

1. Sahne nesnesi ve prefab varyantı.
2. Animator.
3. Timeline.
4. Serialize edilmiş UnityEvent.
5. Mevcut `StoryTouchManager`, `StoryInteractable` ve bölüm director manager'ları.
6. Yalnız başka çözüm yoksa tek, genel ve yeniden kullanılabilir runtime bileşen.

## 2. Faz 0 — Yaratıcı ve asset kararları

Üretimden önce kilitlenecek kararlar:

- Çalışma adı kullanılacak mı?
- Can'ın konfor eşyası fotoğraf mı, oyuncak araba mı, yoksa oyuncu seçimi mi?
- Nermin Story 02'de tanıtılacak mı?
- Melek Teyze şehir dışı iletişim kişisi olarak kalacak mı?
- Yeni çocuk karakter paketi için bütçe var mı?
- Büyük ücretsiz ses paketi indirilebilir mi?

## 3. Faz 1 — Ortak ev

### Teslimatlar

- `Assets/Story/Prefabs/Home/StoryHome_Shared.prefab`
- Sağlam Story 01 varyantı.
- Çalışma Story 02 varyantı.
- Deprem öncesi Story 03 varyantı.
- Hasarlı Story 03 varyantı.

### Yapılacaklar

- Story 03 salon koordinatlarını kanonik taban yap.
- Story 01 mutfak, yardım dolabı, giriş rafı ve aile panosunu aynı tabana ekle.
- Story 02'yi ayrı 12×12 odadan çıkarıp ortak eve taşı.
- Story 03 çanta konumunu giriş rafıyla eşleştir.
- Masa, pencere, gardırop, raf ve çıkış transformlarını bütün sahnelerde aynı tut.
- NavMesh'i ortak geometri üzerinde bir kez doğrula.
- Kamera occluder ve cutaway duvar standardı kur.

### Kalite kapısı

- Üç sahnenin aynı ev olduğu ek açıklama olmadan anlaşılır.
- Story 01'de görülen her ana referans Story 03'te aynı yönde bulunur.
- 9:16 ve 9:19.5 kadrajda duvar kesmesi yoktur.

## 4. Faz 2 — Story 01

Kaynak:

- `Story_01_Rebuild_Plan.md`
- `Story_01_Playable_Script.md`
- `Story_01_Production_Blueprint.md`

### Teslimatlar

- Aile planı panosu.
- Dört bölgeli arama rotası.
- Su şişesinin tarih bandını doğrudan çevirme ve sonucu fiziksel işaretle görme.
- Kapalı sargı paketinin mühür şeridini basılı tutarak kontrol etme; çocukların tedavi/ilaç seçmediğini sahnede açıklama.
- Konsol/ağır çanta sekansı.
- Fener, radyo ve düdük işlev testleri.
- Kontrollü karanlık aile tatbikatı.
- Fermuar, askı ve giriş rafı finali.
- Yeni kamera ve ses katmanı.

### Kalite kapısı

- Kör ilk oynayış 8–10 dakika.
- En az beş farklı doğrudan dünya hareketi.
- Masa teşhir düzeni yok.
- Hiçbir zorunlu bekleme yok.
- İki hazırlık kararının daha sonraki sahne karşılığı prototipte görünür.

## 5. Faz 3 — Story 02

### Uygulama durumu

`Assets/Scenes/Story_02_RebuildPreview.unity` yayın sahnesi olarak üretildi ve Build Settings'teki dört perdelik rotaya bağlandı. Mevcut `Story_02_HomeSafety.unity` referans olarak korunuyor.

- Ortak aile evi bağlı prefab instance'ı kullanılıyor.
- 23 oynanabilir dünya etkileşiminin 13'ü nesneyi parmakla kendi hedef alanına taşıyan `DragToTarget` akışıdır.
- Can'ın okuma köşesi rafın turuncu düşme alanından ortak halıya fiziksel olarak taşınır; Story 03'te güvenli köşe veya eski noktadaki hasarlı minder/döküntü varyantı görünür.
- İlk rota testi, üç fiziksel çıkış engeli, temizlenen kapıyla Nermin'i karşılama, tahliye planı, iki doğrudan düşme alanı jesti, üç raf eşyası, raf/dolap sabitlemesi ve iki sonradan kontrol beat'i sahnede bağlıdır.
- Eski üç maddelik “risklere dokun” kontrolü ve aynı kapıyı ikinci kez sınayan ölü etkileşim rebuild akışından çıkarıldı.
- Nermin koridora fiziksel olarak yürür; finalde sesi geri gelir ve Can oyuncak arabayı cebine koyar.
- Matkap sekansında kontrol yetişkindedir; çocuk yalnız bağlantı parçası/kayış taşır.
- Altı Cinemachine kadrajı 38–50° lens aralığında görsel QA görüntüsüyle kontrol edildi.
- Story 02 yapısal testleri 6/6, baştan sona rebuild PlayMode testi 1/1 ve güncel tüm regresyon 96/96 EditMode ile 21/21 PlayMode geçti.
- 8–10 dakika hedefi ancak kronometreli kör dokunma oynanışıyla onaylanacaktır; yapısal test bu süreyi kanıtlamaz.

### İçerik değişiklikleri

- Ayrı ev sahnesini kaldır.
- Oyuncak araba rota testi ekle.
- Nermin'i kapı sahnesinde tanıt.
- Seri üçlü taşıma görevlerini farklı fiziksel eylemlere dönüştür.
- Yetişkin sabitlemelerini Timeline set-piece hâline getir.
- Sonuç anlatan uzun altyazıları kısa karakter tepkilerine indir.

### Kalite kapısı

- Story 01 çantası ve konfor eşyası aynı yerde görünür.
- Story 02 değişiklikleri Story 03 varyantlarını fiziksel olarak değiştirir.
- Çocuk matkap veya ağır mobilya kullanmaz.
- İlk oynayış 8–10 dakika.

## 6. Faz 4 — Story 03

### Uygulama durumu

`Assets/Scenes/Story_03_RebuildPreview.unity` yayın sahnesi olarak üretildi ve Build Settings'teki dört perdelik rotaya bağlandı. Mevcut `Story_03_Quake.unity` referans olarak korunuyor.

- Ortak aile evi bağlı prefab instance'ı kullanılıyor.
- Zorunlu dört güvenlik incelemesi kaldırıldı. Yalnız oyuncak tekerini takıp arabayı Can'a gönderme mikro-hikâyesi açılış omurgasıdır; radyo ve aile planı eşzamanlı isteğe bağlı anlar olarak kalır. Deprem dört görevin tamamlanmasına değil, görünmez 60–72 saniyelik dramatik pencereye bağlı olarak aniden başlar.
- Deprem toplam 48 saniyelik Timeline üzerinde üç fiziksel faza ayrıldı; toz, ışık, gevşek eşya, kapı, masa, dolap ve raf hareketleri sahnede author edildi.
- `minimumCompletionDuration` yeni akışta sıfır; 90 saniyelik deprem ve 30 saniyelik koridor tekrar döngüsü yalnız eski sahnede korunuyor.
- Deprem sonrası sekiz büyük dünya beat'i; ayakkabı çifti, Can'ın bağcıkları, afet çantası, hazırlık sonuçları ve aile planını tek rota içinde bağlıyor.
- Koridorda el tutma, ışık tarama ve hafif parçayı fiziksel taşıma oynanıyor. Soyut “sese dokun” adımı yerine kapının açıkta kalan kenarına üç kez vuruluyor; sıcak ışık ve ebeveyn cevabı geliyor. Bu cevap sürerken sahneye yerleştirilmiş tavan tozu ve mekânsal gıcırtıyı Can önce fark edip `StoryCall` tepkisiyle iç duvarı gösteriyor; son artçı eylemi Can'ın uyarısına cevap olarak oynanıyor.
- 27 doğrudan dünya etkileşiminin 5'i nesneyi parmakla kendi fiziksel hedef alanına taşıyor.
- On Cinemachine kamera 38–50° aralığında render edildi. Koridor kadrajı duvar/lento blokajından arındırıldı; koruma kadrajı iki çocuk, masa tablası ve tutulan ayağı birlikte gösteriyor.
- Story 03 testleri 8/8; güncel tüm EditMode regresyonu 96/96 ve PlayMode regresyonu 21/21 geçti.
- 9–11 dakika hedefi ancak kronometreli kör dokunma oynanışıyla onaylanacaktır; mevcut yapısal ve PlayMode testleri süreyi kanıtlamaz.

### Silinecek yapay tempo

- `introMinimumDuration` zorunluluğu.
- 90 saniyelik `quakeMinimumDuration`.
- `minimumCompletionDuration = 480`.
- 30 saniyelik `corridorWarningDuration` döngüsü.
- Aynı altyazının süre doldurmak için tekrar edilmesi.

Bu alanlar ancak yeni içerik tamamlandıktan sonra kaldırılır; sahne yarım kalmış biçimde kısaltılmaz.

### İçerik değişiklikleri

- Dört zorunlu intro incelemesini doğal aile açılışıyla değiştir.
- Deprem Timeline'ını 40–50 saniyelik çok fazlı fiziksel sekans yap.
- Deprem sonrası 15 checklist beat'ini 4–5 büyük karakter sekansına birleştir.
- Fener, dolap, raf ve çıkış flag sonuçlarını tek oda panında görünür yap.
- Koridordaki soyut “sese dokun” adımını, kapının açıkta kalan gerçek kenarına üç kez vurma ve aralıktan sıcak ışık/ebeveyn cevabı alma eylemine dönüştür.
- Koridor artçısında Can'ın öğrendiği davranışı kendiliğinden başlatmasını sağla.

### Kalite kapısı

- Deprem önceden tahmin edilemez.
- Oyuncu aktif sarsıntıda çıkışa veya merdivene gidemez.
- Kamera sarsıntısı azaltılsa bile çevre hareketi ve ses gerilimi taşır.
- Hazırlık flag kombinasyonlarının hiçbiri softlock üretmez.
- İlk oynayış 9–11 dakika; ustalaşmış tekrar daha kısa olabilir.

## 7. Faz 5 — Story 04

### Uygulama durumu

`Assets/Scenes/Story_04_RebuildPreview.unity` yayın sahnesi olarak üretildi ve Build Settings'teki dört perdelik rotaya bağlandı. Kaynak `Story_04_Evacuation.unity` referans olarak korunuyor.

- Açılıştaki görünür amber koridor şeridi ve “dinle” tıklaması kaldırıldı. Oyuncu normal dokun-yürüyle gerçek eşiği geçtiğinde trigger otomatik çalışır; Can ve Deniz'in ayrı çevre kontrolleri karakter diyaloğuyla görünür.
- Koridor, merdiven, alt sahanlık, bina önü, sokak ve toplanma alanı tek baked NavMesh rotasında bağlıdır.
- Artçı sekansı en az 10 saniye sürer; dünya navigasyonu kapanır ve korkuluğun kendisinde basılı tutma tamamlanmadan iniş açılmaz.
- Nermin akışında önce izin istenir; karton kutu, hafif köpük ve baston üç ayrı `DragToTarget` etkileşimidir. Nermin bastonunu kendisi kavrar ve çocuk kolundan çekmez.
- Bina kapısından sonra kapı önünde beklenmez; cepheden uzaklaşma checkpoint'ten önce ayrı fiziksel yaklaşma adımıdır.
- Dış rota Pandazole bina, ağaç ve yol ekipmanı prefablarıyla yoğunlaştırıldı. Eski “AÇIK ROTA” ve “SES” dünya talimatları ile cevabı ele veren turkuaz/mercan zemin şeritleri preview'den kaldırıldı. Oyuncu cam sınırından açık yan kaldırıma sürerek gevşek tabela, kayan cam ve tozla fiziksel sonucu görür; Can ardından güvenli kaldırımı sahnede işaret eder.
- Toplanma alanındaki on adımlı eşya kuyruğu kaldırıldı. Levha yaklaşmayla okunur; zorunlu akış Nermin'i görevliye ulaştırma, ilk yardım setini/temiz bezi tedavi tepsisine sürükleme, aile sinyali ve Anne'ye fiziksel yaklaşma olmak üzere dört sahne içi etkileşimdir. Radyo/megafon alana girişte fiziksel ışık ve uzamsal sesle başlar; su, battaniye, iletişim kartı ve konfor oyuncağı ayrı görev olmadan hazırlık sonucu olarak görünür.
- `BagComfortItem`, Story 04 boyunca Can'ın elindeki fiziksel oyuncakla sürer. Can kontrolü tek dokunuş yerine karakter üzerinde kısa basılı tutmadır; oyuncak yoksa ayrı görev eklenmeden izinli el tutma ve ortak nefes karşılığı çalışır.
- Radyo, ilk yardım, su, battaniye, belge ve düdük flag'lerinin her biri hazırlanmış nesne ile güvenli fallback arasında sahne varyantı üretir; eksik kombinasyon softlock oluşturmaz.
- Anne ve Baba sinyalden sonra fiziksel olarak kadraja gelir ve çocuklara döner. Oyuncu Anne'nin sahnedeki karakterine dokununca Deniz ayrı buluşma noktasına gerçekten yürür; Can takip eder, görevli masasındaki aile sayımı `2/4`ten `4/4`e döner ve rapor ancak bundan sonra açılır.
- Rota boyunca bütün zorunlu eylemler doğrudan dünya üzerinde oynanır. Toplanma finali dört zorunlu fiziksel etkileşime indirilmiştir; yalnız Can'ın tedavi malzemesi doğrudan hedef tepsisine sürüklenir, diğer hazırlık eşyaları sahne varyantıdır.
- On dört Cinemachine kamera 38–50° aralığında 9:16 ve 9:19.5 render edildi. Açılış kadrajı iki kardeş ile merdiven kapısını gösterir. Toplanma alanında geniş levha/final planından ayrı sağ çapraz bakım kamerası Deniz, Can, Nermin, görevli ve tedavi masasını aynı portre kadrajına alır; final planı Anne ile Baba'yı birlikte gösterir.
- Story 04 rebuild yapısal testleri 8/8; hazırlanmış ve tümü eksik ekipman yollarını kapsayan iki özel PlayMode testi 2/2 geçti. Güncel tüm regresyon 96/96 EditMode ve 21/21 PlayMode'dur.
- 10–12 dakika hedefi ancak kronometreli kör dokunma oynanışıyla onaylanacaktır; otomatik test süresi gerçek oyuncu süresi değildir.
- 20 Temmuz 2026'da ADB erişimi doğrulandı ancak bağlı cihaz bulunmadı; Unity 6000.0.58f2 kurulumunda Android Build Support (`AndroidPlayer`) da yoktu. Gerçek APK, Snapdragon 778G sınıfı profil, 60 FPS, bellek ve sıcak geçiş kapıları bu iki dış koşul sağlanmadan tamamlanmış sayılmaz.

### İçerik değişiklikleri

- Story 03 koridorundan kesintisiz başlangıç.
- Asansörü eşdeğer seçim gibi sunma; çevresel arıza işaretleriyle okunur yap.
- Nermin karşılaşmasını önceki ilişkiye bağla.
- Primitive dış mekânı mevcut Pandazole bina, yol, ağaç, tabela ve çevre prefablarıyla kur.
- Zemindeki “AÇIK ROTA” yazısını kaldır.
- Toplanma alanında aile gelmeden önce radyo, iletişim kartı ve sinyal payoff'larını oynat.
- Anne ve Baba'nın gerçek geliş ve birleşme sekansını ekle.
- Completion paneli birleşme sahnesinin önüne geçirme.

### Kalite kapısı

- Merdiven, bina önü, sokak ve toplanma alanı dört ayrı görsel bölge gibi okunur.
- Nermin yardımının fiziksel sınırları anlaşılır.
- Aile birleşmesi ekranda gerçekleşir.
- Story 01'den en az üç karar finalde görünür.
- İlk oynayış 10–12 dakika.

### Tam hikâye rotası — uygulandı

- `Story_Rebuild_MainMenu.unity`, `DEVAM ET` ve `YENİ HİKÂYE` seçenekleriyle bağımsız giriş sahnesidir.
- Story 01–04 rebuild sahneleri `SONRAKİ PERDE` akışıyla aynı kalıcı `StoryGameManager` oturumunu sürdürür.
- Perde değişince eski perdenin checkpoint'i sıfırlanır; çanta, mobilya ve çıkış flag'leri korunur.
- Finalde `BÖLÜM SEÇİMİ` açılır; seçilen perde kendi başlangıcından yeniden oynanırken önceki hazırlık sonuçları korunur.
- Build Settings yalnız ana menü ve dört rebuild sahnesini paketler; eski sahneler silinmeden veya ezilmeden editör referansı olarak korunur.
- Tam rota PlayMode testi aynı manager kimliğini, dört sahne yüklemesini, hazırlık sonuçlarını ve final bölüm seçimini doğrular.

## 8. Karakter asset kapısı

### Uygulanan durum

- Yerel `POLYGON Town` ve `POLYGON City Characters` Unity paketleri import edildi.
- Deniz `Character_SchoolBoy_01`, Can `Character_Son_01` kullanır.
- Anne `Character_Mother_01`, Baba `Character_Father_01` kullanır.
- Nermin `Character_Grandma_01`, toplanma görevlisi `Character_Paramedic_01` kullanır.
- Town paketinin kullanılmayan demo sahneleri ve çevre içeriği çıkarıldı; Story 03 apartman koridoru ve Story 04 dış rotası için kapı/duvar/merdiven, apartman aksesuarları, ağaç/çalı/çit, bank, çöp kutusu, sokak lambası, levhalar ve araçlardan oluşan 30 modellik curated set korundu.
- Synty materyallerinin 37/37'si URP/Lit'e dönüştürüldü ve GPU instancing açıldı.
- Dört rebuild sahnesinde Humanoid avatar ve animasyon retarget testi geçti.

### Kalite hedefi

- Deniz ve Can farklı yüz, saç, gövde oranı ve kıyafete sahip olmalı.
- İki karakter aynı humanoid animasyon kütüphanesini kabul etmeli.
- Korku, rahatlama ve konuşma için en az yüz materyali veya basit blendshape desteği tercih edilir.
- Mobil hedef için LOD veya makul üçgen sayısı bulunmalı.

### Önerilen tutarlı kadro seçeneği

20 Temmuz 2026 tarihindeki resmî Synty mağaza fiyatları tekrar satın alma sırasında doğrulanmak üzere:

1. [Synty POLYGON Town Pack](https://syntystore.com/products/polygon-town-pack) — listede indirimli 25 USD:
   - School Boy, Son, Daughter ve School Girl.
   - İki baba ve iki anne varyantı.
   - Shopkeeper ile ek yetişkin.
   - Mecanim kurulumuna uygun karakterler.
2. [Synty POLYGON City Characters Pack](https://syntystore.com/products/polygon-city-characters-pack) — listede indirimli 15 USD:
   - Grandpa ve Grandma dahil 19 temel yetişkin.
   - Paramedic, yol işçisi, dükkân çalışanı ve kalabalık için farklı şehir karakterleri.
   - Renk ve ten varyantları.

Bu ikili Deniz, Can, Anne, Baba, Nermin ve görevliyi aynı görsel dilde çözdü. Mevcut Humanoid animasyonlar dört rebuild sahnesine retarget edildi; özel entegrasyon testleri 6/6, tüm EditMode regresyonu 96/96 ve PlayMode regresyonu 21/21 geçti.

### Daha geniş çocuk çeşitliliği seçeneği

[Synty POLYGON Kids Pack](https://assetstore.unity.com/packages/3d/characters/humanoids/humans/polygon-kids-pack-art-by-synty-180629):

- Güncel listede çok daha geniş çocuk ve aksesuar çeşitliliği.
- Mecanim uyumlu.
- Animasyon içermiyor; mevcut KayKit/UAL kütüphanemiz kullanılabilir.
- Unity Asset Store'da denetim tarihinde 149,99 USD.

Bu paket çocuk ve okul NPC çeşitliliği için güçlüdür fakat yetişkin ana kadroyu tek başına çözmez. Mevcut Town + City Characters kadrosu ana hikâye için yeterli olduğundan Kids Pack şu aşamada gerekli değildir.

### Bütçesiz yol

- Mevcut modeller yalnız graybox için kalır.
- Quaternius saçları, farklı sırt çantası ve renk varyantlarıyla geçici siluet ayrımı yapılır.
- Bu yol nihai karakter kalite kapısını karşılamaz; yalnız sahne üretimini bloke etmemek içindir.

## 9. Ses asset kapısı

### Uygulanan durum

- Küçük ve izlenebilir CC0 yolu seçildi.
- OpenGameArt üzerindeki `rubberduck / 100 CC0 SFX #2` paketi 100 orijinal OGG dosyası ve CC0 kaynak kaydıyla import edildi.
- Dört rebuild sahnesindeki bütün dünya etkileşimlerine sahne üzerinde ayrı 3B `AudioSource` bağlandı.
- Ses seçimi etkileşim kimliğine ve fiziksel malzemeye göre yapılır: kapı, cam, taş/moloz, metal, ahşap, eşya, anahtar ve yaklaşma.
- Story 03 deprem Timeline'ında altı zamanlanmış darbe/döküntü cue'su bulunur.
- Story 01–03 iç mekân tonları, Story 04 uzak trafik ve dış ortam katmanları kullanır.
- Bağlantı ve lisans testleri 10/10 geçti; yeni runtime ses yöneticisi eklenmedi.

### Performans doğrulama durumu

- Editör içi 240 karelik tanısal örneklemede Story 03 p95 4,38 ms, Story 04 p95 4,27 ms verdi.
- Bu değerler Android cihaz kabulü değildir.
- Unity Test Runner aynı sayaçta her kare allocation ürettiği için editör sonucu “sıfır sürekli GC” kanıtı olarak kullanılmaz.
- Makinede Android Build Support modülü ve bağlı ADB cihazı bulunmadığından 60 FPS, 650 MB ve sıcak sahne geçişi kapıları Development Build + cihaz Profiler oturumuna açıktır.

### Ücretsiz güçlü kaynak

[Sonniss GDC 2026 Game Audio Bundle](https://gdc.sonniss.com/):

- 7,47 GB üzeri.
- 347'den fazla profesyonel ses.
- Ticari kullanım için royalty-free.
- Attribution gerektirmiyor.
- Lisans oyun, film, TV ve interaktif projelerde kullanıma izin veriyor.

Lisans:

- [Sonniss GDC Bundle License](https://sonniss.com/gdc-bundle-license/)

Dezavantajı büyük indirme boyutudur. Kullanıcı onayı olmadan 7,47 GB paket indirilmez.

### Hedefli küçük indirme yolu

- Yalnız CC0 lisanslı Freesound kayıtları aranır.
- Her dosyanın kaynak URL'si ve lisansı kaydedilir.
- Ses kalitesi ve kayıt ortamı tutarlılığı tek tek denetlenir.

### Proje lisans düzeni

```text
Assets/Story/Audio/
├── LICENSES.md
├── S01/
├── S02/
├── S03/
└── S04/
```

Kaynak ve lisans kanıtı olmayan hiçbir dış ses build'e girmez.

## 10. VFX ve çevre asset kararı

Yeni ücretli VFX veya çevre paketi ilk aşamada gerekli değildir.

Mevcut havuz:

- Cute Furniture iç mekân mobilyaları ve açılır kapak parçaları.
- Cartoon Kitchen mutfak modelleri.
- Pandazole City Town Pack içinde 139 dış çevre prefabı.
- Yol, bina, pencere, ağaç, tabela ve koni varlıkları.
- Sahneye yerleştirilmiş ParticleSystem toz ve döküntü temeli.

Gerekli çalışma yeni paket satın almak değil:

- Tek materyal/palet standardı.
- Sağlam/hasarlı prefab varyantları.
- Doğru ölçek.
- Katmanlı ses.
- Kamera ve ışık kompozisyonu.

## 11. Teknik görev sırası

1. Ortak ev prefabı ve NavMesh.
2. Story 01 graybox.
3. Story 01 kamera kilidi.
4. Story 01 etkileşim ve Timeline.
5. Story 01 ses/ışık/VFX.
6. Kronometreli Story 01 test.
7. Story 02 ortak eve taşıma.
8. Story 03 yapay süreyi içerikle değiştirme.
9. Story 04 dış rota ve aile finali.
10. Karakter asset değişimi ve bütün animasyon retarget QA.
11. Tam 35–42 dakika ardışık oynayış.
12. Android performans, kayıt ve checkpoint regresyonu.

## 12. Tam oyun kabul kriterleri

- Dört perde aynı hikâye ve karakter ilişkisini sürdürür.
- İç mekân coğrafyası tutarlıdır.
- Toplam kör oynayış 35–42 dakika.
- Hiçbir süre minimum sayaçla doldurulmaz.
- Her perdede farklı bir ana mekanik ve set-piece vardır.
- En az üç Story 01 kararı Story 03/04'te görünür sonuç üretir.
- Nermin finalden önce tanıtılır.
- Anne ve Baba ile birleşme sahnede oynanır.
- Sayısal skor yerine davranış raporu vardır.
- UI üzerindeki eylem düğmeleri yerine doğrudan dünya etkileşimi kullanılır.
- 9:16 ve 9:19.5 kamera/safe-area QA tamamlanır.
- Diyalog sırasında karakter hareket etmez.
- Her checkpoint uygulama kapatıp açıldıktan sonra aynı fiziksel state'i kurar.
- Hedef Android cihazda 60 FPS, kare başına sürekli GC allocation olmaması ve 650 MB altı bellek hedeflenir.
