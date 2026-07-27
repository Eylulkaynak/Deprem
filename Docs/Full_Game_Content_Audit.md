# Deprem — Mevcut Tam Oyun İçerik Denetimi

## Denetimin kapsamı

Bu belge mevcut dört hikâye sahnesinin runtime director, sahne builder, checkpoint, kamera ve asset durumuna dayanır. Süreler henüz kronometreli kör kullanıcı testinden değil; yazılmış diyalog süreleri, etkileşim süreleri, zorunlu gecikmeler ve rota uzunluklarından türetilmiş üretim tahminidir.

## 1. Mevcut süre ve içerik özeti

| Perde | Mevcut yapı | Anlamlı oynanış tahmini | Temel sorun |
|---|---|---:|---|
| Story 01 — Hazırlık | 26 eşya, 13 doğru/13 yanlış, çoğunlukla `DragToBag` | 3–4 dk | Aynı sürükle–bırak fiili ve masa teşhiri |
| Story 02 — Evi güvenli yap | 16 dünya etkileşimi, 6 kamera | 4,5–6 dk | Ayrı ve tutarsız ev; eylem sonrası uzun açıklama |
| Story 03 — Deprem | Yaklaşık 30 beat; 8 dakika minimum kod kapısı | 4,5–6 dk gerçek içerik + zorunlu bekleme | Süre olayla değil sayaçla uzatılıyor |
| Story 04 — Tahliye | 19 tanımlı etkileşim, 13 kamera, yaklaşık 46 m rota | 5,5–7,5 dk | Komşu ve aile finali hazırlıksız; dış dünya boş |

Mevcut oyunun anlamlı eylem ve hikâye içeriği yaklaşık **18–24 dakika** aralığındadır. Story 03'ün minimum sekiz dakika görünmesi gerçek içerikten değil, zorunlu süre alanlarından kaynaklanır.

## 2. Story 01 bulguları

- 26 nesnenin tamamı tek büyük odada dört masa üzerine dizilmiştir.
- 13 temel nesne ve 13 decoy vardır.
- Bütün eşya etkileşimleri `DragToBag` kullanır.
- Eşyalar uzaktan seçilebilir; mekânda arama ve taşıma gerekmez.
- On kamera olmasına rağmen kategori kameraları aynı masa–çanta kompozisyonunu tekrarlar.
- Çalışan fener, radyo veya düdük sonucu yoktur; nesneler yalnız flag'e dönüşür.
- Can'ın oyun konsolu mevcut fakat dramatik bir çatışma yerine sıradan yanlış seçenek olarak kullanılır.

Sonuç: seçim sayısı içerik miktarı izlenimi verir fakat oynanış ve hikâye çeşitliliği üretmez.

## 3. Story 02 bulguları

Mevcut zorunlu eylemler:

- Dolap, raf ve çıkışı inceleme: 3.
- Çıkıştaki ayakkabı, oyuncak ve paketi taşıma: 3.
- Raftaki kitap, saksı ve çerçeveyi indirme: 3.
- Raf bağlantısını yetişkine verme: 1.
- Dolabı test etme, iki noktayı işaretleme, kayışı yetişkine verme: 3.
- Kapı rotasını test etme: 1.

İki isteğe bağlı ramak kala vardır:

- Ağır kutuyu kaldırmaya çalışma.
- Matkabı almaya çalışma.

Güçlü taraflar:

- Çocuk ağır sabitleme veya matkap işi yapmaz.
- Hafif nesne taşıma ile yetişkin işi ayrılmıştır.
- `ExitCleared`, `ShelfSecured` ve `WardrobeSecured` sonraki perdeye taşınır.
- Dünya etkileşimleri kaydırma, basılı tutma ve tekrar dokunma içerir.

Sorunlar:

- Story 02, Story 03'teki evle aynı ev değildir.
- Story 02 odası 12×12 m, kapısı ön duvarda `z=-5.72` konumundadır.
- Story 03 salonu 10×11.5 m, koridor kapısı arka duvarda `z=6` konumundadır.
- Koltuk, pencere, gardırop ve raf tamamen farklı koordinatlardadır.
- Oyuncu “aynı evi hazırladım ve sonra depremde sonucunu gördüm” duygusunu yaşayamaz.
- Her küçük hareketten sonra 4,8–8 saniyelik açıklama gelir; karakterler olayı oynamak yerine sonucu anlatır.
- Nermin komşu bu perdede tanıtılmaz.
- Aynı hareket üç çıkış eşyasında ve üç raf eşyasında seri olarak tekrarlanır.

## 4. Story 03 bulguları

Kodda bulunan zorunlu tempo alanları:

- Sakin açılış minimumu: 60 saniye.
- Sakin açılış maksimumu: 90 saniye.
- Aktif sarsıntı minimumu: 90 saniye.
- Deprem sonrası bekleme: 10 saniye.
- Koridor uyarısı: minimum 30 saniye.
- Sahne minimum tamamlama süresi: 480 saniye.
- Tahmini traversal ilavesi: 115 saniye.

Bu alanlar `StorySequenceDirector` içinde yazılmıştır. Özellikle final koridor döngüsü:

- `corridorWarningDuration` bitene kadar,
- ve toplam süre `minimumCompletionDuration` değerine ulaşana kadar

aynı dört altyazıyı döndürür.

Mevcut içerik:

- Sakin açılışta masa, pencere, dolap ve çıkışın sırayla incelenmesi: 4.
- Depremde Can'a seslenme, masaya geçme, başı koruma, masa ayağına tutunma.
- Deprem sonrasında 15 ayrı checklist beat'i.
- Fener veya acil ışık seçimi.
- Koridora geçiş ve dört koridor beat'i.

Sorunlar:

- Story 01 ve Story 02 zaten oynandıktan sonra aynı masa, pencere, dolap ve çıkış tekrar ders gibi incelenir.
- Deprem aniden başlasa bile öncesindeki dört zorunlu inceleme, oyuncuya olayın yaklaştığını hissettirir.
- 90 saniyelik sarsıntının büyük bölümü masa altında altyazı bekleyerek geçer.
- Deprem sonrası Can kontrolü üç ayrı adıma; ayakkabılar dört adıma; çanta dört adıma bölünmüştür.
- Etkileşim sayısı yüksek görünür fakat dramatik karar sayısı düşüktür.
- Sekiz dakikalık kabul kapısı, içerik azlığını gizler.

Korunacak güçlü taraflar:

- Aktif sarsıntıda uzun mesafe hareket sınırlanır.
- Pencere ve kapı ramak kalaları yaralanma göstermeden checkpoint'e döner.
- Dolap, raf, çıkış ve fener hazırlık flag'lerine göre fiziksel varyant üretir.
- Kamera sarsıntısı azaltılabilirken çevre animasyonu ve ses korunur.

## 5. Story 04 bulguları

Mevcut ana akış:

1. Koridoru kontrol et.
2. Merdiven veya asansörü incele.
3. Üst sahanlığa in.
4. Artçıda korkuluğa tutun.
5. Alt sahanlığa in.
6. Nermin'e seslen.
7. Bastonu ve hafif parçayı taşı.
8. Nermin'e denge desteği ver.
9. Bina kapısından çık.
10. Sokak tehlikesini incele.
11. Açık kaldırımdan ilerle.
12. Toplanma levhası, Can, Nermin ve aile sinyalini kontrol et.

Güçlü taraflar:

- Asansör güvenli seçenek olarak onaylanmaz.
- Artçıda hareket durur.
- Komşunun kolu çekilmez; bastonunu kavraması beklenir.
- Ağır dolap veya yapısal parçaya çocuk müdahale etmez.
- Fener ve düdük flag'leri alternatif sonuç üretir.

Sorunlar:

- Nermin ilk kez merdivende görünür; oyuncunun onunla önceden ilişkisi yoktur.
- Nermin için Anne Ayşe prefabı tekrar kullanılır; görsel olarak aynı karakterin kopyasıdır.
- Bina dışı ve sokak büyük ölçüde primitive zemin/duvar ve dünya yazılarından oluşur.
- “AÇIK ROTA” gibi zemine yazılan metinler dünya yerine öğretici UI hissi verir.
- Asansör oyuncuya eşdeğer bir tercih gibi sunulur; doğru davranış çevre okumayla anlaşılmak yerine yanlış düğmeye basılarak öğretilir.
- Aile birleşmesi oynanmaz. Anne yalnız son altyazıda gelir ve hemen completion panel açılır.
- Toplanma alanındaki dört kontrol yeni bir dramatik final değil, yeni bir checklist'tir.

## 6. Bütün oyundaki yapısal sorunlar

### Aynı ev hissi yok

Story 01, Story 02 ve Story 03 farklı ölçü ve mobilya düzenine sahip üç ayrı salon kullanır.

### Hikâye yerine görev sırası

Eylemlerin çoğu karakter çatışması veya değişim üretmez. Oyuncu doğru nesneyi seçer, altyazı sonucu açıklar ve sıradaki hedef açılır.

### Fazla açıklama

Karakterlerin kısa doğal tepkileri yerine 5–8 saniyelik öğretici paragraflar kullanılır. Eylem ve ses geri bildirimi altyazının anlattığı sonucu zaten göstermelidir.

### Yapay süre

Story 03'te minimum sekiz dakika kodla zorlanır. Bu alanlar oyuncunun ustalaşmasını cezalandırır ve tekrar oynanabilirliği düşürür.

### Karakter görsel kalitesi

- Deniz ve Can aynı `KidsCharacterFree/Boy0` modelinin boy/renk varyantıdır.
- Farklı yaş, beden dili ve siluet yeterince okunmaz.
- Nermin ile Anne aynı yetişkin prefabını kullanır.

### Ses katmanı yok denecek kadar az

Projede hikâye için kullanılan temel havuz:

- `calm_home.wav`
- `quake_rumble.wav`
- `quake_impact.wav`
- Eski doğru/yanlış UI sesleri
- Projeyle ilgisiz birkaç ortam/matkap sesi

Dolap, kumaş, fermuar, radyo, düdük, adım, cam, merdiven, dış ortam, kalabalık ve karakter tepki katmanları eksiktir.

### Final yok

Tahliye teknik olarak biter ancak Deniz'in karakter yayı, Can'ın güven duygusu, ebeveynlerle birleşme ve oyuncunun hazırlık kararları sinematik bir final oluşturmaz.

## 7. Korunacak teknik temel

- `StoryTouchManager` giriş sahipliğinin tek merkezidir.
- `StoryInteractable` doğrudan dünya hareketlerini ve UnityEvent bağlarını destekler.
- `StoryGameManager` flag, checkpoint ve yerel JSON state temelini sağlar.
- NavMesh tabanlı dokun-yürü vardır.
- Cinemachine kamera zone sistemi vardır.
- Hazırlanmış/hazırlanmamış sahne varyantları vardır.
- Güvenli ramak kala ve yakın checkpoint dönüşü vardır.
- UI sırasında dünya girişi ve devam eden rota kilitlenebilir.

Yeniden yapım bu temeli çöpe atmayacaktır. İçerik, ortak çevre, Timeline, ses ve karakter sunumu güçlendirilecektir.

## 8. Denetim sonucu

Mevcut proje teknik bir eğitim prototipi olarak çalışır. Etkileyici bir hikâyeli mobil macera olması için yalnız daha fazla eşya veya daha uzun altyazı eklemek yeterli değildir.

Gerekli dönüşüm:

1. Üç iç mekân sahnesini tek kanonik eve bağlamak.
2. Dört perde boyunca devam eden karakter ilişkileri kurmak.
3. Her perdede farklı bir ana oynanış fiili ve set-piece üretmek.
4. Hazırlık flag'lerini dramatik ve fiziksel sonuçlara çevirmek.
5. Yapay süre kapılarını kaldırmak.
6. Karakter ve ses kalitesini ayrı üretim kapısı yapmak.
7. Aile birleşmesini gerçek oynanabilir/sinematik final hâline getirmek.
