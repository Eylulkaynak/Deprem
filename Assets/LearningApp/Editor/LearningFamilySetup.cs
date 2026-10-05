using System.IO;
using Deprem.Learning;
using UnityEditor;
using UnityEngine;

namespace Deprem.Learning.Editor
{
    public sealed class LearningFamilySetup : EditorWindow
    {
        private const string Path = "Assets/LearningApp/Resources/LearningApp/FamilyConnection.json";
        private string endpoint = "";
        private int timeout = 15;
        [MenuItem("Tools/Deprem App/Family/Service Configuration")]
        public static void Open() => GetWindow<LearningFamilySetup>("Veli bağlantısı");
        private void OnEnable()
        {
            if (!File.Exists(Path)) return;
            var data = JsonUtility.FromJson<LearningFamilyConfig>(File.ReadAllText(Path));
            endpoint = data?.serviceUrl ?? ""; timeout = data?.timeoutSeconds ?? 15;
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Mobil veli hizmeti", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Yayın için ortak HTTPS API adresini girin. Boş bırakılırsa mobil sürümde veli bağlantısı açılmaz; Editor yerel 127.0.0.1:8787 hizmetini kullanır.", MessageType.Info);
            endpoint = EditorGUILayout.TextField("HTTPS hizmet adresi", endpoint);
            timeout = EditorGUILayout.IntSlider("Zaman aşımı (saniye)", timeout, 5, 30);
            bool valid = string.IsNullOrWhiteSpace(endpoint) || !string.IsNullOrEmpty(LearningFamilyConfig.NormalizeEndpoint(endpoint, false));
            if (!valid) EditorGUILayout.HelpBox("HTTPS adresi kullanın. Adreste parola, sorgu veya fragment bulunamaz.", MessageType.Error);
            using (new EditorGUI.DisabledScope(!valid))
                if (GUILayout.Button("Mobil bağlantı ayarını kaydet"))
                {
                    File.WriteAllText(Path, JsonUtility.ToJson(new LearningFamilyConfig { serviceUrl = LearningFamilyConfig.NormalizeEndpoint(endpoint, false), timeoutSeconds = timeout }, true));
                    AssetDatabase.ImportAsset(Path); Debug.Log("[Family] Mobile service configuration saved.");
                }
        }
    }
}
