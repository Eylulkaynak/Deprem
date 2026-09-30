# Taşınan çantanın sırt bağlantısı

Model kaynağı `BackpackClosed.blend` dosyasıdır. Hazırlık masasındaki modeller aynı kalır. Taşınan görünüm 44 cm yüksekliğe ölçeklenir ve askılar karaktere bakacak şekilde 180 derece çevrilir.

Sahne üreticisi, Ada'nın ceketi üzerinde sırtın orta bölümüne yakın, dışarı bakan gerçek bir mesh vertex'i seçer. Bu vertex'in bind pose konumları, normali ve bone ağırlıkları native Visual Scripting bağlantılarına çevrilir. Çalışan oyunda yeni bir C# bileşeni gerekmez.

Çantanın sırt pedinin merkezi seçilen giysi noktasını 5 mm açıklıkla izler. Yönü, giysi normali ve kalça–boyun ekseninden hesaplanır. Böylece sırt eğildiğinde çanta yalnız karakterin kök yönünü izleyip dik kalmaz. Ölçeklenmiş ped merkezi ve seçilen vertex `ClientExports/YanYana/Reports/backpack-surface-binding.txt` içinde kayıtlıdır.

Bu bağlantı hesabı görsel kabulün yerine geçmez. Ayakta taşıma ve çömelme görüntüleri normal hızlı hazırlıklı rota kaydında incelenir; sürüm ve test bilgisi `ClientExports/YanYana/Reports/presentation-review.md` dosyasındadır.
