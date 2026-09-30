# Yan Yana karakter uyarlamaları

Yedi ana karakter kullanıcının onaylanan proje görünüşünü koruma yönlendirmesiyle, mevcut modellerin YanYana alanındaki ayrı kopyalarından türetilir. Bunlar sıfırdan yontulmuş yedi karakter olarak tanımlanmaz. Eski proje kaynakları değişmez.

Altı arka plan varyasyonu: Selma (turkuaz hırka ve mercan atkı), Mert (toprak rengi kazak ve kalem cebi), Gül (yeşil hırka ve şapka), Deniz (mavi kazak ve çapraz çanta), Aslı (mercan hırka ve saç bandı), Ozan (gri kazak ve yuvarlak gözlük). Yusuf'un saç ve kaşları ağartılmış, gözlük, bıyık, düğmeler ve ayrı baston kavrama yüzeyi eklenmiştir. Kadın varyasyonlarının tabanı Derya, erkeklerin tabanı Emre'dir.

Sekiz aksesuar `Tools/YanYana/build_character_accessories.py` ile Blender'da kendi geometrimizden üretilir. Düzenlenebilir dosyalar `ArtDirection/YanYana/Props`, oyun FBX'leri `Assets/YanYana/Art/Models` altındadır. Hazır aksesuar paketi kullanılmaz.

Giysi/saç bölgesi maskeleri, özgün UV üzerinde değerlendirilmiş model yüzeyinden oluşturulur. `render_wardrobe_regions.py` Blender Emission bake ve 8 piksel kenar dolgusu kullanır. Üç yeni PNG kendi geometri verimizden üretilmiştir; kaynak albedo resimleri değiştirilmez. URP Lit'ten türetilen kendi shader'ımız mevcut ışıklandırma, gölge ve doku ayrıntısını korur. Lisans ve kaynak kaydı `Assets/YanYana/Art/Shaders/SOURCE.md` içindedir.

`Yusuf_CaneGrip.json`, Yusuf'un ayrı el geometrisinin kavrama eksenini ve merkezini içerir. Editor authoring işlemi sağ elin 1.592 köşesini tutarlı el ağırlığına bağlar ve parmak yüzeyini 21 mm yarıçap çevresinde büker. Çalışma anındaki baston hareketi sahnenin native Visual Scripting grafiğindedir: yürüme hızından adım, zemine raycast, mevcut kol çözümleyicisi ve bütün önkol üzerinden kavrama yönü. Yeni runtime C# yoktur. Mesafe ölçümü tek başına doğal kavrama kanıtı sayılmaz; ekran görüntüsüyle birlikte incelenmelidir.

## Kaynakları yeniden üretme

1. Unity'de `Tools/Yan Yana/Art/Author Yusuf Cane Grip`, ardından `Prepare Yusuf And Six Neighbors`.
2. `Export Editable Approved Characters` ile yedi ana karakter ve altı komşunun yüzey, UV, ağırlık, malzeme ve eşleşen kemiklerini dışa aktar.
3. Blender'da `Tools/YanYana/import_character_wardrobe_sources.py` çalıştır. Eski `import_approved_character_sources.py` de bu güncel girişe yönlendirilmiştir.
4. `validate_editable_character_sources.py` ve Blender üzerinden `validate_editable_wardrobe_rigs.py` çalıştır.

Her karakterin tek armature'ü vardır; aksesuarlar uygun kafa/gövde kemiğine bağlıdır. Unity'den ham import uzayındaki yüzey yerine değerlendirilmiş poz ve aynı metre uzayındaki kemikler dışa aktarılır. Önceki ham yüzey export'u ile bu kaynaklar karıştırılmamalıdır.

Kaynak ve kol hareketi doğrulaması, Android performansını veya bütün oyun animasyonlarının görsel kalitesini doğrulamaz.
