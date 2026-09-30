# Gündelik başlangıç: Efe'nin oyun kutusu

Yeni oyunda Ada önce Efe'nin yanına yürür. Efe, hafif oyun kutusunun oyun yerine getirilmesini ister. Oyuncu kutuyu alçak raftan kendine çekerek alır; kutuyla yürürken dar aralığa takılabilir, kutuyu çevirebilir veya geniş taraftan dolaşabilir. Oyun yerinde geniş yönde hizalayıp bırakır. Sonra serbest deprem hazırlığı açılır. Hazırlık bittiğinde aynı kitin köprü parçaları Efe'nin oyununda kullanılır.

Bu başlangıç aile planını kendiliğinden öğrenilmiş saymaz. Seçimler sonraki hazırlıkları tamamlamaz. Önceki macera sürümünün kaydında intro anahtarları yoksa var olan ilerleme korunarak bu giriş atlanır; Yeni Oyun girişi baştan oynatır.

- Sahne dışında runtime C# yazılmaz: native Visual Scripting, mevcut hareket bileşenleri, NavMeshObstacle ve fizik sorguları kullanılır.
- Kutu yaklaşık 72 × 20 cm hafif oyuncak tepsisidir. Taşıma halkaları iki yönde tutuşa uygundur. Kaynak raf ve minderler de yeni Blender üretimidir.
- Minder aralığı gövdeye yeterli, geniş kutuya yetersizdir. Öngörülü hacim kontrolü karakteri çarpmadan durdurur; başka yöne yürümek veya döndürmek serbesttir. Işınlanma ve görünmez süre kilidi kullanılmaz.
- Kaynak konum: (-2.5, .64, -2.9). Dar geçiş x=-.1, z=-2.72 civarı. Masadaki kutu merkezi x=1.88, z=-3.2; y gerçek masa yüzeyi ve kutu tabanından hesaplanır (yaklaşık .721 m). Yürünebilir yaklaşma (1.4, 0, -3.2). Bırakma kamerasında kutu masa kenarının üzerine kaldırılır. Depremdeki korunma yaklaşımı açık kalır.
- İlk sefer için içerik hedefi 1–2 dakikadır; gerçek çocuk süresi ayrıca ölçülmelidir. Otomatik çözüm süre kanıtı değildir.
- Kalıcı durum: IntroDone, IntroStage, IntroOrientation, IntroRoute, IntroCourseCleared, IntroEfeWaiting. Yarım alma/bırakma güvenli küçük eylemin başına döner.

Kabul: iki taşıma rotası; dar geçişte erken durma; dönünce geçebilme; hizasız bırakmanın reddi; duraklatma ve kayıt; hazırlıkların istemeden tamamlanmaması; elde ve masada gerçek temas; üç dikey oran. Bu dosya tasarım kaydıdır; uygulama ve test kanıtı `ClientExports/YanYana/Reports/intro-integration.md` altında tutulur.
