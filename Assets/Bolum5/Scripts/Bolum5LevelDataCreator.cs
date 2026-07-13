#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace DepremOyunu.EditorTools
{
    /// <summary>
    /// Tools > Deprem Oyunu > Bölüm 5 LevelData Oluştur menüsünden çalıştırın.
    /// Web prototipiyle birebir aynı ızgarayı (8x5, aynı tehlike ve karar
    /// konumları) içeren bir LevelData asset'i oluşturur, böylece 40 hücreyi
    /// elle Inspector'dan girmeniz gerekmez. Assets/DepremOyunu/Levels/
    /// altına kaydedilir.
    /// </summary>
    public static class Bolum5LevelDataCreator
    {
        [MenuItem("Tools/Deprem Oyunu/Bölüm 5 LevelData Oluştur")]
        public static void CreateBolum5()
        {
            var asset = ScriptableObject.CreateInstance<LevelData>();
            asset.rows = 5;
            asset.cols = 8;
            asset.tileSize = 1.5f;

            asset.start = new GridCoord(2, 0);
            asset.goal = new GridCoord(2, 7);

            // ---- Tehlike tanımları ----
            asset.hazardDefinitions = new List<HazardInfo>
            {
                new HazardInfo{ key="bina",    label="Hasarlı bina",            tip="Hasarlı binalara yaklaşma, çökme riski vardır." },
                new HazardInfo{ key="cam",     label="Cam kırıkları",           tip="Kırık camlar üzerinden yürüme, yaralanabilirsin." },
                new HazardInfo{ key="direk",   label="Elektrik direği",         tip="Enerji hatlarından ve direklerden uzak dur." },
                new HazardInfo{ key="agac",    label="Ağaç altı",               tip="Deprem sonrası ağaç altında durma, dallar kırılabilir." },
                new HazardInfo{ key="duvar",   label="Duvar dibi",              tip="Duvar diplerinden uzak dur, üzerine yıkılabilir." },
                new HazardInfo{ key="cadde",   label="Kalabalık cadde",         tip="Kalabalık ve trafik olan caddelerden uzak dur." },
                new HazardInfo{ key="deniz",   label="Deniz kıyısı",            tip="Deprem sonrası deniz kıyısından uzaklaş, tsunami riski olabilir." },
                new HazardInfo{ key="ambulans",label="Acil yardım aracının yolu", tip="Acil araçların yolunu asla kapatma." },
            };

            // ---- Tehlike konumları (row, col) ----
            asset.hazardCells = new List<HazardCell>
            {
                new HazardCell{ pos=new GridCoord(0,2), hazardKey="bina" },
                new HazardCell{ pos=new GridCoord(3,2), hazardKey="cam" },
                new HazardCell{ pos=new GridCoord(0,4), hazardKey="direk" },
                new HazardCell{ pos=new GridCoord(3,4), hazardKey="agac" },
                new HazardCell{ pos=new GridCoord(1,1), hazardKey="duvar" },
                new HazardCell{ pos=new GridCoord(2,3), hazardKey="cadde" },
                new HazardCell{ pos=new GridCoord(0,6), hazardKey="deniz" },
                new HazardCell{ pos=new GridCoord(2,6), hazardKey="ambulans" },
            };

            // ---- Karar tanımları ----
            asset.decisionDefinitions = new List<DecisionInfo>
            {
                new DecisionInfo{
                    key="oyuncak", title="Karar Anı!", icon="🧸",
                    question="Arkadaşım oyuncağını almak için eve dönmek istiyor. Ne yapmalıyız?",
                    options = new []{
                        new DecisionOption{ text="Hasarlı binaya girmeyiz, bir büyüğe haber veririz.", isCorrect=true },
                        new DecisionOption{ text="Hemen eve gidip oyuncağı alırız.", isCorrect=false },
                    },
                    okFeedback="Doğru! Hasarlı yerlere asla tek başına girme, önce bir büyüğe haber ver.",
                    noFeedback="Bu güvenli değil. Hasarlı binaya dönmek yerine bir büyüğe haber vermeliyiz."
                },
                new DecisionInfo{
                    key="telefon", title="Karar Anı!", icon="📱",
                    question="Telefonla video çekelim mi?",
                    options = new []{
                        new DecisionOption{ text="Evet, hemen video çekelim.", isCorrect=false },
                        new DecisionOption{ text="Hayır, acil durumlar dışında telefonu meşgul etmeyiz.", isCorrect=true },
                    },
                    okFeedback="Doğru! Telefon hatları acil durumlar için boş tutulmalı.",
                    noFeedback="Doğru değil. Acil durumlarda telefonu boşta tutmalıyız, video çekmemeliyiz."
                },
                new DecisionInfo{
                    key="ambulans_karar", title="Karar Anı!", icon="🚑",
                    question="Ambulans geçiyor, ne yapalım?",
                    options = new []{
                        new DecisionOption{ text="Yolu boş bırakırız, kenara çekiliriz.", isCorrect=true },
                        new DecisionOption{ text="Yolun ortasında durup bakarız.", isCorrect=false },
                    },
                    okFeedback="Doğru! Acil araçların yolu her zaman boş olmalı.",
                    noFeedback="Bu tehlikeli olabilir. Ambulans geçerken yolu boş bırakmalıyız."
                },
            };

            // ---- Karar konumları (yol üzerindeki güvenli hücreler) ----
            asset.decisionCells = new List<DecisionCell>
            {
                new DecisionCell{ pos=new GridCoord(1,2), decisionKey="oyuncak" },
                new DecisionCell{ pos=new GridCoord(2,5), decisionKey="telefon" },
                new DecisionCell{ pos=new GridCoord(3,7), decisionKey="ambulans_karar" },
            };

            asset.learningMessage =
                "Toplanma alanına giderken binalardan, duvar diplerinden, enerji hatlarından ve hasarlı yapılardan uzak dur.";
            asset.afadTavsiyesi =
                "Açık alanda enerji hatlarından, binalardan, direklerden, ağaçlardan ve duvar diplerinden uzaklaş; " +
                "deprem sonrasında da hasarlı binalardan ve enerji nakil hatlarından uzak dur.";

            const string folder = "Assets/DepremOyunu/Levels";
            if (!AssetDatabase.IsValidFolder("Assets/DepremOyunu"))
                AssetDatabase.CreateFolder("Assets", "DepremOyunu");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/DepremOyunu", "Levels");

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/Bolum5_LevelData.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            Debug.Log($"Bölüm 5 LevelData oluşturuldu: {path}");
        }
    }
}
#endif