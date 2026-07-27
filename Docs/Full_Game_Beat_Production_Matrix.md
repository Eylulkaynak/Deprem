# Deprem — Beat ve Üretim Matrisi

## Belgenin amacı

Bu belge dört perdenin yalnız hikâye özetini değil, sahnede gerçekten üretilecek içeriğini tanımlar. Her beat için oyuncu fiili, dramatik işlev, kamera, ses/VFX, kayıt noktası ve sonraki perdedeki karşılık birlikte düşünülür.

Süreler zorunlu bekleme değildir. Oyuncu eylemi tamamladığında beat ilerler. Altyazı okunurken güvenlik açısından hareketin kapanması gereken birkaç kısa sinematik dışında kontrol oyuncuda kalır.

## Ortak oynanış dili

| Oyuncunun gördüğü | Oyuncunun yaptığı | Kullanım yeri |
|---|---|---|
| Yerde veya rafta gerçek nesne | Nesnenin kendisini tutup görünür hedefe sürükler | Pil–fener, kart–pano, eşya–çanta, oyuncak–rota |
| Kapak, çekmece, fermuar, kapı | Nesnenin üzerinde yönünde sürükler | Açma, kapatma, çekme |
| Radyo düğmesi veya bağ | Nesnenin üstünde yatay/dairesel hareket yapar | Frekans ayarı, kayış sıkma |
| Can, masa ayağı, korkuluk | Dünya nesnesinin üzerinde basılı tutar | Sakinleştirme, Çök–Kapan–Tutun, artçı bekleme |
| Yürünebilir zemin | Hedef noktaya dokunur | NavMesh üzerinde ilerleme |
| Tehlikeli veya ulaşılamayan yer | Fiziksel ramak kala görür; yakın checkpoint'e döner | Pencere, aktif sarsıntıda kapı, döküntü |

Ortada “tıkla”, “kaydır” veya “şunu yap” düğmesi bulunmaz. İnce hedef şeridi niyeti söyler; fiilin nasıl yapılacağını nesnenin animasyonu, tutamağı, ışığı ve karakter bakışı anlatır.

## Kamera dili

| Kod | Kadraj | Kullanım |
|---|---|---|
| `WIDE` | 42–48°, oyuncu alt üçlüde | Serbest dolaşma ve mekânsal hafıza |
| `PAIR` | 40–46°, Deniz ve Can aynı planda | Kardeş ilişkisi |
| `OBJECT` | 38–44°, el–nesne–hedef birlikte görünür | Fiziksel etkileşim |
| `RISK` | 44–50°, tehlike ve güvenli alternatif aynı planda | Ramak kala ve güvenlik sonucu |
| `COVER` | 38–42°, masa tablası, ayak ve iki çocuk görünür | Çök–Kapan–Tutun |
| `ROUTE` | 44–50°, ön plan rota ve uzak hedef | Koridor, merdiven, sokak |
| `REUNION` | 40–46°, önce çocuklar sonra aile | Final |

Kameralar doğrudan transform oynatmaz. Her bölge bestelenmiş Cinemachine Camera kullanır; Timeline yalnız öncelik ve hedef değiştirir.

---

## Story 01 — Aile planı ve çanta

Hedef süre: **8:30–9:30**

| Beat | Süre | Oyuncu fiili | Hikâye ve fiziksel sonuç | Kamera | Ses / VFX | Kayıt ve karşılık |
|---|---:|---|---|---|---|---|
| `S01_00_HomeLife` | 0:00–0:50 | Odada serbestçe dolaşır; Can'ın oyuncağına veya aile fotoğrafına bakabilir | Aile gündelik hayatın içindedir. Güvenli masa, pencere, gardırop ve çıkış doğal kompozisyonda tanıtılır | `WIDE` → `PAIR` | Ev ambiyansı, uzaktaki trafik, oyuncak tekeri | `PreparationStart`; Story 03 mekân hafızası |
| `S01_01_FamilyMap` | 0:50–1:45 | Toplanma simgesini ve Melek Teyze kartını doğrudan panodaki yuvalarına yerleştirir | Plan bir liste değil, ailece yapılan fiziksel harita olur. Can kendi sarı simgesini seçer | `OBJECT`, ardından `PAIR` | Mıknatıs/kâğıt sesi, kalem, kısa onay motifi | Mevcut `BagNotebook` ve `BagDocuments`; yeni flag ancak gerçek dallanma onaylanırsa |
| `S01_02_LightKit` | 1:45–2:35 | Giriş dolabını çeker; pilleri fenerin içine yerleştirir; düdüğü Can'ın askısına takar | Fener ilk kez gerçekten yanar. Düdük Can'ın kişisel sorumluluğuna dönüşür | `OBJECT` | Çekmece, pil klikleri, fener anahtarı, kısa düdük | `BagFlashlight`, `BagBatteries`, `BagWhistle`; Story 03/04 karşılığı |
| `S01_03_Radio` | 2:35–3:15 | Radyonun düğmesini doğrudan çevirerek parazit içinden net yayını bulur | Deniz hızla çevirmek yerine dinlemeyi öğrenir. Radyo çalışan araç olarak doğrulanır | `OBJECT` → `PAIR` | Statik parazit, frekans geçişi, kısa resmî yayın | `BagRadio`; Story 04 toplanma alanı bilgisi |
| `S01_04_WaterFood` | 3:15–4:00 | Mutfak alt dolabını açar; su şişesinin kendi `SKT` bandını yana çevirip tarihi ve kapağı kontrol eder; sonra küçük suyu ve dayanıklı gıdayı çantaya taşır; cam şişeyi deneyince ağırlık ve çarpışma sonucu görür | Can'ın “büyük olan daha iyi” varsayımı, taşıyabilme ve sızdırmazlık üzerinden düzelir. Doğru cevap kartı yerine şişe üzerinde fiziksel kontrol ve sonuç görülür | `WIDE` → `OBJECT` | Dolap kapağı, etiket sürtünmesi, şişe tıkırtısı, çanta kumaşı | Tarih kontrolü tamamlanmadan eşya sürüklemeleri açılmaz; `BagWater`, `BagFood`; Story 04 bekleyiş karşılığı |
| `S01_05_AidDocuments` | 4:00–4:50 | Kapalı sargı paketinin mühür şeridinde doğrudan basılı tutar; fiziksel onay işaretini gördükten sonra ilk yardım kutusunu ve iletişim dosyasını çantaya yerleştirir | Çocuk tedavi veya ilaç seçimi yapmaz; kapalı malzemeyi bulup sorumlu yetişkine ulaştırma rolünü öğrenir | `OBJECT` | Paket/plastik hışırtısı, fermuar, kâğıt | Mühür kontrolü tamamlanmadan ilgili eşyalar açılmaz; `BagFirstAid`, `BagDocuments`; Story 04 görevliye teslim |
| `S01_06_ConsoleConflict` | 4:50–5:55 | Can'ın sakladığı konsolu çantadan fiziksel olarak çıkarır; çantayı kaldırmayı dener; küçük konfor eşyasını dış cebe yerleştirir | Çanta ağırlaşır ve Deniz sendeleyerek sonucu gösterir. Deniz Can'ı küçümsemek yerine korkusunu anlar | `PAIR` → `OBJECT` | Ağır fermuar, kayış gerilmesi, sessizlik, yumuşak ilişki motifi | Seçilecek konfor eşyası Story 03 ve 04'te görünür |
| `S01_07_ClothesAndFit` | 5:55–6:35 | Hafif battaniyeyi rulo yapar; iki askıyı doğrudan çekip eşitler; çantayı iki omuzla dener | Taşıma biçimi karakter animasyonuyla görünür. Eller serbest kalır | `OBJECT` → `WIDE` | Kumaş, toka, omuz kayışı | `BagBlanket`, `BagClothing`, `BagFitted` |
| `S01_08_DarkDrill` | 6:35–8:05 | Anne ışığı kontrollü kapatınca çantaya değil önce Can'a gider; feneri açar; aile planındaki çıkışa birlikte yürür; düdük sesini takip eder | Story 03'ün duygusal ve mekânsal provasıdır. Yanlışlıkla pencereye yönelirse keskin cam gölgesi güvenli ramak kala üretir | `WIDE` → `PAIR` → `ROUTE` | Elektrik anahtarı, karanlık oda tonu, fener foley, ayak sesi, düdük | Araçların aynı perde içinde ilk gerçek kullanımı |
| `S01_09_BagHome` | 8:05–9:20 | Çantayı kapatır ve çıkışı kapatmadan alçak giriş rafına bırakır; aile panosuna son kez bakar | Kamera açıldığında masa, pencere, gardırop, çıkış ve çanta aynı kadrajda görülür. Anne, deprem sırasında çantaya koşulmayacağını söyler | `OBJECT` → `WIDE` | Fermuar, raf teması, sakin ev motifi dönüşü | `BagReady`, `PreparationComplete`; Story 02/03 başlangıç durumu |

### Story 01 içerik kalite kapısı

- En az altı farklı fiziksel fiil vardır; aynı çantaya sürükleme dört kategori boyunca tekrarlanmaz.
- Fener, radyo ve düdük aynı perde içinde çalışır.
- Can'ın konsol anı oynanabilir çatışmadır; sıradan yanlış eşya değildir.
- Karanlık tatbikat en az 75 saniye gerçek yön bulma ve kardeş koordinasyonu içerir.
- Story 03'te kullanılacak beş mekânsal referans final kadrajında birlikte görünür.

---

## Story 02 — Evi birlikte güvenli yap

Hedef süre: **8:00–10:00**

| Beat | Süre | Oyuncu fiili | Hikâye ve fiziksel sonuç | Kamera | Ses / VFX | Kayıt ve karşılık |
|---|---:|---|---|---|---|---|
| `S02_00_ReturnHome` | 0:00–0:50 | Aynı eve girer; Story 01 çantasını ve konfor eşyasını görür | Mekân ve karar sürekliliği açıklamasız anlaşılır | `WIDE` | Gündüz ev ambiyansı, uzakta komşu sesi | `HomeSafetyStart` |
| `S02_01_ToyRouteTest` | 0:50–2:05 | Can'ın oyuncak arabasını girişe doğru iter; araba ayakkabı, paket ve gevşek oyuncakta takılır; Deniz her engeli gerçek yerine taşır | Çıkış temizleme bir kontrol listesi değil, gözle görülen rota testidir | `ROUTE` → `OBJECT` | Tekerlek, karton sürtünmesi, ayakkabı, kapı | `ExitCleared` için hazırlık; Story 03 açık rota |
| `S02_02_NerminVisit` | 2:05–3:10 | Temizlenen kapıyı açar; Nermin'in düşürdüğü zarfı yerden alıp ona verir; bina planını aile panosuna asar | Ayrı bir kapı yayı testi tekrarlanmaz. Nermin Story 04'te rastgele NPC olmaktan çıkar; bastonuyla koridora yürür | `PAIR` | Kapı zili, koridor ambiyansı, baston ucu | Nermin ilişkisi görsel olarak kurulur; yeni flag gerektirmez |
| `S02_03_RiskFootprints` | 3:10–4:25 | Rafın ve uzun dolabın üstünden zemine doğru parmağını çeker; Can'ın minder ve çizgi romanından oluşan okuma köşesini turuncu alandan ortak halının güvenli köşesine taşır | Risk yalnız zemin işareti kalmaz; Can'ın gündelik yaşamındaki yer fiziksel olarak değişir. Story 03 hazırlanmış/hazırlanmamış varyantı bunu gösterir | `RISK` → `PAIR` | Kısa yüzey tarama sesi, minder sürtünmesi, Can tepkisi | `HomeExitCleared`; güvenli okuma köşesi / eski köşe döküntüsü |
| `S02_04_ShelfWeight` | 4:25–5:35 | Ağır kitabı alt rafa indirir; saksıyı güvenli yere taşır; çerçeveyi duvara yakın yuvaya yerleştirir | Üç nesne üç farklı fiziksel hedefe gider. Rafın ağırlık merkezi değişir | `OBJECT` | Kitap, seramik, çerçeve, raf gıcırtısı | `HomeShelfPrepared` |
| `S02_05_AdultFix` | 5:35–6:45 | Sabitleme parçasını Anne'ye götürür; duvardaki güvenli noktayı işaretler; mesafe koyup yetişkin işlemini izler | Çocuk matkap kullanmaz. Toz, matkap sesi ve metal parça fiziksel sonuç verir; turuncu alan teal sınıra döner | `OBJECT` → `RISK` | Matkap, vida, metal, hafif toz | `ShelfSecured`; Story 03 raf varyantı |
| `S02_06_Wardrobe` | 6:45–7:55 | Dolabı kontrollü iterek sallanmayı hisseder; iki ankraj noktasını seçer; kayışı yetişkine verir | Dolap önce gerçekten oynar, sonra sabitlenince aynı itişte kalır; düşme alanı teal sınıra döner | `RISK` → `OBJECT` | Ahşap gıcırtısı, metal toka, matkap | `WardrobeSecured`; Story 03 dolap varyantı |
| `S02_07_FinalRoute` | 7:55–9:30 | Oyuncak arabayı aynı başlangıçtan eşiğe kadar sürükler | Kamera tüm rotayı tek kadrajda gösterir; Can arabayı yerden alıp cebine koyar, Nermin'in koridor sesi geri gelir | `ROUTE` → `PAIR` | Kesintisiz teker sesi, kapı mandalı, koridor yankısı | `HomeSafetyComplete`; Story 03 rota/payoff |

### Story 02 içerik kalite kapısı

- Story 01 ve Story 03 ile aynı ev geometrisi kullanılır.
- Oyuncak araba ilk testte en az üç fiziksel engele takılır, final testinde kesintisiz ilerler.
- Nermin oyuncuyla yüz yüze tanışır ve baston/merdiven ihtiyacı görsel olarak kurulur.
- Raf ve dolabın düşme alanları nesnenin üstünde yapılan aşağı çekme jestiyle görünür olur; sonuç yalnız renkle değil, sabitleme davranışı ve diyalogla da anlatılır.
- Ağır sabitlemenin tamamını yetişkin Timeline'ı yapar.
- Raf, dolap ve çıkış kararlarının Story 03 varyantları editörde tek tek önizlenebilir.

---

## Story 03 — Yanımda kal

Hedef süre: **9:00–10:30**

| Beat | Süre | Oyuncu fiili | Hikâye ve fiziksel sonuç | Kamera | Ses / VFX | Kayıt ve karşılık |
|---|---:|---|---|---|---|---|
| `S03_00_OrdinaryMoment` | 0:00–1:10 | Kaçan tekeri doğrudan oyuncak arabaya takar ve arabayı Can'a geri gönderir. Radyo düğmesi ile aile planı aynı odada isteğe bağlı etkileşim olarak kalır | Dört maddelik tutorial dizisi yoktur. İki parçalı kardeş mikro-hikâyesinden sonra oyuncu serbest kalır; deprem ekranda sayaç veya görev işareti olmadan 60–72 saniyelik dramatik pencere içinde aniden başlar | `WIDE` → `PAIR` | Story 01 radyo motifi, mutfaktan ebeveyn sesi, oyuncak tekeri | Sahne hafızası; zorunlu dört etkileşim kapısı yok |
| `S03_01_FirstShock` | 1:10–1:22 | İlk düşük darbede Can'a dokunup “benimle kal” bağını kurar | Oyuncak araba yuvarlanır, bardak kayar, lamba sallanır. Uzun mesafe hareket kapanır | `PAIR` → `RISK` | İlk darbe, cam titreşimi, raf gıcırtısı | `QuakeStart`, `SiblingCalmed` |
| `S03_02_ReachCover` | 1:22–1:34 | Yalnız kol mesafesindeki güvenli masaya yönelir; pencere/kapı denemesi ramak kala üretir | Güvenli rota açık kalır; yanlış eylem yaralanma göstermez | `RISK` → `COVER` | İkinci darbe, pencere çatırtısı, kapı vurması | Güvenli geri dönüş, hata sayısı |
| `S03_03_DropCoverHold` | 1:34–2:00 | Can'ın başını korur ve masa ayağını dünya üzerinde basılı tutar; değişen sarsıntı yönünde tutuşu yeniler | 40–50 saniyelik deprem üç fiziksel faz taşır. Dolap/raf flag'leri bu sırada görünür sonuç üretir | `COVER` | Katmanlı rumble, impulse, toz, gevşek eşya darbeleri | `UnderCover`; `WardrobeSecured`, `ShelfSecured` |
| `S03_04_SilenceAndCheck` | 2:00–3:00 | Sessizlikte Can'a seslenir; tek bütüncül kontrol hareketi yapar; ebeveyne ses verir | Üç ayrı Can kontrolü yerine bir anlaşılır kardeş sekansı. Ebeveyn sesi engelin arkasından gelir | `PAIR` | Tinnitus tonu, toz, nefes, uzaktan anne | `PostQuake` |
| `S03_05_SafeFeetAndLight` | 3:00–4:15 | Cam sınırını ışıkla okur; ayakkabıları ayağına geçirir; Can'ın ayakkabısına tek bağ hareketiyle yardım eder | Dört ayakkabı hotspot'u yerine tek sahne. Fener yoksa acil ışık titreşimli ama geçilebilir rota verir | `OBJECT` → `RISK` | Cam çıtırtısı, ayakkabı, fener veya acil ışık buzz | `BagFlashlight` varyantı; softlock yok |
| `S03_06_BagAndExit` | 4:15–5:35 | Çantayı giriş rafından alır; kayışını omzuna geçirir; kapı çevresini gözle kontrol edip gerçek kapıyı açar | Çanta hazırsa kolay alınır; çıkış temizlenmemişse geçilebilir dar yan rota oluşur | `ROUTE` | Kayış, fermuar, kapı kolu, koridor uğultusu | `BagReady`, `ExitCleared` |
| `S03_07_SeparationChoice` | 5:35–6:55 | Can ebeveyne dönmek isteyince onunla göz hizasına iner; aile panosunu/feneri işaret ederek birlikte koridora yönelir | Deniz'in kontrol etme dürtüsü güven verme davranışına dönüşür. Planın duygusal anlamı görünür | `PAIR` | Müzik ilk kez yükselir, ebeveynin sakin yönlendirmesi | Karakter yayı orta noktası |
| `S03_08_CorridorAftershock` | 6:55–8:20 | Koridorda ışığı takip eder; devrilmiş dolaba dokunmadan kapının açıkta kalan kenarına üç kez vurup ebeveyne ses verir; artçı başladığında Can bu kez Deniz'i duvardan uzak güvenli noktaya çağırır | Üç fiziksel vuruştan sonra kapı aralığında sıcak ışık ve ebeveyn cevabı görünür. Can öğrendiği davranışı kendi başlatır; dilim boş uyarı döngüsüyle değil rol değişimiyle biter | `ROUTE` → `PAIR` | Ahşap kapı vuruşları, sıcak ışık, floresan buzz, artçı darbesi | `CorridorReached`; Story 04 kesintisiz geçiş |
| `S03_09_Transition` | 8:20–9:20 | Artçı durunca merdiven kapısına birlikte ilerler | Kamera koridordan merdiven boşluğunu açar; tahliye ancak sarsıntı bitince başlar | `ROUTE` | Merdiven yankısı, uzaktaki komşular, alarm | `EvacuationStart` |

### Story 03 içerik kalite kapısı

- `introMinimumDuration`, `quakeMinimumDuration`, `minimumCompletionDuration` ve `corridorWarningDuration` içerik süresi üretmez.
- Aktif sarsıntı yaklaşık 40–50 saniyedir ve en az üç görsel/işitsel faz taşır.
- Masa altı kamerasında iki çocuk, masa tablası, tutulan ayak ve ana tehlike aynı anda okunur.
- Post-deprem kontrolü 4–5 büyük sekansı geçmez.
- Can koridor artçısında doğru davranışı oyuncudan önce başlatır.

---

## Story 04 — Tahliye ve buluşma

Hedef süre: **10:00–11:30**

| Beat | Süre | Oyuncu fiili | Hikâye ve fiziksel sonuç | Kamera | Ses / VFX | Kayıt ve karşılık |
|---|---:|---|---|---|---|---|
| `S04_00_StairDoor` | 0:00–1:15 | Story 03 koridorundan normal dokun-yürüyle devam eder; gerçek kapı eşiğini geçince dinleme anı otomatik başlar, ardından merdiven kapısını açar; asansör panelinin karanlık/arıza hâlini çevrede görür | Yerde amber görev şeridi veya “dinle” hotspot'u yoktur. Can tavanı, Deniz merdiven yolunu fiziksel eşik geçişi içinde kontrol eder. Asansör eşdeğer seçim kartı değildir | `ROUTE` | Ayak sesi, koridor, alarm, arızalı panel rölesi | `StairwellEntered`, `ElevatorAvoided` |
| `S04_01_Stairs` | 1:15–3:20 | Korkuluk tarafında iner; gevşek parçanın ardından artçı başlayınca korkuluğa tutunur ve Can'ı yanında sabitler | Kamera düşme hissi vermeden dikey derinliği gösterir. Artçı bitmeden iniş açılmaz | `ROUTE` → `RISK` | Basamak, korkuluk, artçı, beton tozu | `AftershockHeld`, `StairRouteCompleted` |
| `S04_02_Nermin` | 3:20–5:30 | Tanıdığı Nermin'e seslenir; bastonunu erişilebilir yerden alıp verir; çantayı taşımaya kalkmadan kapıya kadar yanında yavaş yürür | Yardımın sınırı öğretilir. Story 02 tanışıklığı diyalogu kısaltır ve duyguyu güçlendirir | `PAIR` → `ROUTE` | Baston, nefes, komşu kapıları, kısa ilişki motifi | `NeighborAssisted`, `NeighborHelped` |
| `S04_03_BuildingFront` | 5:30–6:35 | Bina kapısını açar; cepheden güvenli mesafeye gider; düşebilecek tabela altında kalmadan açık noktaya geçer | Dışarı çıkmak final değildir; bina cephesinden uzaklaşma fiziksel rota olur | `WIDE` → `RISK` | Dış ortam açılması, siren, kuşların kalkışı, tabela gıcırtısı | `BuildingExited` |
| `S04_04_Street` | 6:35–8:20 | Pandazole sokak prefabları içinde cam sınırından açık yan kaldırıma doğru sürerek riski okur; tabela gıcırdar, bir cam parçası kayar ve Can güvenli kaldırımı işaret eder; ardından camlı cepheden uzak rotayı seçer | Yerde “AÇIK ROTA” yazısı veya güvenli/tehlikeli cevabı veren renk şeritleri yoktur. Koniler, görevli, bariyer, cam hareketi ve Can'ın işareti yön verir | `ROUTE` | Şehir ambiyansı, tabela gıcırtısı, kayan cam, toz, uzaktan düdük, görevli çağrısı | `StreetRouteCleared`; `BagWhistle` varyantı |
| `S04_05_Assembly` | 8:20–10:15 | Levhaya yaklaşınca Can Story 01 simgesini kendiliğinden tanır; ayrı eşya kontrol listesi açılmaz. Oyuncu Nermin'in yanında basılı tutarak onu görevliye ulaştırır, ilk yardım setini/temiz bezi fiziksel tedavi tepsisine sürükler ve düdük/aile işaretiyle ebeveynlere ses verir | Dört zorunlu dünya etkileşimi vardır: Nermin, Can'ın tedavi malzemesi, aile sinyali ve Anne'ye yaklaşma. Radyo/megafon alana girişte uzamsal olarak başlar; su, battaniye, iletişim kartı ve konfor oyuncağı hazırlık flag'lerine göre görünür çevre sonucu olur, ayrı görev kuyruğu olmaz | `WIDE` → `PAIR` → `OBJECT` → `REUNION` | Kalabalık yatağı, oyuncak tekeri motifi, otomatik fiziksel radyo ayarı, uzamsal anons, tedavi tepsisi, aile sinyali | `AssemblyAreaReached`, `AssemblyHeadcountComplete`; `BagComfortItem` ve diğer çanta flag sonuçları |
| `S04_06_Reunion` | 10:15–11:30 | Kalabalıkta ebeveyn sesine döner; Can'ın düdüğüne cevap gelir; oyuncu Anne'nin sahnedeki karakterine dokunup son birkaç adımı NavMesh üzerinde ailesine yürür | Ebeveynler kadraja fiziksel olarak girer; Anne ve Baba çocuklara dönüktür. Görevli masasındaki sayım işaretleri `2/4`ten `4/4`e geçer. Deniz “Birlikte geldik” der | `REUNION` | Aile motifi, düdük cevabı, ambiyansın yumuşaması | `AssemblyHeadcountComplete`; dört perde tamamlanır ve davranış raporu açılır |

### Story 04 içerik kalite kapısı

- Nermin yardım sekansı Story 02 tanışıklığına açıkça geri döner.
- Dış rota primitive kutularla değil mevcut şehir prefablarıyla bestelenir.
- Toplanma alanında en az üç hazırlık flag'i oynanabilir sonuç üretir.
- Anne ve baba yalnız altyazıda kalmaz; finalde fiziksel olarak görünür.
- Buluşma merkezî onay/hold düğmesiyle bitmez; karaktere dokunma gerçek yaklaşma üretir ve kayıt panosunda fiziksel sonuç görünür.
- Final raporu sayı/puan değil, “Hazırlık”, “Koruma”, “Yardımlaşma” davranış sonuçlarını gösterir.

---

## Hazırlık–sonuç zinciri

| Önceki karar | Yakın sonuç | Story 03 sonucu | Story 04 sonucu | Eksikse |
|---|---|---|---|---|
| `BagFlashlight` + `BagBatteries` | Karanlık tatbikat rahat geçer | Cam ve çıkış rotası güçlü ışıkla okunur | Merdiven/işaret bulma kolaylaşır | Acil ışık yanar; rota daha dar ve yavaştır |
| `BagRadio` | Frekans testi yapılır | Elektrik kesilince sessiz kalır ama çantada görünür | Resmî anons ve toplanma bilgisi alınır | Bilgi görevli üzerinden gelir |
| `BagWhistle` | Can düdüğü kendi askısına takar | Deprem sırasında üzerinde görünür | Sokakta yön işareti/final cevap sesi olur | Görevli sesi ve görsel işaret kullanılır |
| `BagComfortItem` | Deniz küçük oyuncak arabayı Can'a verir | Deprem sonrasında oyuncak Can'ın elinde görünür; kardeş kontrolü diyaloğunda sakinleşme karşılığı üretir | Can'ın taşıdığı kişisel eşya olarak kalır | Deniz'in elini tutarak aynı güvenli akış sürer |
| `BagWater` | Ağırlık–sızdırmazlık sonucu görülür | Çantada fiziksel hacim üretir | Toplanma alanında kontrollü kullanılır | Görevli su dağıtım noktasına yönlendirir |
| `BagFirstAid` | Çocuk seti bulup yetişkine verir | Çantada görünür | Sorumlu yetişkine teslim edilir | Toplanma alanı ilk yardım noktasına gidilir |
| `BagBlanket` / `BagClothing` | Çanta ağırlığı dengelenir | Dış cepte görünür | Can/Nermin beklerken sıcak kalır | Toplanma alanı battaniyesi kullanılır |
| `WardrobeSecured` | Aynı itişte dolap sabit kalır | Dolap yerinde kalır | — | Yan geçidi kapatır; ana rota açık kalır |
| `ShelfSecured` | Raf ağırlık merkezi ve sabitleme görülür | Raf düşmez, yalnız üstteki hafif parçalar oynar | — | Döküntü dar görüş üretir; ana rota açık kalır |
| `ExitCleared` | Oyuncak araba kesintisiz kapıya gider | Çanta ve kapı rahat erişilir | — | Geçilebilir yavaş yan rota açılır |
| `NeighborAssisted` | — | — | Nermin güvenle çıkışa ulaşır; final raporuna yansır | Nermin görevli yetişkinle ilerler; oyun kilitlenmez |

## Yeni flag kararı

Yeni flag yalnız sonraki perdede görünür bir dallanma üretiyorsa eklenir. Salt diyalog değişikliği için yeni veri sözleşmesi açılmaz.

Onay bekleyen olası flag'ler:

- `FamilyPlanReady`: Story 04 toplanma simgesi eşleştirmesi gerçekten farklı çalışacaksa.
- `ComfortItemPacked`: Story 03 ve 04'te seçilen fiziksel prefabı sürdürmek için.
- `CanHasWhistle`: `BagWhistle` çantadaki düdükten ayrı tutulacaksa.
- `BagWeightBalanced`: Story 03 çanta alma animasyonu ve hızını değiştirecekse.

Öneri: ilk üretimde mevcut flag'ler kullanılsın; yalnız konfor eşyasının prefab kimliği için küçük bir seçim verisi eklenmesi değerlendirilsin.

## Teknik karşılık ve runtime sınırı

Mevcut sistem doğrudan şu fiilleri destekler:

- `Tap`
- `RepeatedTap`
- `SwipeDown`
- `SwipeHorizontal`
- `Approach`
- `WorldHold`
- `DragToBag`

Mevcut `DragToBag`, `BagDropZone.Instance` hedefine bağlıdır; pil–fener, kart–pano veya oyuncak–rota gibi genel dünya hedeflerini desteklemez.

Üretim sırası:

1. Mevcut jestler ve Timeline ile bütün sabit yönlü hareketler prototiplenir.
2. Nesnenin oyuncunun parmağı altında serbestçe görünür sokete taşınması kalite kapısı için zorunlu kalırsa tek bir genel `WorldSocketDrag` bileşeni değerlendirilir.
3. Bu bileşen yalnız giriş ve soket doğrulaması yapar; sonuç animasyonu, ses, flag ve kamera UnityEvent/Timeline üzerinden sahnede kalır.
4. Kapı, pil, kart veya oyuncak için ayrı input scriptleri yazılmaz.

## Tam oyun süre hesabı

| Perde | Alt hedef | Üst hedef |
|---|---:|---:|
| Story 01 | 8:30 | 9:30 |
| Story 02 | 8:00 | 9:00 |
| Story 03 | 9:00 | 10:30 |
| Story 04 | 10:00 | 11:30 |
| **Toplam** | **35:30** | **40:30** |

Bu hesaba menü, yükleme, başarısız ramak kala tekrarı veya oyuncunun serbest inceleme süresi dahil değildir.
