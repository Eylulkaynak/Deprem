# Story 01 — Sahne Üretim Şartnamesi

## 1. Kanonik ev coğrafyası

Story 03'ün mevcut salon ölçüleri ortak evin kanonik tabanı olarak korunur:

| Unsur | Kanonik konum / ölçü |
|---|---|
| Salon zemini | Merkez `(0, -0.12, 0.25)`, ölçü `(10, 0.24, 11.5)` |
| Salon–koridor kapısı | Merkez `x=2.5, z=6`, 2.2 m net açıklık |
| Güvenli masa | `(0.45, 0, 0.65)` |
| Pencere | `(-2.2, 1.85, 5.83)` |
| Gardırop | `(-4.2, 0, 5.38)` |
| Aile koltuğu | `(-2.78, 0, -3.72)` |
| Mevcut alçak dolap | `(3.72, 0, -3.92)` |
| Koridor | `x=0.98–4.02`, `z=6–12.8` |

Ortak prefab yeni bir dosyada üretilecektir:

- `Assets/Story/Prefabs/Home/StoryHome_Shared.prefab`

Sahneye özel durumlar ortak geometrinin altında tutulmaz:

- `Story01_SafeState`
- `Story02_WorkState`
- `Story03_QuakeState`
- `Story03_DamagedState`

Story 01'de kullanılan her ana mekânsal işaret Story 03'te aynı konumda kalır.

## 2. Önerilen yeni ev bölgeleri

### Salon

Mevcut güvenli masa, koltuk, pencere ve gardırop korunur. Masanın üstündeki oyun konsolu sakin açılışta Can'ın eylemini taşır.

### Açık mutfak

Sağ duvarın salon tarafında, yaklaşık `x=4.35–4.75`, `z=-4.45–-0.9` şeridinde kurulur. Alçak dolaplardan oluşur; güvenli masa ile mutfak arasında en az 1.15 m yürüyüş koridoru bırakılır.

### Yardım dolabı

Sağ duvarda, yaklaşık `x=4.55`, `z=0.1` noktasında alçak dolap olarak kurulur. Story 03'te devrilebilen yüksek rafın altında veya düşme hattında bulunmaz.

### Giriş rafı

Salon içindeki kapı açıklığının sağında, yaklaşık `(3.95, 0, 4.75)` noktasında kurulur. Kapı ve NavMesh geçişi açık kalır. Story 03'te afet çantası mevcut `(3.72, 0, -2.48)` konumundan bu kanonik rafa taşınacaktır.

### Aile planı panosu

Koridorun sol duvarında yaklaşık `(1.10, 1.55, 7.75)` konumunda, salondan bakıldığında okunacak açıyla kurulur.

### Gardırop girintisi

Mevcut Story 03 gardırop konumu kullanılır. Story 01'de sağlam fakat henüz Story 02 sabitleme sonucu görünmeyen nötr varyant kullanılır.

## 3. Sahne hiyerarşisi

```text
STORY_01_BAG_PREPARATION
├── Environment_StoryHomeShared
│   ├── Architecture
│   │   ├── LivingRoom
│   │   ├── EntryCorridor
│   │   ├── Kitchenette
│   │   └── SkirtingAndFrames
│   ├── Furniture_Static
│   │   ├── SafeTable
│   │   ├── FamilySofa
│   │   ├── KitchenCabinets
│   │   ├── AidCabinet
│   │   ├── EntryLowShelf
│   │   └── Wardrobe
│   ├── Props_Shared
│   ├── Navigation
│   └── CameraTargets
├── Story01_SafeState
│   ├── FamilyPlanBoard
│   ├── SearchProps
│   │   ├── SignalSet
│   │   ├── FoodSet
│   │   ├── HealthSet
│   │   └── WarmthSet
│   ├── DeviceTestSet
│   ├── ComfortChoiceSet
│   ├── BagVariants
│   └── PreparedConsequences
├── Characters
│   ├── Deniz_12
│   ├── Can_8
│   └── Anne_Ayse
├── Story01_Interactions
├── Story01_Timelines
├── Story01_Cameras
├── Story01_Lighting
├── Story01_Audio
├── Story01_VFX
├── StoryUI_Preparation
└── _StoryPreparationCore
```

## 4. Kesin yerel asset eşlemesi

### Ortak mobilya

| Kullanım | Asset |
|---|---|
| Aile koltuğu | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Furniture/Couch_11.prefab` |
| Alternatif koltuk | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Furniture/Couch_08.prefab` |
| Güvenli masa | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Furniture/Kitchen_Table_09.prefab` |
| Sehpa | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Furniture/Coffee_Table_03.prefab` |
| Gardırop gövdesi | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Furniture/Closet_02.prefab` |
| Sol gardırop kapağı | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Furniture/Closet_02_Door_01.prefab` |
| Sağ gardırop kapağı | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Furniture/Closet_02_Door_02.prefab` |
| Giriş/yardım dolabı | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Furniture/Nightstand_02.prefab` |
| Açılır dolap kapağı | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Furniture/Nightstand_02_Door.prefab` |
| Alternatif yatak odası elemanı | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Furniture/Bed_02.prefab` |

### Mutfak

| Kullanım | Asset |
|---|---|
| Ana alt dolap | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Kitchen/Kitchen_D_01.prefab` |
| Ana dolap kapakları | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Kitchen/Kitchen_D_01_Door_01.prefab`, `Kitchen_D_01_Door_02.prefab` |
| Tek kapaklı dolap | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Kitchen/Kitchen_D_08.prefab` |
| Tek dolap kapağı | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Kitchen/Kitchen_D_08_Door.prefab` |
| Alternatif çekmeceli gövde | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Kitchen/Kitchen_D_10.prefab` |
| Çekmece/kapak parçaları | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Kitchen/Kitchen_D_10_Door_01.prefab`, `Kitchen_D_10_Door_02.prefab` |
| Buzdolabı | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Kitchen/Fridge_01.prefab` |
| Buzdolabı kapakları | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Kitchen/Fridge_01_Door_01.prefab`, `Fridge_01_Door_02.prefab` |

### Hazırlık nesneleri

| Nesne | Asset |
|---|---|
| Açık çanta | `Assets/Story/Prefabs/Preparation/EmergencyBag_Open.prefab` |
| Alternatif sırt çantası | `Assets/Sprites/FBX-20260707T095721Z-3-001/FBX/Backpack.fbx` |
| Fener | `Assets/Bolum1Prefab/Item_Fener.prefab` |
| Pil | `Assets/Bolum1Prefab/Item_Pil.prefab` |
| Radyo | `Assets/Bolum1Prefab/Radio.prefab` |
| Düdük | `Assets/Bolum1Prefab/whistle.prefab` |
| Su | `Assets/Bolum1Prefab/Item_Su.prefab` |
| Cam şişe | `Assets/Bolum1Prefab/camSise.prefab` |
| Dayanıklı gıda | `Assets/Bolum1Prefab/konserve.prefab` |
| İlk yardım | `Assets/Bolum1Prefab/FirstAidKit.prefab` |
| Sabun | `Assets/Bolum1Prefab/soap.prefab` |
| Islak mendil | `Assets/Bolum1Prefab/Toallas Benzal.prefab` |
| Belge | `Assets/Bolum1Prefab/dockument.prefab` |
| Defter | `Assets/Bolum1Prefab/Notebook_01.prefab` |
| Kıyafet | `Assets/Bolum1Prefab/T-shirt.prefab` |
| Battaniye | `Assets/Bolum1Prefab/Quilt_514.prefab` |
| Oyun konsolu | `Assets/Bolum1Prefab/GameConsole_01 Variant.prefab` |
| Küçük oyuncak | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Decorations/Toy_02.prefab` |
| Alternatif küçük oyuncak | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Decorations/Toy_03.prefab` |
| Aile fotoğrafı | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Decorations/Picture_17.prefab` |
| Plan çerçevesi | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Decorations/Picture_08.prefab` |
| Plan kâğıtları | `Assets/ithappy/Cute_Furniture_Free/Prefabs/Decorations/Paper_01.prefab`, `Paper_02.prefab` |

### Karakter ve animasyon

| Kullanım | Kaynak |
|---|---|
| Deniz | `Assets/Story/Prefabs/Deniz_12.prefab` |
| Can | `Assets/Story/Prefabs/Can_8.prefab` |
| Anne | `Assets/Story/Prefabs/Preparation/Anne_Ayse.prefab` |
| Genel etkileşim, alma, kullanma | `Assets/Story/Animations/ThirdParty/KayKit/Rig_Medium_General.fbx` |
| Diz üstü çalışma ve konuşma | `Assets/Story/Animations/ThirdParty/Quaternius/UAL1_Standard.fbx` |
| Fener tutma | `UAL1_Standard.fbx` içindeki `Armature|Idle_Torch_Loop` |
| Masadan/raftan alma | `UAL1_Standard.fbx` içindeki `Armature|PickUp_Table` |
| Konuşma | `UAL1_Standard.fbx` içindeki `Armature|Idle_Talking_Loop` ve `Sitting_Talking_Loop` |
| Genel alma | KayKit içindeki `PickUp` |
| Nesne kullanma | KayKit içindeki `Use_Item` |

Yeni Mixamo indirmesi bu perde için üretim kapısı değildir.

## 5. Etkileşim kurulum tablosu

| Hikâye eylemi | Dünya nesnesi | Mevcut hareket | Sahne sonucu |
|---|---|---|---|
| Plan mıknatısını yerleştir | Fiziksel mıknatıs | `SwipeHorizontal`, `gestureTarget=MapSocket` | Timeline mıknatısı sokete taşır |
| Dolap aç | Görünür kapak/kulp | `SwipeHorizontal` | Animator kapağı 85–105° açar |
| Çekmece aç | Görünür çekmece önü | `SwipeHorizontal` | Animator çekmeceyi 0.35 m çıkarır |
| Pil kutusunu aç | Pil kutusu | `RepeatedTap` | Kapak açılır, pil görünür |
| Etiketi çevir | Su şişesi / sargı | `SwipeHorizontal` | Nesne 120–180° döner |
| Eşyayı al | Eşyanın kendi collider'ı | `Tap` veya yönlü kaydırma | Karakter alma animasyonu, kaynak görsel kapanır |
| Battaniyeyi katla | Battaniye yüzeyi | `SwipeDown` iki aşama | İki Timeline ile katlı varyanta geçer |
| Konsolu çıkar | Konsolun kendisi | `SwipeHorizontal`, hedef masa | Konsol çantadan masaya taşınır |
| Çantayı kaldır | İki askı collider'ı | `WorldHold` | Ağır/normal kaldırma Timeline'ı |
| Pili fenere tak | Pilin kendisi | `SwipeHorizontal`, hedef pil yuvası | Pil kayar, kapak kapanır |
| Radyo ayarla | Radyo düğmesi | `SwipeHorizontal` | Düğme döner, parazit yayın sesine geçer |
| Düdüğü dene | Düdüğün kendisi | `WorldHold` | Tek düdük sesi ve Can animasyonu |
| Feneri yönlendir | Üç dünya ışık odağı | `Tap` | Timeline ışık konisini ilgili odağa çevirir |
| Can'ın elini tut | Can'ın eli/kolu | `Tap` | Birlikte yürüme sekansı |
| Fermuarı kapat | Fermuar tutacağı | `SwipeHorizontal`, hedef fermuar sonu | Fermuar Timeline'ı ve kapalı çanta varyantı |
| Askıları ayarla | Sol/sağ askı | `SwipeDown` veya `SwipeHorizontal` | Ayrı askı animasyonları |
| Çantayı rafa bırak | Raf üzerindeki fiziksel hedef | `Approach` | Yerleştirme animasyonu |

Serbest sürükleme yalnızca mevcut `DragToBag` davranışının gerçekten uygun olduğu yakın planlarda kullanılacaktır. Uzak odadan çantaya uçan eşya kullanılmaz.

## 6. Manager ve checkpoint eşlemesi

Yeni tekil eşya scriptleri eklenmez. Akış `StoryPreparationDirector` manager'ı, var olan `StoryInteractable` olayları ve Timeline'lar üzerinden yönetilir.

| Checkpoint | Yeni anlamı |
|---|---|
| `PreparationStart` | Aile planı panosu tamamlandı |
| `BagInspected` | Boş çanta, cepler ve taşıma sınırı incelendi |
| `FoodPacked` | Su etiketi ve gıda fiziksel olarak kontrol edildi |
| `HealthPacked` | İlk yardım mührü ve hijyen poşeti kontrol edildi |
| `WarmthPacked` | Güncel kıyafet, battaniye ve belge dosyası hazır |
| `CommunicationPacked` | Fener, radyo ve düdük işlev testinden geçti |
| `BagFitted` | Konsol çıkarıldı, ağırlık ve iki askı doğrulandı |
| `PreparationComplete` | Karanlık tatbikatı ve raf yerleşimi tamamlandı |

`BagFlashlight`, `BagRadio` ve `BagWhistle` toplama anında değil, test tamamlanınca yazılır.

Yeni flag ekleme kararı üretim sırasında şu kurala bağlıdır:

- Sonraki perdede görünür dallanma yaratmıyorsa yeni flag eklenmez.
- Yalnız metin farkı yaratıyorsa mevcut state veya sahne varyantı tercih edilir.
- Softlock üreten bir zorunluluk kurulmaz.

## 7. Timeline listesi

Sahne içinde aşağıdaki Timeline varlıkları hazırlanır:

1. `S01_Opening_FamilyAfternoon`
2. `S01_PlanBoard_AssemblyPoint`
3. `S01_PlanBoard_ContactCard`
4. `S01_PlanBoard_Roles`
5. `S01_Cabinet_SignalOpen`
6. `S01_Kitchen_CabinetOpen`
7. `S01_AidCabinet_OpenAndInspect`
8. `S01_Wardrobe_Open`
9. `S01_Clothing_SizeCheck`
10. `S01_Blanket_Fold`
11. `S01_Console_SecretPack`
12. `S01_Bag_HeavyLift`
13. `S01_ComfortItem_Resolve`
14. `S01_Flashlight_FunctionTest`
15. `S01_Radio_Tuning`
16. `S01_Whistle_Handoff`
17. `S01_Blackout_Drill`
18. `S01_Sibling_Reunite`
19. `S01_Bag_ZipAndFit`
20. `S01_Final_EntryShelf`

Timeline'lar ortam, ışık, ses, aktivasyon ve karakter animasyonu taşır. Runtime'da component, materyal veya ParticleSystem üretilmez.

## 8. Kamera planı

Mevcut `StoryCameraZoneId` değerleri yeniden kullanılır; ilk üretimde enum genişletilmez.

| Zone | Yeni kadraj görevi |
|---|---|
| `PreparationOverview` | Aile, salon, güvenli masa ve giriş ilişkisini kuran takip planı |
| `PreparationParent` | Aile planı panosu ve üç karakteri gösteren orta plan |
| `PreparationSignal` | Giriş dolabı, fener ve pil yuvası |
| `PreparationFood` | Açık mutfak dolabı, su etiketi ve Deniz'in elleri |
| `PreparationHealth` | Alçak yardım dolabı ve ilk yardım yakın planı |
| `PreparationWarmth` | Gardırop, Can'ın beden ölçüsü ve katlama alanı |
| `PreparationWrongChoice` | Konsolun çantaya girişi ve ağır çanta sonucu |
| `PreparationBag` | Fener/radyo/düdük işlev testleri |
| `PreparationBagFit` | Deniz'in bütün bedeni ve çanta askıları |
| `PreparationExitShelf` | Karanlık tatbikatı ve finalde oda–koridor ilişkisi |

Her kamera için 9:16 ve 9:19.5 ekran görüntüsü alınır. Etkileşim nesnesi, hedef yön ve karakter eli aynı kadrajda okunmadan kamera kabul edilmez.

## 9. Ses üretim paketi

Projede kullanılabilir özgün ses havuzu Story 01 için yetersizdir. Yerel dosyalarda yalnız sakin ev tonu, deprem rumble/impact ve eski doğru/yanlış UI sesleri bulunur.

Aranacak sesler:

| Grup | Gerekli varyant |
|---|---|
| Ev ambiyansı | Gündüz iç mekân, uzak trafik, mutfak tabak sesi |
| Dolap | Ahşap kapak aç/kapat, çekmece, küçük kilit |
| Çanta | Kumaş, fermuar, askı gerilmesi, yere bırakma |
| Fener | Pil yerleştirme, plastik kapak, anahtar tıkı |
| Radyo | Güç düğmesi, parazit döngüsü, frekans geçişi |
| Düdük | Tek kısa çocuk düdüğü, üç kısa acil sinyal |
| Karakter | Çocuk adımları, kıyafet hareketi, kısa nefes ve ünlem |
| Işık | Ev anahtarı, elektrik uğultusunun kesilmesi/gelmesi |
| Nesne sonucu | Cam şişe ağırlığı, konsolun yumuşak çantaya oturması |

Dosyalar `Assets/Story/Audio/S01/` altında anlamlı adlarla saklanacaktır. Lisans kaynağı ve kanıtı `Assets/Story/Audio/LICENSES.md` içinde yazılmadan hiçbir dış ses build'e alınmaz.

## 10. Satın alma kapısı

İlk graybox ve oynanış prototipi için:

- Yeni çevre paketi: gerekmez.
- Yeni karakter paketi: gerekmez.
- Yeni animasyon paketi: gerekmez.
- Ses paketi: yüksek olasılıkla gerekir veya lisanslı ücretsiz kaynaklardan derlenmelidir.

Yeni görsel asset yalnız şu koşullardan biri kanıtlanırsa aranır:

1. Mevcut mutfak ve mobilya stilleri aynı materyal standardına getirilemiyorsa.
2. Çanta için fermuar/askı yakın planında mevcut model yeterli geometri vermiyorsa.
3. Aile panosu ve fiziksel mıknatıslar mevcut dekor parçalarından okunabilir üretilemiyorsa.

## 11. Üretim kabul listesi

- Ortak ev prefabı hem Story 01 hem Story 03 sahnesinde aynı transformlarla kullanılıyor.
- Eski dört masa ve kategori tabelaları sahneden çıkarılmış.
- Oyuncu en az dört farklı bölgeye fiziksel olarak yürüyor.
- En az beş farklı doğrudan dünya hareketi var.
- Konsol çatışması fiziksel ağır çanta sonucu üretiyor.
- Fener, radyo ve düdük toplanınca değil test edilince flag yazıyor.
- Karanlık tatbikatı üç dünya ışık odağıyla tamamlanabiliyor.
- Diyalog açıkken devam eden rota iptal ediliyor ve yeni hareket girişi alınmıyor.
- Story 03'te çanta, masa, pencere ve gardırop Story 01 ile aynı yerde.
- Hazırlık seçimi eksik olsa bile sonraki perde softlock olmuyor.
- İlk kör oynayış 8–10 dakika.
- Zorunlu bekleme veya tam ekran öğretici yok.
- Kamera QA, checkpoint restore ve Android performans testi tamamlanmış.
