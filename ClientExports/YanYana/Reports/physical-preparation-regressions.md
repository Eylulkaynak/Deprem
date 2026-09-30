# Hazırlık ve giriş düzeltmeleri

13 Eylül 2026. Aşağıdakiler Editor bütünleşim testidir; çocuk oynayışı ve gerçek telefon testi değildir.

- Yedi küçük nesne, envanter bayrağı atanmadan gerçek sahne tıklaması ve NavMesh yürüyüşüyle alındı. Yiyecek/su, battaniye ve kart için mobilyanın içinde kalan yaklaşma noktaları düzeltildi. Ayrıntı: physical-collectibles.txt.
- Fener iki pili, kapak sürüklemesi ve anahtarla çalıştırıldı; radyo döner düğmeyle ayarlandı. Dokuz eşya gerçek çanta hücrelerine yerleştirildi ve çanta kapandı. Kayıtlı testte hiçbir envanter veya yerleştirme bayrağı doğrudan atanmadı.
- Aynı anda istenen raf/dolap işi için ilk sürümde iki coroutine aynı yetişkini sahiplenebiliyordu. Beklemeden sonra ikinci kez sahiplik kontrolü eklendi. Yeniden testte ShelfSecured=1, WardrobeSecured=1 ve AdultWorking=false görüldü.
- Açılan kapının NavMesh engeli aynı karede henüz kaldırılmadığı için ilk yürüyüş hedefi içerideki en yakın noktaya çözülebiliyordu. Kısa kapı geçişi boyunca giriş kilitlendi; tamamlanmış çıkış bayrakları yol güncellendikten sonra yazıldı. Yeniden bütünleşik kayıtta sahanlık ve artçı geçildi.
- Üç yangın grubu yeni giriş yüzeyi korumasıyla ilk denemede yaklaşık 18.1 saniyede tamamlandı. Sağlık değeri test tarafından değiştirilmedi.
- Yardım alanında ilk sürükleme kamera yaklaşırken kabul ediliyordu. İlk kişi yanlış konuma döndü, sonraki ikisi geçti. Bu hata kaydı saklandı; yardım kameralarının geçiş sonunda giriş açması eklendi. Yeni sürümün tekrar testi gereklidir.
- Hazırlanmış ve çantaya konmuş radyo yardım bölümünde BroadcastSource=1 ile net yayını açtı. Eksik radyo için görevli alıcısı yolu ayrıca sınanmalıdır.

Eksik eğitim içerikleri ve hedef yaş süresi bu teknik geçişlerle tamamlanmış kabul edilmez.
