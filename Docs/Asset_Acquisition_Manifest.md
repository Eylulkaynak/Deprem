# Deprem — Asset Kullanım ve Edinim Manifesti

## Amaç

Bu belge hangi içeriğin projede zaten bulunduğunu, hangi beat'te kullanılacağını, hangi açığın dış asset gerektirdiğini ve lisans kanıtının nerede tutulacağını tanımlar. Büyük paketler doğrudan `Assets` klasörüne dökülmez; yalnız oyunda kullanılan seçili dosyalar projeye girer.

Denetim tarihi: **20 Temmuz 2026**

## Karar özeti

| Alan | Önerilen yol | Tahmini maliyet / boyut | Kullanıcı kararı |
|---|---|---:|---|
| Ana karakter kadrosu | Yerel Synty Town + City Characters | Kullanıcının indirdiği paketler | Hazır ve rebuild sahnelerinde kullanılıyor |
| Geniş çocuk NPC çeşitliliği | Synty POLYGON Kids | 149,99 USD / 60,7 MB mağaza paketi | Yalnız ihtiyaç varsa |
| Ses | Sonniss GDC 2026'dan seçili dosyalar | Paket 7,47 GB+, projeye yalnız seçilen 40–60 cue | Bekliyor |
| Küçük ses alternatifi | `rubberduck / 100 CC0 SFX #2` | 100 izlenebilir CC0 cue, 2,5 MB | Uygulandı |
| Ev ve iç mekân | Mevcut Cute Furniture + Bolum1Prefab | Yeni satın alma gerektirmez | Hazır |
| Apartman koridoru, sokak ve bina önü | Pandazole City Town + curated Synty Town seti | 139 prefab + seçilmiş 30 model | Story 03 koridoru ve Story 04 dış rotasında kullanılıyor |
| Animasyon | Mevcut KayKit + Quaternius UAL | CC0 | Hazır |
| Deprem VFX | Mevcut authored Timeline/ParticleSystem temeli | Yeni paket gerektirmez | Hazır |

## 1. Yerel çevre assetleri

### Ev mobilyaları

Kök:

`Assets/ithappy/Cute_Furniture_Free/Prefabs`

Denetimde **67 prefab** bulundu.

| Kullanım | Asset | Perde |
|---|---|---|
| Aile koltuğu | `Furniture/Couch_11.prefab` | Story 01–03 |
| Güvenli masa | `Furniture/Kitchen_Table_09.prefab` | Story 01–03 |
| Gardırop | `Furniture/Closet_02.prefab` | Story 01–03 |
| Alçak giriş rafı/dolap | `Furniture/Nightstand_02.prefab` | Story 01–03 |
| Açılabilir alçak dolap | `Furniture/Nightstand_02_Door.prefab` | Story 01 |
| Mutfak alt dolabı | `Kitchen/Kitchen_D_01.prefab` veya `Kitchen_D_08.prefab` | Story 01 |
| Açılır mutfak kapakları | `Kitchen/Kitchen_D_01_Door_01.prefab`, `Kitchen_D_01_Door_02.prefab` | Story 01 |
| Salon bitkisi | `Plants/Plants_19.prefab` | Story 01–03 |
| Oyun konsolu | `Decorations/GameConsole_01.prefab` | Story 01 |
| Aile fotoğrafı | `Decorations/Picture_17.prefab` | Story 01–03 |
| Kitaplar | `Decorations/Book_03.prefab`, `Book_08.prefab` | Story 02–03 |
| Asılı lamba | `Decorations/Light_05.prefab` | Story 01–03 |

Bu assetlerde yeni model ihtiyacı yoktur. Kalite artışı aşağıdakilerden gelecektir:

- Tutarlı ölçek.
- Aynı ev koordinatları.
- Malzeme paleti.
- Hasarlı/sağlam prefab varyantları.
- Bestelenmiş ışık.
- Nesneye bağlı ses.

### Hazırlık nesneleri

Kök:

`Assets/Bolum1Prefab`

| Kullanım | Asset |
|---|---|
| Fener | `Item_Fener.prefab` |
| Pil | `Item_Pil.prefab` |
| Radyo | `Radio.prefab` |
| Düdük | `whistle.prefab` |
| Su | `Item_Su.prefab` |
| Dayanıklı gıda | `konserve.prefab` |
| İlk yardım seti | `FirstAidKit.prefab` |
| Belge dosyası | `dockument.prefab` |
| Hafif battaniye | `Quilt_514.prefab` |
| Kıyafet | `T-shirt.prefab` |
| Cam şişe ramak kala | `camSise.prefab` |
| Konsol varyantı | `GameConsole_01 Variant.prefab` |

Bu nesnelerin modelleri yeterlidir. Eksik olan:

- Açılma/takılma/çalışma animasyonu.
- Ses.
- Işık veya ekran sonucu.
- Elde tutma/soket yerleşimi.

Bunlar ayrı model paketiyle değil Animator, Timeline, Light ve sahne sesleriyle çözülür.

### Story 02–03 güvenlik assetleri

| Kullanım | Asset / mevcut üretim |
|---|---|
| Raf gövdesi | `Assets/Sprites/FBX-20260707T100149Z-3-001/FBX/Bookcase.fbx` |
| Metal bağlantı | `StoryAuthoredPropFactory.CreateMetalBracket` ile editörde üretilen prop |
| Gardırop kayışları | Story 03 builder'ındaki authored safety strap |
| Dolap/raf sağlam ve hasarlı durumları | `Assets/Story/Prefabs/Wardrobe_*`, `Shelf_*` |
| Deprem Timeline | `Assets/Story/Generated/Timelines/Story_03_Earthquake.playable` |
| Lamba, gevşek eşya, kapı ve ışık klipleri | `Assets/Story/Generated/Timelines/*.anim` |
| Ev güvenliği klipleri | `Assets/Story/Generated/Animations/Chapters/Home_*.anim` |
| Tahliye klipleri | `Assets/Story/Generated/Animations/Chapters/Evac_*.anim` |

Mevcut klipler referans/graybox temelidir. Yeni senaryodaki zamanlama ve kadraj için editörde yeniden author edilir; runtime'da materyal veya ParticleSystem üretilmez.

## 2. Yerel dış dünya assetleri

Kök:

`Assets/Pandazole_Ultimate_Pack/Pandazole City Town Pack/Prefabs`

Denetimde **139 prefab** bulundu.

### Story 04 için seçilecek çekirdek set

| Kullanım | Prefab ailesi |
|---|---|
| Ana apartman | `Env_ResidentBuilding_01`–`06` |
| Kısa sokak | `Env_Road_Straight_01`–`03` |
| Yan kaldırım | `Env_Road_Side_01`–`04` |
| Bina önü sonu | `Env_Road_End_01`–`02` |
| Gerekirse kavşak arka planı | `Env_Road_Cross_01`–`02` |
| Yönlendirme konileri | `Prop_RoadCone_01`–`03` |
| Toplanma levhası gövdesi | `Prop_StreetSign_Empty` |
| Yaya yönlendirmesi | `Prop_StreetSign_Footpath_01`–`02` |
| Ağaçlar | `Prop_Tree_01`–`08` |
| Bitkiler | `Prop_Plant_01`–`03` |
| Çöp ve sokak ayrıntısı | `Prop_CTPTrashCan_01`–`02`, `Prop_TrashBag_01`–`03` |

### Kullanım sınırı

- Demo sahnesi toplu hâlde oyuna taşınmaz.
- 46 metrelik rota için 1 ana bina, 2 arka plan bina varyantı ve 3–4 yol modülü yeterlidir.
- Uzak binalarda collider kapatılır veya basitleştirilir.
- Tek atlas/material avantajı korunur.
- Toplanma alanı yazıyla değil levha, görevli, bariyer ve insan akışıyla okunur.

## 3. Karakter asset kararı

### Uygulanan durum

| Rol | Uygulanan kaynak | Kullanım |
|---|---|---|
| Deniz | `PolygonTown/Character_SchoolBoy_01` | Humanoid retarget doğrulandı |
| Can | `PolygonTown/Character_Son_01` | Deniz'den ayrı yüz, saç, kıyafet ve siluet |
| Anne | `PolygonTown/Character_Mother_01` | Story 01–02 ve Story 04 reunion |
| Nermin | `POLYGONCityCharacters/Character_Grandma_01` | Story 02 ve Story 04 |
| Baba | `PolygonTown/Character_Father_01` | Story 04 fiziksel reunion |
| Görevli | `POLYGONCityCharacters/Character_Paramedic_01` | Story 04 toplanma alanı |

### Uygulanan paketler

#### A — Çekirdek kadro, uygulandı

[POLYGON Town Pack](https://syntystore.com/products/polygon-town-pack):

- School Boy / Son: Deniz ve Can.
- Father 01/02: Baba.
- Mother 01/02: Anne.
- Daughter/School Girl: toplanma alanı çocuk NPC.
- `C:\Users\Gokturk\Downloads\POLYGON_Town_Unity_2021_3_v1_8_5.unitypackage` yerel paketi doğrulandı; içerik projede `Assets/PolygonTown` altında zaten bulunduğu için yinelenen import yapılmadı.

[POLYGON City Characters Pack](https://syntystore.com/products/polygon-city-characters-pack):

- Grandma: Nermin.
- Paramedic: görevli.
- Farklı yetişkinler: toplanma alanı kalabalığı.
- `C:\Users\Gokturk\Downloads\POLYGON_City_Characters_Unity_2021_3_v1_2_1.unitypackage` yerel paketi doğrulandı; içerik projede `Assets/POLYGONCityCharacters` altında zaten bulunduğu için yinelenen import yapılmadı.

Town paketinin kullanılmayan demo sahneleri ve çevre içeriği projeden çıkarıldı. Karakter bağımlılıklarına ek olarak Story 03 apartman koridoru ve Story 04 dış rotası için seçilen 30 çevre modeli `Assets/Story/Environment/SyntyTown` altında yaklaşık 1,4 MB'lık curated set olarak korundu.

#### B — Geniş çocuk çeşitliliği

[POLYGON Kids Pack](https://assetstore.unity.com/packages/3d/characters/humanoids/humans/polygon-kids-pack-art-by-synty-180629):

- Çok daha fazla çocuk ve aksesuar varyantı.
- Denetim tarihinde 149,99 USD.
- Yetişkin kadroyu tek başına çözmez.

Ana oyun için A seçeneği uygulandı. Kids Pack yalnız daha büyük okul/toplanma alanı kalabalığı gerekirse yeniden değerlendirilir.

#### C — Eski graybox yolu, emekli edildi

- Boy0 ve RG Poly gövdeler yalnız eski, değiştirilmemiş referans sahnelerinde kalır.
- Yeni rebuild sahneleri Synty kadrosunu kullanır.

### Import kalite kapısı

Uygulanan kalite kapısı:

1. Deniz ve Can ayrı Synty prefablarıyla dört rebuild sahnesine bağlandı.
2. Humanoid avatar geçerliliği ve ortak Animator Controller bağlantısı test edildi.
3. Idle/yürüme retarget'ı Play Mode 9:16 kamera görüntülerinde incelendi.
4. Ayak, kalça ve bacak açıklığı doğal yürüyüş pozu sırasında doğrulandı.
5. Anne, Baba, Nermin ve Paramedic aynı görsel dilde sahnelere bağlandı.
6. Synty entegrasyon testleri 6/6, tüm EditMode 92/92 ve PlayMode 21/21 geçti.

## 4. Animasyon lisansı ve kullanım

### Doğrulanmış CC0 kaynaklar

- `Assets/Story/Animations/ThirdParty/KayKit/LICENSE.txt`
  - CC0 1.0.
  - Kişisel, eğitim ve ticari kullanım açıkça belirtilmiş.
- `Assets/Story/Animations/ThirdParty/Quaternius/LICENSE.txt`
  - CC0 1.0.
- `Assets/Story/Characters/ThirdParty/RGPolyFamily/SOURCE_AND_LICENSE.md`
  - Kaynak ve CC0-1.0 bilgisi bulunuyor.

### Eksik lisans kanıtı

- Yerel `KidsCharacterFree` readme dosyasında açık lisans metni yoktur.
- Resmî Unity Asset Store sayfası Standard Unity Asset Store EULA gösterir.
- Ticari build öncesi mağaza kaydı/edinim kanıtı lisans manifestine eklenmelidir.
- Cute Furniture ve Pandazole paketlerinin kaynak/edinim kanıtı repo içinde açıkça görünmüyor; ticari yayın öncesi Unity hesabındaki edinim kaydı veya lisans belgesi arşivlenmelidir.

Lisans kanıtı bulunamayan asset “zaten projede var” gerekçesiyle yayın adayına otomatik kabul edilmez.

## 5. Ses açığı

### Mevcut yerel ses

| Dosya | Kullanım |
|---|---|
| `calm_home.wav` | Geçici ev ambiyansı |
| `quake_rumble.wav` | Geçici deprem düşük frekans yatağı |
| `quake_impact.wav` | Geçici darbe |
| `community-power-drill-90294.mp3` | Story 02 matkap |
| `correctSound.mp3`, `wrongSound.mp3` | Yeni hikâyeli akışta kullanılmayacak |

Mevcut üretilmiş sesler tek başına tam oyun için yetersizdi. Kaynak kaydıyla import edilen `100 CC0 SFX #2` paketi; genel 3B nesne, darbe ve ortam seslerinin büyük bölümünü kapattı. Story 01'de yeni eklenen fiziksel radyo ayarı ve Can'ın düdüğü için iki ayırt edici cue hâlâ eksiktir; şu anda genel etkileşim sesi kullanılır.

### Gerekli minimum ses grupları

| Grup | Cue sayısı | Örnek |
|---|---:|---|
| Ev ambiyansı | 3 | gündüz, akşam, elektrik kesintisi |
| Çanta ve kumaş | 6 | fermuar, kayış, kumaş, yere koyma |
| Küçük prop | 10 | pil, düğme, şişe, kâğıt, çekmece, kapak |
| Radyo | 5 | statik, tarama, kilitlenme, kısa anons, bozulma |
| Düdük | 3 | yakın kısa, koridor, uzak cevap |
| Ev güvenliği | 8 | kitap, saksı, raf gıcırtısı, toka, matkap, vida |
| Deprem | 12–16 | üç rumble katmanı, yapı gıcırtısı, kapı, cam, düşen hafif eşya, toz |
| Merdiven | 7 | farklı ayaklar, korkuluk, kapı, beton parça, yankı |
| Sokak | 8 | şehir yatağı, siren, araç alarmı, tabela, kalabalık, görevli |
| Karakter tepkileri | 12–20 | nefes, kısa çağrı, korku, rahatlama; tam dublaj değil |

İlk hedef yaklaşık **55–75 kısa cue** idi. İçe aktarılan CC0 paket 100 kısa cue içerir; kullanılan alt küme dört rebuild sahnesinde malzeme ve eylem türüne göre seçilir.

### Önerilen büyük ücretsiz kaynak

[Sonniss GDC 2026 Game Audio Bundle](https://gdc.sonniss.com/):

- 7,47 GB üzeri arşiv.
- Telifsiz ticari kullanım.
- Atıf zorunlu değil.
- [Lisans](https://sonniss.com/gdc-bundle-license/).

İndirme yöntemi:

1. Arşiv proje dışındaki geçici klasöre indirilir.
2. Tam paket `Assets` içine kopyalanmaz.
3. Manifestteki cue grupları için 40–60 uygun dosya seçilir.
4. Seçilen kaynak adı, üretici, arşiv adı ve lisans URL'si kaydedilir.
5. Yalnız seçilen WAV dosyaları `Assets/Story/Audio/ThirdParty/SonnissGDC2026` altına alınır.

### Küçük indirme alternatifi

Tek tek CC0 dosyalar seçilecekse:

- Her dosya için doğrudan kaynak sayfası saklanır.
- “Royalty free” ifadesi tek başına kabul edilmez; açık lisans aranır.
- CC-BY dosya kullanılırsa krediler otomatik lisans listesine eklenir.
- Lisansı belirsiz YouTube/MP3 dönüştürmeleri kullanılmaz.

Story 01 için seçilmeye hazır iki küçük kaynak:

| Kullanım | Kaynak | Lisans | Planlanan işlem |
|---|---|---|---|
| Radyo paraziti → frekans kilidi | [OpenGameArt — Static / ScatterNoise1.mp3](https://opengameart.org/content/static) | CC0 | Kısa parazit bölümü kırpılır; frekans düğmesi hareketi boyunca düşük seviyede çalınır ve net yayın anında kesilir |
| Can'ın kısa acil durum düdüğü | [Mixkit — Police short whistle](https://mixkit.co/free-sound-effects/whistle/) | Mixkit Free License | Tek 1 saniyelik üfleme kullanılır; yakın/koridor/uzak karşılıkları sahnede spatial blend ve seviye ile üretilir |

Bu iki dosya kullanıcı onayı olmadan indirilmez. Onaydan sonra kaynak sayfası, lisans bağlantısı, edinim tarihi ve yapılan kırpma/miks işlemi manifestte dosya bazında kaydedilir.

## 6. Ses import standardı

| Ses türü | Unity import |
|---|---|
| Kısa tek seferlik foley | Vorbis/ADPCM, Decompress On Load |
| Uzun ambiyans | Vorbis, Streaming |
| Deprem rumble | Vorbis, Streaming veya Compressed In Memory; cihaz profiliyle test |
| Karakter nefes/ünlem | Mono, Decompress On Load |
| 3D ortam kaynakları | Mono, spatial blend sahnede |
| UI | Mono/stereo kaynağa göre, 2D |

Tüm sesler:

- Normalizasyon için toplu olarak ezilmez.
- Aynı loudness hedefinde sahne içinde mikslenir.
- Deprem sırasında altyazıyı maskelemez.
- “Sarsıntıyı azalt” seçeneğinde kapanmaz; yalnız kamera hareketi azalır.

## 7. Proje klasör standardı

```text
Assets/Story/
├── Audio/
│   ├── Authored/
│   └── ThirdParty/
│       └── SonnissGDC2026/
├── Characters/
│   ├── Prefabs/
│   └── ThirdParty/
│       └── Synty/
│           ├── Town/
│           └── CityCharacters/
├── Environment/
│   ├── Home/
│   ├── Stairwell/
│   └── Exterior/
└── Licenses/
    ├── AssetManifest.md
    ├── Sonniss_GDC_2026_License.txt
    └── Synty_Proof_Of_License.md
```

Unity package, zip ve mağaza kurulum dosyaları repoya eklenmez. Yalnız seçilen runtime assetler ve lisans/provenance kaydı tutulur.

## 8. Asset kaydı şablonu

Her dış dosya için:

| Alan | Değer |
|---|---|
| Proje yolu | |
| Görünen asset adı | |
| Üretici | |
| Paket / arşiv | |
| Kaynak URL | |
| Lisans türü | |
| Lisans URL / dosya | |
| Edinim tarihi | |
| Değişiklik | kırpma, miks, retarget, materyal vb. |
| Kullanılan sahne/beat | |

## 9. Kullanıcıdan gereken izin

Karakter kararı tamamlandı: yerel Town + City Characters paketleri rebuild sahnelerine uygulandı. Genel foley/ortam kararı da tamamlandı: `100 CC0 SFX #2` import edildi ve sahnelere bağlandı.

Kalan tek asset kararı:

1. OpenGameArt CC0 radyo parazitini indirip Story 01 radyo ayarına bağla.
2. Mixkit kısa polis düdüğünü indirip Can'ın düdük etkileşimlerine bağla.

İndirme küçük ve ücretsizdir; yine de dış dosya edinimi kullanıcı onayı bekler.
