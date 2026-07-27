# Story 01 — Mevcut Sahne Dönüşüm Envanteri

## Amaç

Bu belge `Assets/Scenes/Story_01_BagPreparation.unity` sahnesini silmeden, masa sergisinden aynı aile evinde geçen oynanabilir hikâyeye dönüştürmek için mevcut parçaların hangilerinin korunacağını, taşınacağını ve değiştirileceğini belirler.

Kaynaklar:

- Sahne builder: `Assets/Editor/StoryPreparationSceneBuilder.cs`
- Runtime manager: `Assets/Scripts/Story/StoryPreparationDirector.cs`
- Kanonik ev tabanı: `Assets/Editor/StoryVerticalSliceBuilder.cs`
- Hedef beat'ler: `Docs/Story_01_Playable_Script.md`
- Tam oyun matrisi: `Docs/Full_Game_Beat_Production_Matrix.md`

## Doğrulanmış mevcut durum

- Aktif sahne `Story_01_BagPreparation`.
- Sahne kökü `STORY_01_BAG_PREPARATION`.
- Ev `Environment_PreparationHome/FamilyRoom` altında bağımsız üretiliyor.
- Oda yaklaşık 11×12 metrelik tek açık hacim.
- Dört kategori istasyonu var:
  - `SignalStation`
  - `FoodStation`
  - `HealthStation`
  - `WarmthLabel` ile temsil edilen sıcak kalma alanı
- 26 seçim nesnesi aynı hacimde sergileniyor.
- 13 nesne önerilen, 13 nesne yanlış/decoy.
- Eşya tanımındaki `gesture` alanları farklı olsa da `BuildDecisionItem` bütün nesnelere zorla `StoryInteractionGesture.DragToBag` veriyor.
- `DragToBag`, tekil `BagDropZone.Instance` hedefine bağlı.
- 30 adet görünür çanta slotu üretiliyor; bu sayı sahne kalabalığını artırıyor.
- On kamera bölgesi var ancak dört kategori kadrajı aynı “masa–eşya–çanta” yapısını tekrar ediyor.
- Ortam sesi yalnız `calm_home.wav`.
- Fener, radyo ve düdük aynı sahnede işlevsel araç olarak oynanmıyor.
- `StoryPreparationDirector` ilerlemeyi dört kategori ve zorunlu doğru eşya sayımı üzerinden yürütüyor.

## Korunacak parçalar

| Parça | Karar | Gerekçe |
|---|---|---|
| `StoryGameManager` ve yerel kayıt | Koru | Mevcut flag/checkpoint sözleşmesi çalışıyor |
| `StoryTouchManager` | Koru | Bütün dokunma girişinin tek sahibi olma şartını karşılıyor |
| `StoryPlayerMovement` ve NavMesh | Koru | Dokun–yürü omurgası mevcut |
| `StoryCameraController` | Koru, zone listesini yeniden bestele | Tek Cinemachine Brain düzeni doğru temel |
| `StoryUIController` | Koru, yalnız minimal hedef/altyazı/context kullan | Tema, safe area ve altyazı altyapısı mevcut |
| `StoryPreparationDirector` | Aynı manager içinde yeniden sırala | Yeni ayrı runtime manager açmaya gerek yok |
| Orijinal açık/kapalı/takılı çanta varyantları | Koru ve kanonik giriş rafına taşı | Devamlılık ve görünür doluluk için kullanılabilir |
| `Deniz_12`, `Can_8`, `Anne_Ayse` bağlantıları | Graybox boyunca koru | Yeni karakter kararı gelene kadar sahne üretimini durdurmaz |
| KayKit/Quaternius animasyon kütüphanesi | Koru | Yürüme, etkileşim, eğilme, taşıma için yeterli kaynak var |
| URP profil ve renk tokenları | Koru | Görsel dil diğer hikâye sahneleriyle uyumlu |
| `BagFlashlight`, `BagBatteries`, `BagRadio`, `BagWhistle`, `BagWater`, `BagFood`, `BagFirstAid`, `BagDocuments`, `BagBlanket`, `BagClothing`, `BagReady` | Koru | Sonraki perdelerde gerçek sonuç üretebilir |

## Kaldırılacak veya değiştirilecek parçalar

| Mevcut parça | İşlem | Yerine gelecek |
|---|---|---|
| Bağımsız `FamilyRoom` geometrisi | Builder'dan çıkar | Story 03 tabanlı `StoryHome_Shared` sağlam varyantı |
| `SignalStation` | Kaldır | Giriş dolabı/çekmece ve gerçek fener–pil çalışma yüzeyi |
| `FoodStation` | Kaldır | Mutfak alt dolabı ve tezgâh |
| `HealthStation` | Kaldır | Yardım dolabı ve fermuarlı belge dosyası |
| `WarmthLabel` ve kategori yazıları | Kaldır | Gardırop/çekmece, görsel yönlendirme |
| Büyük dünya etiketleri | Kaldır | Nesne yerleşimi, ışık, karakter bakışı ve çevresel işaret |
| 26 nesnenin aynı anda görünmesi | Dağıt ve azalt | 10–12 temel nesne + 4 bağlamsal riskli alternatif |
| 13 bağımsız decoy | Büyük ölçüde kaldır | 3–4 fiziksel sonucu olan bağlamsal yanlış seçim |
| Bütün eşyalarda `DragToBag` | Değiştir | Nesneye özgü açma, takma, ayarlama, taşıma, çekme |
| Dört kategori review hotspot'u | Kaldır | Aile karakterlerinin hareket içindeki kısa tepkileri |
| `selected/required` kategori hedefleri | Kaldır | Hikâye niyetini belirten ince hedef şeridi |
| 30 packed slot | 12 görünür slotla sınırla | Çantanın gerçek doluluk silueti; geri kalanı kapalı varyantta |
| `PreparationWrongChoice` genel kamera | Değiştir | Her riski ve güvenli alternatifi birlikte gösteren yerel `RISK` kamera |
| Final completion panelinin anında açılması | Geciktir | Karanlık tatbikat ve aile final kadrajından sonra perde geçişi |

## Kanonik ev eşlemesi

Story 03 builder'ındaki aşağıdaki koordinatlar taban kabul edilir:

| Referans | Story 03 durumu | Story 01 hedefi |
|---|---|---|
| Salon zemini | 10×11,5 m; merkez yaklaşık `(0, 0, 0.25)` | Aynen kullan |
| Güvenli masa | merkez yaklaşık `(0.45, 0, 0.65)` | Aile planı ve karanlık tatbikat referansı |
| Pencere | arka-sol, odak yaklaşık `(-2.2, 0.1, 4.75)` | Karanlık tatbikat ramak kala sınırı |
| Gardırop | arka-sol, yaklaşık `(-4.2, 0, 5.38)` | Story 01 kıyafet alanı; Story 02 sabitleme; Story 03 varyant |
| Raf | sağ taraf, yaklaşık `(4.58, 0, 1.55)` | Story 02 ağırlık düzenleme |
| Çıkış | arka-sağ, merkez `x=2.5`, `z≈6` | Story 01 çanta rafı ve tatbikat finali |
| Koridor | kapıdan `z≈12.8` yönüne uzanıyor | Story 01'de kapı ötesi kısa önizleme, Story 03/04 devamı |
| Aile koltuğu | ön-sol, yaklaşık `(-2.78, 0, -3.72)` | Sakin açılış |
| Alçak dolap | ön-sağ, yaklaşık `(3.72, 0, -3.92)` | Radyo ve aile fotoğrafı |
| Oyuncak araba | yaklaşık `(-1.15, 0.08, -2.7)` | Story 01 konfor eşyası; Story 02 rota testi; Story 03 yuvarlanma |

Story 01'e kanonik tabanı bozmadan üç kısa niş eklenir:

1. Ön-sağda mutfak alt dolabı.
2. Çıkışa yakın yardım/iletişim dolabı.
3. Koridor eşiğini kapatmayacak alçak çanta rafı ve aile planı panosu.

## Yeni Story 01 sahne hiyerarşisi

```text
STORY_01_BAG_PREPARATION
├── Environment_StoryHome
│   ├── SharedHome_Geometry
│   ├── Story01_IntactVariant
│   │   ├── KitchenNook
│   │   ├── AidCabinet
│   │   ├── EntryStorage
│   │   ├── FamilyPlanBoard
│   │   └── BagShelf
│   ├── Story01_Props
│   │   ├── FlashlightAssembly
│   │   ├── RadioTuning
│   │   ├── FoodWater
│   │   ├── AidDocuments
│   │   ├── Clothing
│   │   ├── ConsoleConflict
│   │   └── ComfortItem
│   └── Story01_DarkDrill
│       ├── DayLights
│       ├── DrillLights
│       ├── FlashlightCone
│       └── SafeRouteMarkers
├── Characters
│   ├── Deniz
│   ├── Can
│   └── Anne
├── Story01_Interactions
├── Story01_Timelines
├── Story01_Cameras
├── Story01_Audio
├── Story01_VFX
├── Story01_UI
└── _StoryPreparationCore
_StorySession
```

## Etkileşim dönüşüm tablosu

| Hedef beat | Dünya nesnesi | Fiziksel fiil | Mevcut sistemle prototip | Sonuç |
|---|---|---|---|---|
| Aile planı | Toplanma simgesi | Panodaki yuvaya taşı | `SwipeHorizontal` + Timeline | Simge panoda görünür, harita ışığı yanar |
| Aile planı | Melek Teyze kartı | Şeffaf cebe yerleştir | `SwipeDown` + Timeline | Kart cepte görünür |
| Işık | Çekmece | Tutup dışarı çek | `SwipeDown` veya `SwipeHorizontal` + Animator | İçerik görünür |
| Işık | Pil | Fener gövdesine taşı | İlk prototip: pil üzerinde yönlü swipe + Timeline | Pil kaybolmaz; fener içinde görünür |
| Işık | Fener anahtarı | Doğrudan dokun | `Tap` | Gerçek spot/konik ışık açılır |
| Radyo | Frekans düğmesi | Yatay ayarla | `SwipeHorizontal` | Parazit temiz yayına crossfade olur |
| Mutfak | Alt dolap kapağı | Tutup aç | `SwipeHorizontal` + Animator | Su/gıda görünür |
| Çanta | Temel eşyalar | Gerçek çanta ağzına taşı | Mevcut `DragToBag` | Çantada görünür slot |
| Konsol | Ağır çanta | Dizleri büküp kaldırmayı dene | `WorldHold` veya `RepeatedTap` + Timeline | Deniz sendeleyip çantayı güvenli bırakır |
| Konsol | Konsol | Çantadan dışarı çıkar | `SwipeDown` + Timeline | Konsol Can'ın masasına döner |
| Konfor | Oyuncak/fotoğraf | Dış cebe yerleştir | `DragToBag`, farklı görünür slot | Sonraki sahnelerde aynı prefab |
| Çanta askısı | İki toka | Doğrudan çek | İki `SwipeDown` | Askılar eşitlenir |
| Tatbikat | Can | Yanına git ve bağ kur | `Approach` + `WorldHold` | Can takip durumuna geçer |
| Tatbikat | Fener | Aç ve rotayı tara | `Tap` + kamera/ışık Timeline | Güvenli yüzeyler görünür |
| Final | Giriş rafı | Çantayı alçak rafa bırak | `Approach` + Timeline | `BagReady` ve final kadrajı |

Kalite testinde pilin veya kartın parmak altında gerçek hedefe taşınması şart kalırsa, yalnız bu genel ihtiyaç için tekrar kullanılabilir dünya-soket sürükleme çözümü değerlendirilir. Her nesne için ayrı runtime script yazılmaz.

## Kamera dönüşümü

Mevcut kategori kameraları kaldırılır. Hedef kamera listesi:

| Kamera | Kadraj hedefi | Kritik görünür öğeler |
|---|---|---|
| `Story01_HomeWide` | Salon dolaşma | Deniz alt üçlüde; masa, pencere, çıkış |
| `Story01_FamilyPair` | Deniz–Can/Anne konuşması | İki karakterin yüz yönü ve beden dili |
| `Story01_PlanBoard` | Pano etkileşimi | Elde taşınan kart ve hedef yuva |
| `Story01_EntryStorage` | Fener/pil/düdük | Çekmece içi ve çanta aynı planda |
| `Story01_Radio` | Radyo ayarı | El, düğme, frekans göstergesi |
| `Story01_Kitchen` | Su/gıda | Alt dolap, nesne, çantaya giden yol |
| `Story01_AidCabinet` | İlk yardım/dosya | Çocuk, dolap ve hedef dosya |
| `Story01_ConsoleConflict` | Ağır çanta ve Can | Deniz'in sendelemesi ve Can'ın tepkisi |
| `Story01_DarkRoute` | Karanlık tatbikat | Fener konisi, Can ve hedef çıkış |
| `Story01_FinalWide` | Perde finali | Masa, pencere, gardırop, çıkış ve çanta |

Her etkileşim kadrajında masa ayağına veya duvara aşırı yaklaşan kamera yasaktır. Kamera hedefi nesnenin pivotu değil, eylemin okunacağı bestelenmiş focus transformudur.

## Timeline ve Animator teslimleri

Yeni runtime davranışı yerine sahnede üretilecek sekanslar:

1. `S01_Plan_MapMagnet`
2. `S01_Plan_ContactCard`
3. `S01_Drawer_Open`
4. `S01_Flashlight_BatteryInsert`
5. `S01_Flashlight_PowerOn`
6. `S01_Radio_Tune`
7. `S01_KitchenCabinet_Open`
8. `S01_GlassBottle_NearMiss`
9. `S01_AidFile_Zip`
10. `S01_Console_HeavyBag`
11. `S01_Console_Return`
12. `S01_ComfortItem_Pocket`
13. `S01_Blanket_Roll`
14. `S01_BagStraps_Adjust`
15. `S01_Drill_LightsOut`
16. `S01_Drill_FlashlightRoute`
17. `S01_Drill_WindowNearMiss`
18. `S01_Bag_Close`
19. `S01_Bag_ToShelf`
20. `S01_Final_FamilyWide`

Gerekli karakter klipleri mevcut KayKit/Quaternius kütüphanesinden retarget edilir:

- doğal idle varyasyonları
- eğilme ve yerden alma
- iki elle kaldırma
- hafif sendeleme
- işaret etme
- çocuğa göz hizasında çömelme
- kısa çağırma
- yürüyüş ve yavaş takip

## Ses teslimleri

Mevcut yerel sesler Story 01 için yeterli değildir. Gereken minimum tekil dosyalar:

- iç mekân gündüz room tone
- uzak şehir/trafik yatağı
- çekmece açma/kapatma
- dolap kapağı
- fermuar açma/kapatma
- çanta kumaşı ve kayış gerilmesi
- pil yerleştirme klikleri
- el feneri anahtarı
- radyo statik, tarama ve temiz kısa yayın
- düdük yakın ve koridor varyantı
- plastik şişe
- cam şişe tıkırtısı/ramak kala
- kâğıt, kart, kalem
- battaniye/kumaş
- ayak sesleri: ahşap, halı, koridor
- elektrik anahtarı ve hafif karanlık oda tonu

Müzik yerine ilk kalite kapısında çevresel ses ve iki kısa motif yeterlidir:

- sakin aile motifi
- Can–Deniz ilişki motifi

## Builder değişiklik sırası

1. `StoryPreparationSceneBuilder` içinde mevcut builder'ın yedeğini koru.
2. Story 03 ev geometrisini ortak prefab üretim kaynağına çıkar.
3. Story 01 builder'ı bu prefabı sağlam varyant olarak instantiate etsin.
4. Dört istasyon ve büyük dünya etiketlerini builder'dan kaldır.
5. Mutfak, yardım dolabı, giriş saklama ve aile panosu nişlerini ekle.
6. 26 seçim nesnesini hedef 10–12 temel nesne ve 4 bağlamsal riskli alternatifle değiştir.
7. Mevcut director'ı beat sırasına göre aynı manager içinde yeniden bağla.
8. On bestelenmiş kamerayı kur ve 9:16/9:19.5 ekran görüntüsüyle kilitle.
9. Yirmi Timeline/Animator sekansını ekle.
10. Sesler onaylandıktan sonra AudioSource'ları sahneye yerleştir.
11. NavMesh bake, checkpoint devamı ve bütün flag kombinasyonlarını test et.
12. Kronometreli ilk oynanış 8:30 altında kalırsa yalnız içerik ve serbest gözlem ekle; bekleme sayacı ekleme.

## Story 01 tamamlanma kanıtı

Story 01'in “dolu” sayılması için aşağıdaki kanıtların tamamı gerekir:

- Unity Play Mode'da baştan sona kesintisiz video veya kronometre kaydı.
- İlk oynayan kullanıcı için 8–10 dakika gerçek eylem/hikâye.
- Ortada öğretici eylem butonu bulunmadığını gösteren UI görüntüsü.
- En az altı farklı dünya fiilinin çalıştığını gösteren etkileşim kaydı.
- Fener, radyo ve düdüğün sahnede çalışan sonuçları.
- Konsol çatışması ve karanlık tatbikatın oynanabilir hâli.
- Story 03 ile aynı ev referanslarının karşılaştırmalı görüntüsü.
- Checkpoint'ten devam ve bütün temel flag'lerin JSON dönüşü.
- Console'da exception olmaması ve frame başına sürekli GC allocation bulunmaması.
