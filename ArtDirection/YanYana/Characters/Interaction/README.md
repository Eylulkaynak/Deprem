# Etkileşimlerde eller

Ada, Efe ve İdil'in onaylı gövde uyarlamalarına, her el için dört düzenlenebilir kavrama şekli eklendi: `Loop`, `Cylinder`, `Soft`, `Pinch`. Bunlar yeni parmak kemikleri gerektirmeyen, Unity'nin yerleşik blend shape özelliğini kullanan mesh şekilleridir. Gövde, yüz, UV ve özgün kaynak mesh korunur; incelenen el bölgelerinin ağırlıkları ilgili el kemiğinde birleştirilir. İdil'in elleri ayrı koruyucu eldiven malzemesi kullanır.

`*_Hands.json` dosyaları el bölgesindeki vertex kimliklerini, avuç yönünü ve her kavramanın fiziksel temas merkezini içerir. Bunlar Editor'da sahne grafikleri yazılırken okunur. Oyunda yeni bir C# el denetleyicisi bulunmaz. Yerleşik `SetBlendShapeWeight`, Transform işlemleri ve projenin mevcut kol çözücüsü, Visual Scripting grafikleri tarafından çağrılır.

Temas hedefleri kutunun gerçek saplarına, fenerin açılmış taşıma sapına, aile kartının kenarına ve hortum başlığının iki elli sapına bağlanır. Avuç merkezi ile bilek kemiği arasındaki fark hesaba katılır. Avuç yönü önkol döndürülerek ayarlanır; bilek, kol yüzeyinden bağımsız çevrilmez. Duraklatıldığında etkin poz korunur. Ada ve İdil'in türev meshlerinde kol-yeni ağırlıkları dirsek ve omuz geçişlerinde de düzenlenmiştir; kaynak proje meshleri korunur.

Şekillerin Unity kaynağı `Assets/YanYana/Characters/Interaction/` altındadır. Düzenlenebilir Blender shape key'leri `ArtDirection/YanYana/Characters/ApprovedStyle/{Ada,Efe,Idil}.blend` dosyalarında bulunur. `Author Interaction Hands`, `Export Editable Approved Characters` ve `Tools/YanYana/import_character_wardrobe_sources.py` üretim adımlarıdır. Mevcut kaynak karakterler bu işlem tarafından değiştirilmez.

Yakın planlar ve temas ölçümleri `ClientExports/YanYana/Reports/interaction-*`, `toy-kit-reach.csv` ve `ClientExports/YanYana/Screenshots/interaction-*` altında tutulur. Sayısal temas hatası tek başına animasyon kalitesi kanıtı değildir; oyun kamerasındaki görüntü ve tüm hareket zinciri ayrıca incelenir. Gerçek dokunma ve hedef yaş oyuncu doğrulaması ayrı kabul koşullarıdır.
