# Yan Yana — aktif fiziksel macera

Sahne: Assets/YanYana/Scenes/YanYana_Adventure.unity. Unity'de **Tools > Yan Yana > Build Adventure** sahneyi ve native Visual Scripting grafiklerini üretir. Yeni C# yalnız Editor klasöründedir. Önceki 124 adımlı prototip aktif ilerlemeyi yönetmez.

Bu sürüm, çocuklarla doğrulanmış 30 dakikalık tamamlanmış oyun olarak sunulmaz. Güncel kanıtlar ve eksikler PHYSICAL_ADVENTURE_STATUS.md ile ClientExports/YanYana/Reports/ içindedir.

| Aktif içerik | Düzenlenebilir kaynak | Unity karşılığı |
|---|---|---|
| Onaylanan çizgiye uyarlanmış yedi karakter | Characters/ApprovedStyle/*.blend, mesh JSON ve dokular | Assets/YanYana/Characters/ApprovedStyle/ |
| Ölçülü daire | Environment/HomeRoom.blend | Art/Models/HomeRoom.fbx |
| Avlu, merdiven, sokaklar ve iki buluşma alanı | Environment/NeighborhoodGround.blend | Art/Models/NeighborhoodGround.fbx |
| Çadır, çanta yüzeyi, alev ve nesneler | Environment/*.blend, Props/*.blend | Aynı adlı FBX modelleri |
| Açılmış su ve yiyecek ambalajları | Props/Water_Opened.blend, Props/Food_Opened.blend | Art/Models/*_Opened.fbx |
| Fermuar ve askı denemesinin çanta yüzleri | Props/BackpackTrialFront.blend, Props/BackpackTrialBack.blend | Art/Models/BackpackTrial*.fbx |
| Aktif portreler | Onaylanan karakter kopyalarının Unity render'ları | UI/Portraits/ |
| Kısa Türkçe konuşmalar | Audio/Physical/manifest.json ve MP3 kaynakları | Audio/Physical/*.wav |

Karakterler, kullanıcının görünüş düzeltmesine göre projenin onaylanmış özel karakterlerinden türetildi. Kendi mesh ve materyal kopyaları kullanılır; asıl proje kaynakları değiştirilmez. Bunlar sıfırdan yontulmuş yedi yeni karakter diye tanımlanmamalıdır. Characters klasörünün kökündeki prosedürel denemeler ve Concepts/Cast_v1.png aktif karakter standardı değildir.

Yeni oda, dolaşım alanları, nesneler ve efekt geometrileri Blender kaynaklarına sahiptir. Konuşmalar sentetiktir; oyun yerel ses dosyalarını çevrimdışı oynatır.

## Yeniden üretme

Aktif oda ve mahalle için Blender 5.2'de Tools/YanYana/build_physical_room.py ve build_continuous_neighborhood.py kullanılır. Yeni sesler için python Tools/YanYana/build_physical_voices.py çalıştırılır. Unity'de Assets > Refresh, ardından Tools > Yan Yana > Build Adventure uygulanır. Elle yapılan yeni sahne düzenlemeleri için yeniden üretmeden önce ayrı kopya saklanmalıdır.

Ambalaj varyantları Tools/YanYana/build_supply_variants.py ile üretilir. Karşılaştırmadaki tarih, öykünün Eylül 2026 zamanına aittir. Çanta malzemelerini düzenli kontrol etme davranışı [AFAD çanta hazırlığı kaynağı](https://www.afad.gov.tr/afet-ve-acil-durum-cantasi-nasil-hazirlanmali) ile kontrol edilmiştir; sahnedeki seçim ve yardım akışı oyuna ait uygulamadır.

Fermuar ve askı denemesinin düzenlenebilir modelleri Tools/YanYana/build_backpack_trial.py ile üretilir. Sahne grafikleri fermuar yolunu, iki ayrı askı ayarını ve kapıya kadar gerçek taşıma yürüyüşünü yönetir.

build_original_art.py betiğinin all seçeneği eski karakter ve portre denemelerini de üretir; aktif onaylı karakterleri yenilemek için kullanılmaz. Düzenlenebilir karakter kaynakları Art > Export Editable Approved Characters ve Tools/YanYana/import_approved_character_sources.py ile üretilir.

Tools > Yan Yana > Delivery altında yalnız macera sahnesini içeren Windows build ve normal hız Game View kaydı araçları vardır. Aracın bulunması, build veya cihaz testinin geçtiği anlamına gelmez; sonuç raporuna bakılmalıdır. Android Build Support ve gerçek test cihazı bu makinede yoktur.

## Eğitim kaynakları

[AFAD rehberi](https://www.afad.gov.tr/deprem-oncesi-ani-ve-sonrasi-alabileceginiz-onlemleri-biliyor-musunuz) ve [KKTC Sivil Savunma okul kaynağı](https://sivilsavunma.gov.ct.tr/Portals/33/3-%20OKULLARDA-AFET-EGITIMI-ICERIK.pdf) temel alınır. Ağır mobilya işi yetişkine, yangın müdahalesi İdil'e aittir. Yardım almak puan kaybettirmez. Nihai eğitim ve çocuk oynayışı değerlendirmesi ayrıca yapılmalıdır.
