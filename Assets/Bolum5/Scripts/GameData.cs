using System;
using UnityEngine;

namespace DepremOyunu
{
    /// <summary>
    /// Bir tehlike türünün (hasarlı bina, cam kırıkları, vb.) görsel ve metin bilgisi.
    /// </summary>
    [Serializable]
    public class HazardInfo
    {
        [Tooltip("Kod içinde eşleştirme için benzersiz anahtar, örn. 'bina'")]
        public string key;

        [Tooltip("Oyuncuya gösterilecek kısa isim, örn. 'Hasarlı bina'")]
        public string label;

        [TextArea(2, 4)]
        [Tooltip("Neden tehlikeli olduğunu açıklayan AFAD tavsiyesi")]
        public string tip;

        [Tooltip("Tile üzerinde gösterilecek ikon/sprite (opsiyonel, UI toast için)")]
        public Sprite icon;

        [Tooltip("Bu tehlike türü için karo materyali (opsiyonel görsel ayrım)")]
        public Material tileMaterial;
    }

    /// <summary>
    /// Bir karar baloncuğundaki tek bir seçenek.
    /// </summary>
    [Serializable]
    public class DecisionOption
    {
        [TextArea(1, 3)]
        public string text;
        public bool isCorrect;
    }

    /// <summary>
    /// Bir karar noktasında (örn. "arkadaşım oyuncağını almak istiyor") gösterilecek
    /// soru, seçenekler ve geri bildirim metinleri.
    /// </summary>
    [Serializable]
    public class DecisionInfo
    {
        [Tooltip("Kod içinde eşleştirme için benzersiz anahtar, örn. 'oyuncak'")]
        public string key;

        [Tooltip("Baloncukta gösterilecek emoji/ikon metni, örn. '🧸'")]
        public string icon;

        [Tooltip("Baloncuk başlığı, örn. 'Karar Anı!'")]
        public string title = "Karar Anı!";

        [TextArea(2, 4)]
        public string question;

        public DecisionOption[] options;

        [TextArea(2, 4)]
        [Tooltip("Doğru cevap seçildiğinde gösterilecek övgü/pekiştirme metni")]
        public string okFeedback;

        [TextArea(2, 4)]
        [Tooltip("Yanlış cevap seçildiğinde gösterilecek düzeltici metin")]
        public string noFeedback;
    }
}