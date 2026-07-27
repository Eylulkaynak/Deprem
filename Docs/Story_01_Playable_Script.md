# Story 01 — Oynanabilir Senaryo

## Sahnenin amacı

Bu perde oyuncuya bir afet çantası listesi ezberletmez. Deniz, Can ve annesinin birbirlerine nasıl güvendiklerini kurar; Story 03'te kullanılacak ev coğrafyasını tanıtır; hazırlanan araçların aynı sahnede gerçekten işe yaradığını gösterir.

Dramatik soru:

> “Her şeyi yanımıza alamıyorsak, gerçekten önemli olanı nasıl seçeriz?”

Deniz'in perde başındaki tutumu işi hızlıca bitirmektir. Can'ın ihtiyacı sevdiği şeyleri kaybetmeyeceğini hissetmektir. Perdenin sonunda ikisi de hazırlığın bir eşya listesi değil, birbirlerine ulaşabilme planı olduğunu anlar.

## Karakter tonu

### Deniz, 12

- Sorumluluk almaya yatkın ama bazen sabırsız.
- Can'a tepeden konuştuğunu fark ettiğinde kendini düzeltir.
- Bilgiyi bilen çocuk değil; deneyerek öğrenen oyuncu temsilidir.

### Can, 8

- Komik veya beceriksiz yardımcı değildir.
- Konsolu ve küçük oyuncağı onun güven duygusunu temsil eder.
- Düdük ve aile planında gerçek bir sorumluluk üstlenir.

### Anne Ayşe

- Ders anlatan sunucu gibi konuşmaz.
- Önce çocukların ne düşündüğünü sorar, sonra fiziksel sonucu gösterir.
- Tehlike veya yanlış seçimlerde çocukları utandırmaz.

## 0. Sakin açılış — 0:00–0:50

### Görüntü

Tek planla salon görülür. Can güvenli masada oyun konsoluyla oynar. Deniz pencereye yakın olmayan koltuk tarafında not defterine bir şey çizer. Mutfaktan tabak sesi ve sakin ev ortamı gelir. Anne girişteki alçak rafın altından boş afet çantasını çıkarır.

Kamera yakın çanta planına kesmez; önce aile ve ev coğrafyası kurulur.

### Oynanış

Oyuncu Deniz'i ilk kez serbestçe yürütür. Dünya vurgusu anneye veya açık çantaya gider. Ekranın ortasında genel eylem düğmesi yoktur.

### Diyalog

**Anne:** Çantadaki suyun tarihini yenileyecektik. Hazır başlamışken aile planını da bir kontrol edelim mi?

**Deniz:** Geçen sefer yapmıştık.

**Anne:** Eşyaların tarihi geçer. Siz büyürsünüz. Plan da evle birlikte değişir.

**Can:** Deprem bugün mü olacak?

Anne diz çöküp Can'ın göz hizasına gelir.

**Anne:** Ne zaman olacağını bilmiyoruz. Hazırlık, olacak diye korkmak değil; olursa birbirimizi nasıl bulacağımızı bilmek.

Bu konuşma sırasında hareket kilitlidir; altyazı bittiğinde dünya girişi açılır.

## 1. Aile planı panosu — 0:50–1:45

### Kurulum

Koridordaki fiziksel panoda mahalle haritası, şehir dışı iletişim kartı ve iki görev mıknatısı bulunur. Bunlar UI kartı değildir.

### Eylem 1 — Toplanma alanı

Oyuncu park mıknatısını haritadaki toplanma alanına doğru sürükler.

**Can:** Okul bahçesi daha yakın.

**Deniz:** Ama bizim planda park var. Bina ve duvarlardan daha açık.

**Anne:** Ayrı kalırsak herkes aynı yeri bilirse birbirimizi ararken dağılmayız.

### Eylem 2 — Şehir dışı iletişim kişisi

Oyuncu “Melek Teyze” kartını telefon ağacındaki yuvaya takar.

**Anne:** Mahallede hatlar yoğun olursa hepimiz şehir dışındaki aynı kişiye haber bırakırız.

**Can:** Ben numarayı ezberleyemem.

**Deniz:** O yüzden kartın bir kopyası çantada, biri de sende olacak.

### Eylem 3 — Kardeş rolleri

Oyuncu Deniz simgesini çanta işaretine, Can simgesini düdük işaretine bağlar.

**Deniz:** Sarsıntı durmadan çantaya koşmak yok.

**Can:** Önce yanında kalacağım. Sonra düdüğüm bende.

Anne başıyla onaylar. `PreparationStart` checkpoint'i burada yazılır.

## 2. Giriş dolabı: ışık ve haberleşme — 1:45–2:35

### Eylem dizisi

1. Oyuncu giriş dolabının kulbunu yana çekerek kapağı açar.
2. Fener doğrudan raftan seçilir.
3. Pil kutusunun kapağı iki dokunuşla açılır.
4. Mum seçilirse yakılmadan elden kayıp metal tepsiye düşer; kısa bir ramak kala sesi çıkar.

### Kısa tepkiler

**Can:** Mum daha uzun yanar.

**Anne:** Sarsıntıdan sonra gaz kokusu veya devrilmiş eşya olabilir. Açık alev kullanmıyoruz.

**Deniz:** Fener ve ayrı saklanan pil.

Bu aşamada araçlar toplanır ancak `BagFlashlight` henüz yazılmaz. Fenerin çalışması daha sonra test edilecektir.

## 3. Mutfak: su ve dayanıklı gıda — 2:35–3:20

### Eylem dizisi

1. Alt mutfak dolabı doğrudan kulbundan açılır.
2. Oyuncu iki su şişesinden birini çevirip tarih etiketini görür.
3. Tarihi uygun, sızdırmaz şişe seçilir.
4. Ağır cam şişe seçilirse çantaya gitmez; tezgâha bırakıldığında tok bir ağırlık sesi çıkar.
5. Kolay açılan dayanıklı gıda alınır.

### Diyalog

**Deniz:** Bu daha büyük. Daha çok su alır.

**Anne:** Cam olduğu için kırılabilir ve ağır. En büyük olan değil, güvenle taşıyabildiğimiz doğru seçim.

**Can:** Tarihi geçen su bozulur mu?

**Anne:** Çantadaki suyu ve yiyeceği düzenli yenilersek bunu afet günü düşünmek zorunda kalmayız.

`FoodPacked` checkpoint'i, su ve gıda sahnede kontrol edildikten sonra yazılır.

Uygulama notu: tarih kontrolü artık eşya seçiminden önce zorunlu bir nesne eylemidir. Oyuncu şişenin kendi ince `SKT` bandını yana çevirir; fiziksel onay işareti görülmeden su ve gıda sürüklemeleri açılmaz.

## 4. Yardım dolabı: ilk yardım ve hijyen — 3:20–4:05

### Eylem dizisi

1. Alçak yardım dolabının kapağı açılır.
2. İlk yardım çantasının fermuarı kısa bir kaydırmayla açılır.
3. Oyuncu kapalı sargı paketini çevirip mührünü kontrol eder.
4. Sabun ve ıslak mendil küçük sızdırmaz poşete yerleştirilir.
5. İlaç kutusuna dokunulursa Anne onu kendisi alır; çocuk ilacı seçmez veya kullanmaz.

### Diyalog

**Can:** Bu ilaç da gelsin mi?

**Anne:** İlaçları yetişkinler, kullanan kişiye göre kontrol eder. Siz kapalı sargıyı ve hijyen poşetini hazırlayabilirsiniz.

**Deniz:** Yani yardım etmek, her şeye tek başına karar vermek değil.

**Anne:** Aynen öyle.

`HealthPacked` checkpoint'i yazılır.

Uygulama notu: sargı mührü ayrı bir ekran düğmesi değildir. Paket sahnede basılı tutularak incelenir; mercan renkli mühür şeridi, koyu fiziksel onay işaretli kapalı şeride dönüşmeden ilk yardım ve belge eşyaları açılmaz.

## 5. Gardırop: sıcak kalma ve belge — 4:05–4:50

### Eylem dizisi

1. Gardırop kapağı kulbundan yana çekilir.
2. Can'ın eski tişörtü açılır; Can göğsüne tutunca küçük kaldığı fiziksel olarak görülür.
3. Mevsime uygun güncel kıyafet seçilir.
4. Hafif battaniye iki kaydırmayla katlanır.
5. Belge kopyası ve iletişim kartı su geçirmez dosyaya konur.

### Diyalog

**Can:** Bu benim en sevdiğim tişörttü.

**Deniz:** “İdi” kısmı doğru. Kolun yarısında kalıyor.

Can güler.

**Anne:** İşte bu yüzden çantayı yılda en az iki kez birlikte kontrol ediyoruz.

`WarmthPacked` checkpoint'i yazılır.

## 6. Can'ın konsolu — 4:50–5:55

### Sahne

Deniz son poşeti çantaya getirirken Can konsola bakar. Kamera bunu Deniz'in görmediği kısa bir arka plan hareketi olarak gösterir. Can konsolu çantaya yerleştirir ve fermuarı yarıya kadar çeker.

### Eylem

Oyuncu çantanın iki askısını tutarak kaldırmaya çalışır. Ağır çanta varyantı devreye girer:

- Çantanın tabanı aşağı sarkar.
- Askı gerilir.
- Deniz bir adım öne düşer ama yaralanmaz.
- İçeriden konsolun sert köşesi görünür.

### Diyalog

**Deniz:** Can! Bunu niye koydun?

Can konsolu iki eliyle tutar.

**Can:** Evden çıkarsak burada kalacak.

Deniz cevap vermeden önce kısa bir duraklama olur.

**Deniz:** Ben de bazı şeyleri bırakmak istemem.

**Anne:** Çanta yalnızca hayatta kalma araçlarından ibaret olmak zorunda değil. Ama taşıyamadığımız çanta kimseye yardım etmez.

### Oyuncu kararı

Oyuncu konsolu doğrudan çantadan çıkarıp masaya geri götürür. Sonra iki küçük konfor nesnesinden birini seçer:

- Küçük aile fotoğrafı.
- Can'ın küçük oyuncak arabası.

Seçilmeyen nesne reddedilmiş gibi gösterilmez; yerinde kalır.

**Can:** Bunu taşırım. Konsol eve göz kulak olsun.

**Deniz:** Döndüğümüzde ilk sen açarsın.

Çanta tekrar kaldırılır ve bu kez Deniz dengede kalır. `BagWeightBalanced` kullanılacaksa burada yazılır.

## 7. Araçların gerçekten çalışması — 5:55–7:05

### Fener

1. Pil, fenerin fiziksel pil yuvasına yönlendirilir.
2. Kapak kapatılır.
3. Anahtar kaydırılır.
4. Duvardaki aile planı aydınlanır.

**Deniz:** Yanıyor.

**Anne:** Çantaya girmeden önce çalıştığını bilmek, içinde olduğunu bilmekten daha önemli.

`BagFlashlight` ve `BagBatteries` burada yazılır.

### Radyo

1. Oyuncu güç düğmesine dokunur.
2. Frekans düğmesini yatay çevirir.
3. Parazit, kısa ve anlaşılır resmi yayın sesine dönüşür.

Radyo sesi uzun bilgi anlatmaz:

> “Acil durumlarda doğrulanmamış bilgileri paylaşmayın; resmî duyuruları takip edin.”

`BagRadio` burada yazılır.

### Düdük

1. Oyuncu düdüğü Can'a uzatır.
2. Can düdüğü dış cebine klipsler.
3. Oyuncu düdük üstünde kısa süre basılı tutar.
4. Can tek bir kısa deneme sesi çıkarır.

**Anne:** Üç kısa ses yardım çağrısı olarak daha kolay fark edilir. Evde yalnız deneme için bir kez.

**Can:** Bir kez.

`BagWhistle` burada yazılır.

`CommunicationPacked` checkpoint'i bütün işlev testleri tamamlandığında yazılır.

## 8. Kontrollü karanlık tatbikatı — 7:05–8:20

### Hazırlık

**Anne:** Şimdi kısa bir ışık kesintisi tatbikatı. Deprem yok. Koşmak yok. Can, işaretli bekleme noktasına; Deniz, olduğun yerde kal.

Can salonun koridor eşiğindeki görünür güvenli noktaya yürür. Anne ışığı kapatır. Acil ortam ışığı tamamen kaybolmaz.

### Eylem dizisi

1. Oyuncu fenerin anahtarını doğrudan kaydırır.
2. İlk ışık odağı mutfak zeminini gösterir; burada Can yoktur.
3. İkinci odak aile planını gösterir; oyuncu rotayı yeniden hatırlar.
4. Can bir kısa düdük sesi verir.
5. Oyuncu ışığı koridor eşiğine yönlendirir.
6. Deniz Can'ın yanına yürür.
7. Oyuncu Can'ın eline dokunup birlikte yürüme sekansını başlatır.
8. İkisi alçak çanta rafına döner.

### Diyalog

**Deniz:** Can?

Kısa düdük sesi.

**Can:** Buradayım.

**Deniz:** Işığı gördün mü?

**Can:** Evet. Sen de beni duydun.

Anne ışığı açar.

**Anne:** Hazırlık tam olarak bu. Karanlıkta bile birbirimize ulaşacak bir yol bırakmak.

Tatbikat sırasında yanlış ışık odağı ceza değildir. Yalnız kısa ortam ayrıntısı gösterir ve oyuncuyu tekrar denemeye bırakır.

## 9. Çantayı kapatma ve yerine koyma — 8:20–9:20

### Eylem dizisi

1. Seçilen fotoğraf veya küçük oyuncak dış cebe yerleştirilir.
2. Oyuncu fermuarı çantanın gerçek hattı boyunca çeker.
3. Deniz dizlerini büker ve çantayı kaldırır.
4. Sol ve sağ askı ayrı kaydırmalarla ayarlanır.
5. Oyuncu çantayı kapı geçişini kapatmayan alçak rafa yerleştirir.
6. Can'ın düdüğü dış cepte görünür kalır.

### Final

**Can:** Deprem olursa hemen çantaya koşuyoruz.

Deniz başını sallar.

**Deniz:** Hayır. Önce Çök–Kapan–Tutun. Sarsıntı durunca birbirimizi kontrol ediyoruz. Sonra ayakkabılar ve çanta.

**Anne:** Peki ayrı kalırsanız?

Can aile planı panosuna bakar.

**Can:** Parkta buluşuruz. Ulaşamazsak Melek Teyze'ye haber bırakırız.

Anne çantanın üstündeki küçük hatırayı düzeltir.

**Anne:** O zaman çanta değil, plan hazır.

Kamera aileden yavaşça açılır. Güvenli masa, pencere, gardırop, giriş ve çanta aynı planda görünür. Story 03'ün mekânsal hafızası burada tamamlanır.

`BagReady` yazılır ve `PreparationComplete` checkpoint'i alınır.

## Hata ve ramak kala dili

- Yanlış eşya puan düşürmez.
- Karakter yaralanmaz.
- Nesne neden uygun olmadığını fiziksel olarak gösterir.
- Anne yalnız gerektiğinde kısa bir cümle söyler.
- Aynı yanlış seçim tekrarlandığında uzun diyalog tekrarlanmaz.
- Checkpoint yeniden denemesi aile planını veya tamamlanmış bölgeleri silmez.

Örnek sonuçlar:

- Mum metal tepsiye düşer; açık alev riski anlatılır.
- Cam şişe ağır bir sesle tezgâha bırakılır.
- Büyük yorgan çantanın ağzını kapatır.
- Oyun konsolu çantayı aşağı çeker.
- Eski kıyafet Can'a küçük gelir.
- Çalışmayan fener, pil takılana kadar ışık üretmez.

## Sonraki perdelerde geri dönüşler

- Story 02'de aynı gardırop ve raf sabitlenir.
- Story 03'te güvenli masa, pencere, gardırop ve çantanın konumu aynıdır.
- Story 03'te çalışan fener, karanlık çıkış rotasını görünür biçimde değiştirir.
- Story 03'te Can'ın konsola bakması için zaman yoktur; küçük hatıra çantada görünür ve önceki karar sessizce hatırlatılır.
- Story 04'te Can'ın düdüğü bir yardım veya yön bulma anında işlev görür.
- Toplanma alanında Melek Teyze iletişim kartı aile birleşmesi bilgisini değiştirir.
