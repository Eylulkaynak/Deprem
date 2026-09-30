# Windows derleme düzeltmesi

14 Eylül 2026. İlk denemede Visual Scripting'in oluşturduğu `AotStubs.cs`, `Animation.get_Item` erişimi için CS0571 hatası verdi. Editörde çalışan yansıtmalı çağrı oyuncu derlemesinde geçerli C# üretmiyordu. Başarısız derleme özeti `windows-build-indexer-failed-0724.txt` içinde, oluşturulan dosyanın doğrulanmış kopyası `.codex_tmp/yanyana_implementation/failed-aot-20260914-0724/` altında korundu.

`YanYanaPresentationPause.cs` yalnız editör grafiği üretir. İlgili grafikte klip erişimi, `Animation` koleksiyonunu yerleşik `For Each` düğümleriyle dolaşıp klip adıyla eşleştirmeye çevrildi. Her klibin önceki hızı ayrı saklanır. Yeni runtime sınıfı veya paket değişikliği yoktur.

Güncel sahne SHA256: `1e1c299b3dcf5596db0929de89c1c60f776199da625af3778bd2855e59528272`. Tek sahnelik Windows derlemesi başarılı: 0 hata, 0 uyarı, 34,39 saniye. Güncel sahnede `physical-effects-pause-073147.txt`, art arda duraklatma, donan parçacık/klip zamanı, sıfırlanan karakter animasyon hızı ve devam etmeyi doğruladı.

07:30:54'teki hortum bağlantısı QA istisnası, Yeni Oyun kurulumu bitmeden yangın test durumuna geçilmesinden kaynaklandı. Kurulum tamamlandıktan sonra test durumu ve hortum hazırlığı yeniden kuruldu; son duraklatma testi geçti. Bu ara QA kurulum hatası, tam yol testi olarak sayılmadı.
