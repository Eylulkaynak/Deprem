# 3D oyunlarda görsel çocuk modu

Çocuk profilinde Yan Yana, Deprem Hikâyesi ve 3D pratik oyunlarının 15 sahnesine otomatik uygulanır. Uzun yönergeler kısa kelimelere ve resimlere indirgenir. “Oyna”, “Devam”, “Geri”, “Tekrar”, “Çantanı hazırla” gibi yardımcı yazılar ve oyun başlıkları kalır; hareketli el ve gerçek hedef işaretleri okumadan oynamayı destekler. Simgeler kısa yazının yanında, özgün arayüzün renk ve yazı düzenine uyumlu gösterilir. Yetişkin profili özgün açıklamaları kullanır.

- Görevlerde mevcut eşya/davranış resimleri, ilerlemede dolan noktalar, kalan sürede saat dilimi, sonuçta yıldızlar bulunur.
- Yeni oyun, devam, tekrar, ev, duraklatma ve ayarlar kısa etiketler ve ayrı simgelerle gösterilir. Ayar durumları tik/çarpı ile ayrılır. Uygulamaya dönüş onayında “Ana menü” ve “Devam et” seçenekleri vardır.
- Hikâye etkileşimleri, uygun durumdaki nesne ve varsa gerçek sürükleme hedefi üzerinde hareketli el/hedef halkası gösterir. Girdi kilitliyken ve duraklatmada rehber gizlenir. Rehberler dokunuş yakalamaz.
- Yan Yana rehberi mevcut Visual Scripting durumunu yalnızca okur; giriş hedefini ve uygun çalışma noktalarını gösterir, mevcut fiziksel ipuçlarından da yararlanır. İlerleme, bekleme süresi veya seçim değişkenlerine yazmaz.
- Beş aşamalı pratik oyunundaki özgün nesneye bağlı hareket rehberleri korunur. Yazılı rota/sektör eşleşmeleri daire/kare/üçgen, sinyal seçenekleri farklı ritim şekilleriyle ayrılır.
- Mevcut seslendirmeler korunur. Hikâye konuşmaları gizli yazının harf harf açılmasını bekletmez; anlatım süresi, duraklatma ve dokunarak ilerleme davranışı sürer.
- Çocuk hikâyesinin duraklatma paneli doğrudan açılır: zaman durunca legacy animasyonun sıfır saydamlıkta donması önlenir.
- Uygulamaya dönüş penceresi ayrı bir yüksek sıralı UI Toolkit paneli kullanır; 3D sahnenin UGUI kartlarının arkasında kalmaz.

`Assets/Scripts/UI/ReadingFree3D.cs`, `ReadingFreeIcon.cs` ve `ReadingFreeYanYanaGuide.cs` yalnızca çalışma anındaki sunumu düzenler. Sahne dosyaları yeniden üretilmez. Kaynak TMP metinleri silinmez: mevcut ses eşleştirmeleri, hikâye akışı ve Visual Scripting metin karşılaştırmaları aynı değerleri okuyabilir. Puan, doğru cevap, kayıt ve oynanış kuralları değiştirilmez.

## Doğrulama

Unity menüsü: `Tools > Deprem App > Review > 3D Visual Verification`. Son gezinme düzeltmeleri için `3D Navigation Verification`, dönüş penceresi için `3D Bridge Verification` bulunur. Raporlar ve gerçek Game View görüntüleri `ClientExports/DepremApp/Visual3D` içindedir.

- 15 sahnede çocuk sunumu, resim kaynakları, metin dönüşümü ve rehberlerin dokunuş engellememesi kontrol edildi.
- Beş pratik oyununun 38 aşaması sunum önizlemesiyle denetlendi; bu kontrol bütün aşamaların oyuncu girdileriyle bitirildiği anlamına gelmez.
- Hikâyenin dört bölümünde ve Yan Yana girişinde gerçek UI raycast/click olaylarıyla duraklat/devam; Yan Yana yeni oyun; uygulamaya dönüş/oyunda kalma onayı denetlendi.
- Ses kapalıyken resimli hikâye konuşmasının görünmez typewriter beklemeden açılması ve mevcut dokunma callback'iyle ilerlemesi kontrol edildi.
- 750×1334 telefon görünümü, 1536×2048 hikâye görünümü ve yetişkin hub metinlerinin korunması denetlendi.
- Kontroller önceki eğitim deposunu, hikâye ve mini oyun kayıt dosyalarını, Yan Yana PlayerPrefs değerlerini, ses düzeyini ve Game View boyutunu geri yükler.

5 Ekim 2026 oynanış denetiminde dört hikâye bölümü, beş ortak mini oyunun 38 görevi, sokak tahliyesinin 15 aşaması ve Yan Yana'nın hazırlıklı tam rotası oynatıldı. Yeni altı aşamalı deprem senaryosu, duraklatma/sıfırlama düzeltmeleri ve raporlar [3D polish notlarında](3D_Polish_20261005.md) açıklanır. Sokak görevindeki hareket ipucu uygun hedefin gerçek hareketini izler; haber verme dokunması ve güvenli rota seçimi tehlikeli nesneden uzakta gösterilir.

Bunlar Windows Unity Editor kontrolleridir; fiziksel mobil cihaz ve okuma bilmeyen çocuklarla gözlemsel kullanılabilirlik testi yapılmadı. Yeni hedef/görev eklenirse anlamlı resim eşleştirmesi ve gerçek sahne görüntüsü bu denetimlere eklenmelidir.
