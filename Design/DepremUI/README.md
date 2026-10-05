# Deprem UI paletleri

`preview.html` dosyasını tarayıcıda açarak dört paleti, üç aşamanın kartlarını, buton durumlarını ve dar ekran yerleşimini inceleyebilirsiniz. Palet seçimi ve örnek ilerleme yalnızca bu önizlemenin belleğinde tutulur.

Bu klasör `Assets` dışında tutulur; açık Unity oturumuna, sahnelere ve mevcut oyun akışına uygulanmaz.

## Unity UI Toolkit için

`DepremColorPalettes.uss` dosyası mevcut `LearningStyles.uss` değişken adlarıyla uyumludur. Entegrasyon sırasında dosyayı Unity kaynaklarına alın, mevcut stillerden sonra yükleyin ve `UIDocument.rootVisualElement` üzerine `theme-orman`, `theme-deniz`, `theme-leylak` veya `theme-gunisigi` sınıflarından yalnızca birini ekleyin. Mevcut sabit renkleri de uyarlamak için aynı köke `deprem-palette` sınıfını ekleyin. Dosya, bu ortak sınıf altında kart ve gezinme bileşenlerini kapsar; aile ve çevrimiçi stillerini değiştirmez.

`palettes.json`, HTML önizlemesinde de kullanılan renk rollerini ve hesaplanmış metin kontrastlarını içerir. Başarı, uyarı ve hata renkleri tüm paletlerde sabittir. Ölçümler düz renk çiftleri içindir; Unity'nin gerçek render çıktısı ayrıca kontrol edilmelidir.

## Tasarım kararları

- Sıcak, açık yüzeyler ve güçlü başlık hiyerarşisi.
- Hazırlık, deprem anı ve sonrası için görünür aşama seçimi.
- Simgeli ve açıklamalı kartlar; tek bir belirgin ana eylem.
- En az 48 px tıklama alanları ve görünür klavye odağı.
- Dar ekranda tek sütun; azaltılmış hareket tercihine uyum.

Bu dosyalar bağımsız UI çalışmasıdır. Önizlemenin düğmeleri gerçek oyunları başlatmaz.
