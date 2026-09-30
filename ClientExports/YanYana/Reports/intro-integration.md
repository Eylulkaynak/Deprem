# Gündelik başlangıcın uygulama kaydı

14 Eylül 2026. Yeni sahneye hafif oyun kutusunu taşıma bulmacası eklendi. Ada Efe'ye yürür; alçak sehpadaki kutuyu kavrar; dar aralık için döndürür veya minderlerin çevresinden dolaşır; masadaki şekle hizalayıp bırakır. Ardından mevcut serbest hazırlık açılır. Aile planı, fener ve diğer hazırlıklar bu girişten bağımsız kalır.

Üç yeni düzenlenebilir Blender kaynağı: `ArtDirection/YanYana/Props/IntroToyKit.blend`, `IntroToyStand.blend`, `IntroCushionStack.blend`. Üretim betiği `Tools/YanYana/build_intro_playkit.py`. Kit yaklaşık 72 × 20 cm; ağır eşya taşıma görevi değildir. Yeni runtime C# yoktur. `YanYanaPhysicalIntro.cs` yalnız Editor altında native sahne grafikleri oluşturur. Hareket mevcut `StoryPlayerMovement` ve `StorySiblingFollower` bileşenleriyle çalışır. Minderler ayrı fizik katmanında hacim sorgusuyla kontrol edilir; proje katman ayarları değiştirilmez.

İlk denemede masa yaklaşması yürünebilir alan dışında kaldı. Hedef 1.5 yerine 1.4 metre x konumuna çekildi. Kaynak kamerası kapı arkasından odanın içine taşındı. Masa kenarına girmemesi için bırakma sırasında kutunun tutuş yüksekliği yükseltildi. Bu sorunlar gizli durum atayarak veya karakter ışınlayarak çözülmedi.

## Editörde geçen kontroller

- `physical-intro-narrow.txt`: gerçek yürüyüş ve EventSystem sürüklemeleriyle geniş kutunun minderlerden önce durması; döndürülünce dar aralıktan geçmesi; yanlış bırakma yönünün reddi; doğru hizalamayla bitirme.
- `physical-intro-wide.txt`: kutuyu çevirmeden çevreden dolaşma ve bırakma.
- `physical-intro-save.txt`: ikinci parmağın reddi; yarım dönüşün duraklatmada iptali; tekrarlanan duraklatma; taşırken konum/kutu/kayıt devamı; yarım yerleştirmenin yeniden kurulması; yeniden deneme ve tamamlanmış girişin yeniden yüklenmesi. Son kontrolde aile planı ve fener hâlâ hazırlanmamış durumda.
- `intro-geometry.txt`: son kontrolün konumları ve taşıma durumları. Taşıma anında iki el kemiğinin hedefe mesafesi 0.001 metreden küçük ölçüldü. Bu ölçüm parmakların tüm yüzey temasının veya animasyon kalitesinin tek başına kanıtı değildir.

Dar geçit testi son kol/masa düzenlemesiyle tekrar geçti. Kayıt testi taşıma/bırakma yüksekliği düzeltmesiyle geçti; son avuç yönü değişikliği kayıt verisini değiştirmiyor. Dört tam akış `four-full-routes.md` içinde kayıtlıdır. `physical-playthrough-20260914-013729.mp4`, son kamera düzeniyle hazırlıklı yolun 219,2 saniyelik ek kaydıdır; son kol/avuç yönü ve milimetrik masa yerleşimi düzeltmelerinden önce çekilmiştir. Otomatik süre çocuk oynayışının yerine geçmez.

## Kamera, erişim ve el yüzeyi

İlk üç oranlı gerçek EventSystem raycast kontrolünde kaynak yakın planı geçti; taşıma başlangıcında kapı, bırakma yakın planında Ada kutunun merkezini örttü (üçer başarısız kontrol). Bu yalnız doğrudan pointer olayı gönderen tam tur testinden anlaşılamıyordu. Taşıma için ayrı bir üst kamera, bırakma için karakterin önünden bakan yakın plan kuruldu. Ekran dışına çıkan başlangıç ipucu görünür sınırlar içinde tutuluyor.

`physical-intro-framing.txt` son sahnede 540×960, 540×1170 ve 540×1200 oranlarında üç görünümü; ayrıca taşırken dört bakış yönünü kontrol etti. On üç sorgunun ilk hedefi kutuydu. Arayüz metinleri için taşma raporlanmadı. Görseller `Screenshots/intro-source-*`, `intro-carry-*`, `intro-drop-*` dosyalarında. Bunlar gerçek telefonda dokunma testi değildir.

Dirsekler artık Ada'nın yönüne göre aşağı/dışarı bükülüyor. Onaylı modelin rig'i el kemiklerinde sonlanıyor; ayrı parmak kemikleri yok. İlk standart parmak bağlantısı denemesi bu nedenle authoring hatası verdi ve uygulanmış sayılmadı. Son çözüm, ağırlık verilmiş gerçek el yüzeyinden avuç eksenini editörde hesaplayıp native grafikte el yönünü hizalıyor. Parmağın ayrı ayrı kapanması hâlâ model/şekil anahtarı çalışması gerektirir. `intro-hand-basis.txt` bu sınırlamayı ve ölçülen eksenleri kaydeder.

`intro-table-contact.txt`: masa üstü 0,655 m; hedef plaka kalınlığı 0,006 m; kutunun yerleşim yüksekliği gerçek mesh tabanından hesaplanır. Geometrik taban/plaka farkı 0,000000 m. Bu sayı tek başına animasyon veya parmak teması onayı değildir.

## Kalan doğrulama

Gerçek telefon dokunması, çocuklarla ilk oynayış, parmakların kavrama şekli ve bütün karakter animasyonlarının son sanat kontrolü ayrı koşullardır. Otomatik uzman yürüyüşü 1–2 dakikalık çocuk açılışını veya 28–35 dakikalık tüm macerayı doğrulamaz. Windows paketinin güncel sahne eşleşmesi `windows-package.json` ve `REVIEW_DELIVERY.md` üzerinden izlenir.
