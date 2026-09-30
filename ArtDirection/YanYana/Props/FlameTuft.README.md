# Kıvrılan mahalle alevleri

Kaynak: `FlameTuft.blend`. Üretim aracı: `Tools/YanYana/build_flame_mesh.py`. Oyun modeli: `Assets/YanYana/Art/Models/FlameTuft.fbx`.

Üç farklı yükseklikte kıvrılan dilin her biri turuncu kabuk, altın iç alev ve açık renk çekirdek içerir. Dokuz düzenlenebilir mesh, toplam 2592 vertex; hazır efekt paketi veya indirilmiş görsel kullanılmadı. Kaynak Blender koordinatlarında Z yukarıdır; FBX Unity'ye Y yukarı olarak aktarılır.

Unity sahne üreticisi her dil için ayrı bir hareket pivotu oluşturur. `OriginalFlameFlicker.anim`, üç pivotu farklı fazlarla esnetir ve hafifçe yatırır. Unlit malzemeler alevin karanlık yüzeylerde kahverengi görünmesini önler. Alev gölge düşürmez; mevcut ışık, duman, su çarpması ve sönme mekanizmasıyla birlikte çalışır.

Animasyonun kalıcı klip adı kullanılır; yalnız editörde yaşayan AddClip takma adlarına bağlantı kurulmaz. Duraklatma grafiği parçacıkları, klip hızlarını ve karakter animasyonlarını açıkça durdurur. Devam etme işlemi, parmak bırakıldıktan sonra önceki oynatma durumunu geri getirir.

Görünüm ve duraklatma kanıtları `ClientExports/YanYana/Reports/presentation-review.md` dosyasında sürümleriyle tutulur. Hedef yaş oyuncu veya Android performans kabulünün yerine geçmez.
