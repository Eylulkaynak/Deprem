// Editor-only restoration of the test snapshot through PlayerPrefs, including its memory cache.
using System;
using System.IO;
using Microsoft.Win32;
using UnityEditor;
using UnityEngine;

namespace YanYana.Editor
{
    public static class YanYanaInterfaceSaveRestore
    {
        [Serializable] sealed class Snapshot { public Entry[] entries; }
        [Serializable] sealed class Entry { public string key, type, text; public int integer; public float number; }
        const string Prefix = "Deprem.YanYana.v1.";

        [MenuItem("Tools/Yan Yana/QA/Restore Interface Test Save")]
        static void Restore()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop the test first.");
            if (PlayerSettings.companyName != "DepremEgitim" || PlayerSettings.productName != "Deprem") throw new InvalidOperationException("Unexpected PlayerPrefs profile.");
            const string manifest = ".codex_tmp/yanyana_implementation/qa-restore-source.txt";
            string source = File.Exists(manifest) ? File.ReadAllText(manifest).Trim() : ".codex_tmp/ui_redesign_20260914/preferences-unity.json";
            if (!Path.GetFullPath(source).StartsWith(Path.GetFullPath(".codex_tmp") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Snapshot must belong to the project scratch directory.");
            var data = JsonUtility.FromJson<Snapshot>(File.ReadAllText(source));
            foreach (var entry in data.entries) if (!entry.key.StartsWith(Prefix, StringComparison.Ordinal)) throw new InvalidOperationException("Snapshot contains another profile.");
            using (var registry = Registry.CurrentUser.OpenSubKey(@"Software\Unity\UnityEditor\DepremEgitim\Deprem"))
                foreach (string name in registry.GetValueNames())
                    if (name.StartsWith(Prefix, StringComparison.Ordinal)) PlayerPrefs.DeleteKey(name.Substring(0, name.LastIndexOf("_h", StringComparison.Ordinal)));
            foreach (var entry in data.entries)
            {
                if (entry.type == "string") PlayerPrefs.SetString(entry.key, entry.text);
                else if (entry.type == "float") PlayerPrefs.SetFloat(entry.key, entry.number);
                else if (entry.type == "int") PlayerPrefs.SetInt(entry.key, entry.integer);
                else throw new InvalidOperationException("Unknown snapshot value type.");
            }
            PlayerPrefs.Save();
            foreach (var entry in data.entries)
            {
                bool valid = entry.type == "string" ? PlayerPrefs.GetString(entry.key) == entry.text : entry.type == "float" ? PlayerPrefs.GetFloat(entry.key) == entry.number : PlayerPrefs.GetInt(entry.key) == entry.integer;
                if (!valid) throw new InvalidOperationException("PlayerPrefs restoration failed: " + entry.key);
            }
            File.WriteAllText("ClientExports/YanYana/Reports/adventure-save-cache-restoration.txt", "Restored and verified " + data.entries.Length + " values through Unity PlayerPrefs. Other profiles untouched.\n");
        }
    }
}
