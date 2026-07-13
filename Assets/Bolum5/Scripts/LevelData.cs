using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepremOyunu
{
    [Serializable]
    public struct GridCoord
    {
        public int row;
        public int col;

        public GridCoord(int r, int c) { row = r; col = c; }

        public Vector2Int ToVector2Int() => new Vector2Int(row, col);

        public override string ToString() => $"{row}-{col}";
    }

    [Serializable]
    public class HazardCell
    {
        public GridCoord pos;
        [Tooltip("HazardInfo listesindeki 'key' alanıyla eşleşmeli")]
        public string hazardKey;
    }

    [Serializable]
    public class DecisionCell
    {
        public GridCoord pos;
        [Tooltip("DecisionInfo listesindeki 'key' alanıyla eşleşmeli")]
        public string decisionKey;
    }

    /// <summary>
    /// Tek bir bölümün (level) tüm verisi: ızgara boyutu, başlangıç/bitiş,
    /// tehlike karoları ve karar noktaları. Her yeni "Bölüm" için ayrı bir
    /// LevelData asset'i oluşturup GameManager'a bağlamanız yeterli.
    /// Create > Deprem Oyunu > Level Data menüsünden oluşturulur.
    /// </summary>
    [CreateAssetMenu(fileName = "YeniBolum_LevelData", menuName = "Deprem Oyunu/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Izgara Boyutu")]
        public int rows = 5;
        public int cols = 8;

        [Header("Kare Boyutu (dünya birimi)")]
        public float tileSize = 1f;

        [Header("Başlangıç / Bitiş")]
        public GridCoord start;
        public GridCoord goal;

        [Header("Tehlikeli Karolar")]
        public List<HazardCell> hazardCells = new List<HazardCell>();

        [Header("Karar Noktaları")]
        public List<DecisionCell> decisionCells = new List<DecisionCell>();

        [Header("Tehlike Tanımları (ikon + AFAD tavsiyesi)")]
        public List<HazardInfo> hazardDefinitions = new List<HazardInfo>();

        [Header("Karar Tanımları (soru + seçenekler)")]
        public List<DecisionInfo> decisionDefinitions = new List<DecisionInfo>();

        [Header("Bölüm Sonu Mesajı")]
        [TextArea(2, 5)]
        public string learningMessage =
            "Toplanma alanına giderken binalardan, duvar diplerinden, enerji hatlarından ve hasarlı yapılardan uzak dur.";

        [TextArea(2, 5)]
        public string afadTavsiyesi =
            "Açık alanda enerji hatlarından, binalardan, direklerden, ağaçlardan ve duvar diplerinden uzaklaş; " +
            "deprem sonrasında da hasarlı binalardan ve enerji nakil hatlarından uzak dur.";

        // ---- Yardımcı sorgular ----

        public TileType GetTileType(int row, int col)
        {
            var c = new GridCoord(row, col);
            if (Equals(c, start)) return TileType.Start;
            if (Equals(c, goal)) return TileType.Goal;

            foreach (var h in hazardCells)
                if (h.pos.row == row && h.pos.col == col) return TileType.Hazard;

            foreach (var d in decisionCells)
                if (d.pos.row == row && d.pos.col == col) return TileType.Decision;

            return TileType.Open;
        }

        public string GetHazardKey(int row, int col)
        {
            foreach (var h in hazardCells)
                if (h.pos.row == row && h.pos.col == col) return h.hazardKey;
            return null;
        }

        public string GetDecisionKey(int row, int col)
        {
            foreach (var d in decisionCells)
                if (d.pos.row == row && d.pos.col == col) return d.decisionKey;
            return null;
        }

        public HazardInfo FindHazard(string key)
        {
            foreach (var h in hazardDefinitions)
                if (h.key == key) return h;
            return null;
        }

        public DecisionInfo FindDecision(string key)
        {
            foreach (var d in decisionDefinitions)
                if (d.key == key) return d;
            return null;
        }

        private static bool Equals(GridCoord a, GridCoord b) => a.row == b.row && a.col == b.col;
    }
}
