// Editor authoring only. The menu uses scene geometry and native Visual Scripting.
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using Unity.VisualScripting;
using Unity.Cinemachine;

namespace YanYana.Editor
{
    public static class YanYanaInterfaceArtDirection
    {
        const string Root = "Assets/YanYana/";
        const string VignetteName = "Açılış · mahallede bir öğleden sonra";
        static TMP_FontAsset body, strong, display;
        static Sprite edge, fade;
        static Color Ink => C("233C3C");
        static Color Cream => C("FFF6DF");
        static Color Gold => C("F3BC59");
        static Color C(string hex) { UnityEngine.ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        static Transform Find(string name) => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == name);
        static RectTransform RT(Transform t) => (RectTransform)t;
        static void Place(Transform t, Vector2 min, Vector2 max, Vector2 lo, Vector2 hi)
        { var r = RT(t); r.anchorMin = min; r.anchorMax = max; r.offsetMin = lo; r.offsetMax = hi; }
        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 lo, Vector2 hi)
        { var o = new GameObject(name, typeof(RectTransform)); o.transform.SetParent(parent, false); Place(o.transform, min, max, lo, hi); return RT(o.transform); }
        static Image Shape(string name, Transform parent, Vector2 min, Vector2 max, Vector2 lo, Vector2 hi, Color color, Sprite sprite = null)
        {
            var i = Rect(name, parent, min, max, lo, hi).gameObject.AddComponent<Image>();
            i.color = color; i.raycastTarget = false; i.sprite = sprite; i.type = sprite == edge ? Image.Type.Sliced : Image.Type.Simple; return i;
        }
        static TMP_Text Text(string name, string value, Transform parent, Vector2 min, Vector2 max, Vector2 lo, Vector2 hi, float size, Color color, TMP_FontAsset face = null)
        {
            var t = Rect(name, parent, min, max, lo, hi).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = face ? face : body; t.text = value; t.fontSize = size; t.color = color;
            t.raycastTarget = false; t.alignment = TextAlignmentOptions.MidlineLeft;
            t.textWrappingMode = TextWrappingModes.Normal; return t;
        }
        static TMP_FontAsset Font(string name, string source, int size)
        {
            string path = Root + "UI/Fonts/" + name + ".asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (!font)
            {
                font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(source), size, 10, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                font.name = name; AssetDatabase.CreateAsset(font, path);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var tex in font.atlasTextures) AssetDatabase.AddObjectToAsset(tex, font);
            }
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.TryAddCharacters(new string(Enumerable.Range(32, 224).Select(i => (char)i).ToArray()) + "ĞğİıŞşÇçÖöÜü’–—…→", out string missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            if (name != "YanYanaLilita" && "ĞğİıŞşÇçÖöÜü".Any(c => !font.HasCharacter(c))) throw new InvalidOperationException(name + " lacks Turkish glyphs: " + missing);
            EditorUtility.SetDirty(font); return font;
        }
        static Sprite Sprite(string name, bool gradient)
        {
            string path = Root + "UI/Generated/" + name + ".png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float distance = new Vector2(Mathf.Max(Mathf.Abs(x - 31.5f) - 20f, 0), Mathf.Max(Mathf.Abs(y - 31.5f) - 20f, 0)).magnitude - 10f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, gradient ? Mathf.SmoothStep(0, 1, y / 63f) : Mathf.Clamp01(1 - distance)));
                }
                tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex); AssetDatabase.ImportAsset(path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = gradient ? Vector4.zero : Vector4.one * 14; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); if (!sprite) throw new InvalidOperationException("UI sprite import failed: " + path); return sprite;
        }
        static void RemoveDecoration(Transform t)
        { foreach (Transform child in t.Cast<Transform>().Where(x => x.name.StartsWith("AD ·")).ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject); }
        static void ButtonStyle(Button b, bool primary = false, bool quiet = false)
        {
            RemoveDecoration(b.transform);
            var image = b.GetComponent<Image>(); image.sprite = edge; image.type = Image.Type.Sliced;
            image.color = quiet ? new Color(1, 1, 1, 0) : primary ? Gold : Ink;
            var label = b.GetComponentInChildren<TMP_Text>(true); label.font = strong; label.color = primary ? Ink : Cream;
            label.fontSize = 22; label.enableAutoSizing = true; label.fontSizeMin = 17; label.fontSizeMax = 22;
            var colors = b.colors; colors.normalColor = Color.white; colors.highlightedColor = C("FFF2CF"); colors.pressedColor = C("C9D9D2"); colors.fadeDuration = .12f; b.colors = colors;
            if (!quiet)
            {
                var shadow = Shape("AD · button depth", b.transform, Vector2.zero, Vector2.one, new Vector2(0, -5), new Vector2(0, -5), primary ? C("866332") : C("14292A"), edge);
                shadow.transform.SetAsFirstSibling();
                // Inset face covers the shadow except for its lower edge.
                var face = Shape("AD · button face", b.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, image.color, edge);
                face.transform.SetSiblingIndex(1); b.targetGraphic = face;
            }
            else b.targetGraphic = image;
            label.transform.SetAsLastSibling();
        }
        static void Arrow(Transform parent)
        {
            var center = new Vector2(1, .5f);
            Shape("AD · arrow stem", parent, center, center, new Vector2(-55, -2), new Vector2(-29, 2), Ink);
            var a = Shape("AD · arrow up", parent, center, center, new Vector2(-39, 2), new Vector2(-24, 6), Ink); a.transform.localRotation = Quaternion.Euler(0, 0, -45);
            var b = Shape("AD · arrow down", parent, center, center, new Vector2(-39, -6), new Vector2(-24, -2), Ink); b.transform.localRotation = Quaternion.Euler(0, 0, 45);
        }

        [MenuItem("Tools/Yan Yana/Art/Apply Immersive Interface")]
        public static void ApplyAndSave()
        {
            Apply(); EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            var machines = UnityEngine.Object.FindObjectsByType<ScriptMachine>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            File.WriteAllText("ClientExports/YanYana/Reports/scene-inventory.json", "{\n  \"scene\": \"" + YanYanaAdventureBuilder.ScenePath + "\",\n  \"version\": \"physical-adventure-immersive-interface\",\n  \"chapters\": 8,\n  \"endings\": 4,\n  \"scriptMachines\": " + machines.Length + ",\n  \"graphUnits\": " + machines.Sum(x => x.graph.units.Count) + ",\n  \"newAuthoredRuntimeCSharp\": 0\n}");
            File.WriteAllText("ClientExports/YanYana/Reports/interface-authoring.txt", "Scene-authored 3D title, Lilita One + Lexend typography, compact HUD, modal separation. Runtime C# added: 0. Visual and input QA required.\n");
        }
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave play mode before UI authoring.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != YanYanaAdventureBuilder.ScenePath && !UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t => t.name.StartsWith("01 Akış"))) throw new InvalidOperationException("Open Yan Yana first.");
            body = Font("YanYanaLexend", "Assets/Fonts/StoryPlayful/Lexend-Regular.ttf", 48);
            strong = Font("YanYanaLexendSemibold", "Assets/Fonts/StoryPlayful/Lexend-SemiBold.ttf", 48);
            display = Font("YanYanaLilita", "Assets/Fonts/StoryPlayful/LilitaOne-Regular.ttf", 96);
            display.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { strong };
            edge = Sprite("AdventureEdge12", false); fade = Sprite("AdventureFade", true);
            var safe = Find("Güvenli ekran alanı"); var title = Find("Yan Yana · başlangıç"); var pause = Find("Duraklatma"); var ending = Find("Senin yolun · neden ve sonuç");
            foreach (var label in safe.GetComponentsInChildren<TMP_Text>(true))
            { label.font = body; label.characterSpacing = 0; label.fontStyle = FontStyles.Normal; }
            foreach (var b in safe.GetComponentsInChildren<Button>(true)) ButtonStyle(b);
            StyleHUD(safe);
            StyleTitle(title);
            StylePause(pause);
            StyleEnding(ending);
            var replay = Find("Dönüp deneyebileceğin kararlar"); replay.GetComponent<Image>().sprite = null; replay.GetComponent<Image>().color = Cream; replay.GetComponent<Image>().raycastTarget = true;
            var replayHeading = replay.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Hangi ana dönelim?"); replayHeading.font = display; replayHeading.fontSize = 40; replayHeading.color = Ink;
            CreateTitleWorld(title);
            ConnectVisibility(safe, title, pause, ending);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        static void StyleHUD(Transform safe)
        {
            var top = Find("Tek amaç"); var image = top.GetComponent<Image>(); image.sprite = edge; image.color = Ink;
            Place(top, new Vector2(0, 1), Vector2.one, new Vector2(20, -124), new Vector2(-20, -20));
            Place(Find("Aktif karakter portresi"), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, -23), new Vector2(66, 41));
            var labels = top.GetComponentsInChildren<TMP_Text>(true);
            var chapter = labels.First(t => t.gameObject.name == "BİZİM MAHALLE");
            var goal = labels.First(t => t.gameObject.name == "Efe’nin yanına git");
            var role = labels.First(t => t.gameObject.name == "Ada");
            Place(chapter.transform, new Vector2(0, 1), Vector2.one, new Vector2(83, -30), new Vector2(-66, -10)); chapter.font = strong; chapter.fontSize = 12; chapter.color = Gold; chapter.characterSpacing = 2;
            Place(goal.transform, Vector2.zero, Vector2.one, new Vector2(82, 12), new Vector2(-60, -33)); goal.font = strong; goal.fontSize = 23; goal.color = Cream; goal.enableAutoSizing = true; goal.fontSizeMin = 20; goal.fontSizeMax = 23;
            Place(role.transform, Vector2.zero, Vector2.zero, new Vector2(8, 5), new Vector2(72, 25)); role.fontSize = 13; role.color = Cream;
            var pause = Find("Pause"); Place(pause, new Vector2(1, 1), Vector2.one, new Vector2(-59, -59), new Vector2(-3, -3)); ButtonStyle(pause.GetComponent<Button>(), false, true);
            pause.GetComponentInChildren<TMP_Text>().text = "";
            Shape("AD · pause left", pause, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-9, -9), new Vector2(-4, 9), Cream);
            Shape("AD · pause right", pause, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(4, -9), new Vector2(9, 9), Cream);
            var dialog = Find("Kısa konuşma"); dialog.GetComponent<Image>().sprite = edge; dialog.GetComponent<Image>().color = Cream;
            Place(dialog, Vector2.zero, new Vector2(1, 0), new Vector2(20, 22), new Vector2(-20, 128)); RemoveDecoration(dialog);
            Shape("AD · speech accent", dialog, Vector2.zero, new Vector2(0, 1), new Vector2(12, 20), new Vector2(16, -20), Gold);
            var line = dialog.GetComponentInChildren<TMP_Text>(); Place(line.transform, Vector2.zero, Vector2.one, new Vector2(30, 15), new Vector2(-22, -15)); line.color = Ink; line.fontSize = 22; line.enableAutoSizing = true; line.fontSizeMin = 20; line.fontSizeMax = 22; line.lineSpacing = 3;
            var gesture = Find("Hareket ipucu"); gesture.GetComponent<Image>().color = new Color(.09f, .16f, .17f, .9f); gesture.GetComponent<Image>().sprite = edge;
            Place(gesture, Vector2.zero, new Vector2(1, 0), new Vector2(48, 138), new Vector2(-48, 185));
            var instruction = gesture.GetComponentInChildren<TMP_Text>(); instruction.fontSize = 17; instruction.enableAutoSizing = true; instruction.fontSizeMin = 15; instruction.fontSizeMax = 17; instruction.color = Cream;
            var roleBanner = Find("Karakter geçişi"); roleBanner.GetComponent<Image>().sprite = edge; roleBanner.GetComponent<Image>().color = Ink;
            roleBanner.GetComponentInChildren<TMP_Text>().font = strong; roleBanner.GetComponentInChildren<TMP_Text>().color = Cream;
            var hint = Find("İsteğe bağlı destek"); hint.GetComponent<Image>().sprite = edge; hint.GetComponent<Image>().color = Gold; hint.GetComponentInChildren<TMP_Text>().color = Ink;
        }

        static void StyleTitle(Transform title)
        {
            var mainImage = title.GetComponent<Image>(); mainImage.sprite = null; mainImage.color = Color.clear; mainImage.raycastTarget = true;
            foreach (Transform child in title.Cast<Transform>().Where(t => t.name != "Continue" && t.name != "New").ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            Shape("AD · sky scrim", title, new Vector2(0, .40f), Vector2.one, Vector2.zero, Vector2.zero, new Color(.07f, .18f, .19f, .84f), fade).transform.SetAsFirstSibling();
            var bottom = Shape("AD · ground scrim", title, Vector2.zero, new Vector2(1, .29f), Vector2.zero, Vector2.zero, new Color(.07f, .14f, .14f, .9f), fade);
            bottom.transform.localRotation = Quaternion.Euler(0, 0, 180); bottom.transform.SetAsFirstSibling();
            var eyebrow = Text("AD · imprint", "ADA İLE EFE’NİN MACERASI", title, new Vector2(.095f, .918f), new Vector2(.95f, .953f), Vector2.zero, Vector2.zero, 12, Cream, strong); eyebrow.characterSpacing = 2;
            OriginalBrand(title);
            var cont = Find("Continue"); Place(cont, new Vector2(.105f, 0), new Vector2(.895f, 0), new Vector2(0, 100), new Vector2(0, 174)); ButtonStyle(cont.GetComponent<Button>(), true); cont.GetComponentInChildren<TMP_Text>().text = "Maceraya devam";
            Arrow(cont);
            var primaryLabel = cont.GetComponentInChildren<TMP_Text>(); primaryLabel.alignment = TextAlignmentOptions.MidlineLeft; Place(primaryLabel.transform, Vector2.zero, Vector2.one, new Vector2(26, 5), new Vector2(-66, -5));
            var fresh = Find("New"); Place(fresh, new Vector2(.23f, 0), new Vector2(.77f, 0), new Vector2(0, 30), new Vector2(0, 90)); ButtonStyle(fresh.GetComponent<Button>(), false, true); fresh.GetComponentInChildren<TMP_Text>().text = "Yeni bir macera"; fresh.GetComponentInChildren<TMP_Text>().fontSizeMax = 17;
            Shape("AD · new underline", fresh, new Vector2(.20f, .14f), new Vector2(.80f, .14f), Vector2.zero, new Vector2(0, 1), new Color(1, .96f, .87f, .5f));
            // The title is a live scene. No character cutout images or background illustration.
        }
        static void OriginalBrand(Transform title)
        {
            // Reuse the established DEPREM identity without changing its source or importer.
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Story/UI/Brand/DepremLogo.png");
            if (!sprite) throw new InvalidOperationException("The original DEPREM logo is missing.");
            var previous = title.Find("AD · wordmark");
            if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var logo = Shape("AD · wordmark", title, new Vector2(.025f, .63f), new Vector2(.975f, .927f), Vector2.zero, Vector2.zero, Color.white, sprite);
            logo.preserveAspect = true;
        }
        [MenuItem("Tools/Yan Yana/Art/Restore Original DEPREM Brand")]
        static void RestoreOriginalBrand()
        {
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != YanYanaAdventureBuilder.ScenePath)
                throw new InvalidOperationException("Open the adventure in Edit mode first.");
            OriginalBrand(Find("Yan Yana · başlangıç"));
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        static void StylePause(Transform pause)
        {
            pause.GetComponent<Image>().sprite = null; pause.GetComponent<Image>().color = C("203839"); pause.GetComponent<Image>().raycastTarget = true;
            var heading = pause.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Bir nefes alalım"); heading.font = display; heading.fontSize = 49; heading.color = Cream;
            Place(heading.transform, new Vector2(.1f, .79f), new Vector2(.9f, .93f), Vector2.zero, Vector2.zero); heading.alignment = TextAlignmentOptions.MidlineLeft;
            ButtonStyle(Find("Resume").GetComponent<Button>(), true);
            foreach (string name in new[] { "ReducedMotion", "Sound", "Captions", "Vibration" })
            { var b = Find(name).GetComponent<Button>(); ButtonStyle(b, false, true); var t = b.GetComponentInChildren<TMP_Text>(); t.alignment = TextAlignmentOptions.MidlineLeft; Shape("AD · setting rule", b.transform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 1), new Color(1, 1, 1, .16f)); }
            var note = pause.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Tamamladığın hareketler kaydediliyor."); note.color = C("ADBDB2"); note.fontSize = 16;
        }
        static void StyleEnding(Transform ending)
        {
            ending.GetComponent<Image>().sprite = null; ending.GetComponent<Image>().color = Cream; ending.GetComponent<Image>().raycastTarget = true;
            var heading = ending.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Senin yolun"); heading.font = display; heading.fontSize = 56; heading.color = Ink;
            foreach (var card in ending.Cast<Transform>().Where(t => t.name.StartsWith("Neden–sonuç")))
            { card.GetComponent<Image>().sprite = edge; card.GetComponent<Image>().color = C("E6E6D4"); card.GetComponentInChildren<TMP_Text>().fontSize = 21; card.GetComponentInChildren<TMP_Text>().color = Ink; }
            ButtonStyle(Find("ReplayPlan").GetComponent<Button>(), true); ButtonStyle(Find("ReplayBag").GetComponent<Button>());
        }

        static GameObject Object(string name, Transform parent, Vector3 at)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = at; return go; }
        static GameObject Model(string name, Transform parent, Vector3 at, float scale, float yaw)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Art/Models/" + name + ".fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent); go.name = "AD · " + name; go.transform.localPosition = at; go.transform.localRotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = Vector3.one * scale;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(m => AssetDatabase.LoadAssetAtPath<Material>(Root + "Art/Materials/" + m.name + ".mat") ?? m).ToArray();
            return go;
        }
        static GameObject Cube(string name, Transform parent, Vector3 at, Vector3 scale, string material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "AD · " + name; go.transform.SetParent(parent, false); go.transform.localPosition = at; go.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Art/Materials/" + material + ".mat"); return go;
        }
        static void CreateTitleWorld(Transform title)
        {
            var old = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == VignetteName);
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var presentation = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t => t.name.StartsWith("05 Sunum"));
            var scene = Object(VignetteName, presentation, new Vector3(-80, 0, 0));
            Cube("avlu", scene.transform, new Vector3(0, -.12f, 0), new Vector3(22, .22f, 26), "YY_leaf");
            Cube("sıcak taş yol", scene.transform, new Vector3(0, -.006f, 1), new Vector3(2.05f, .025f, 22), "YY_stone");
            Model("Facade_1", scene.transform, new Vector3(-1.95f, 0, 4.8f), .52f, 155);
            Model("Facade_3", scene.transform, new Vector3(2.05f, 0, 5.8f), .55f, 205);
            Model("Facade_2", scene.transform, new Vector3(.1f, 0, 8.5f), .31f, 180);
            Model("Bench", scene.transform, new Vector3(-.30f, 0, 1.75f), .64f, 180);
            Model("Plant", scene.transform, new Vector3(-1.28f, 0, .9f), .88f, 20);
            Model("Plant", scene.transform, new Vector3(1.20f, 0, 1.8f), 1.04f, -30);
            Model("Plant", scene.transform, new Vector3(-1.15f, 0, 3.5f), 1.6f, 20);
            Cube("bahçe duvarı sol", scene.transform, new Vector3(-1.40f, .18f, 2.8f), new Vector3(.17f, .36f, 4), "YY_cream");
            Cube("bahçe duvarı sağ", scene.transform, new Vector3(1.40f, .18f, 3.5f), new Vector3(.17f, .36f, 3), "YY_cream");
            for (int i = 0; i < 8; i++) Cube("yol taşı " + i, scene.transform, new Vector3(0, .014f, -1.1f + i * .74f), new Vector3(1.89f, .026f, .68f), i % 2 == 0 ? "YY_cream" : "YY_stone");
            Model("BackpackClosed", scene.transform, new Vector3(-1.0f, .02f, -.3f), .9f, 150);
            for (int i = 0; i < 2; i++)
            {
                string name = i == 0 ? "Ada" : "Efe";
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(YanYanaCharacterStyle.Root + "/" + name + ".prefab"), scene.transform);
                go.name = "AD · " + name + " açılış"; YanYanaCharacterStyle.NormalizeRoot(go); go.transform.localPosition = new Vector3(i == 0 ? -.56f : .59f, 0, i == 0 ? 0 : -.12f); go.transform.localRotation = Quaternion.Euler(0, i == 0 ? 164 : 195, 0);
                var animator = go.GetComponentInChildren<Animator>(); animator.Rebind(); animator.Update(0); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var mesh = new Mesh(); float lowest = float.PositiveInfinity;
                foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.BakeMesh(mesh, true); foreach (var p in mesh.vertices) lowest = Mathf.Min(lowest, r.transform.TransformPoint(p).y); }
                UnityEngine.Object.DestroyImmediate(mesh); animator.transform.position += Vector3.up * (.005f - lowest);
            }
            var fill = Object("AD · yüzlerde yumuşak gün ışığı", scene.transform, new Vector3(0, 3.1f, -4)).AddComponent<Light>();
            fill.type = LightType.Spot; fill.range = 10; fill.spotAngle = 76; fill.intensity = 15; fill.color = C("FFF0D9"); fill.shadows = LightShadows.None; fill.transform.LookAt(scene.transform.position + Vector3.up * .9f);
            var cam = Object("AD · mahalle açılış kadrajı", scene.transform, new Vector3(.12f, 2.5f, -7.6f)); cam.transform.LookAt(scene.transform.position + new Vector3(0, 1.16f, 0));
            var view = cam.AddComponent<CinemachineCamera>(); view.Priority = 100;
            string blendPath = Root + "Animation/TitleCameraBlends.asset";
            var blends = AssetDatabase.LoadAssetAtPath<CinemachineBlenderSettings>(blendPath);
            if (!blends) { blends = ScriptableObject.CreateInstance<CinemachineBlenderSettings>(); AssetDatabase.CreateAsset(blends, blendPath); }
            blends.CustomBlends = new[] { new CinemachineBlenderSettings.CustomBlend { From = cam.name, To = "**ANY CAMERA**", Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0) }, new CinemachineBlenderSettings.CustomBlend { From = "**ANY CAMERA**", To = cam.name, Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0) } };
            Camera.main.GetComponent<CinemachineBrain>().CustomBlends = blends; EditorUtility.SetDirty(blends);
            Camera.main.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            Camera.main.backgroundColor = C("8AB5B3");
            var lens = view.Lens; lens.ModeOverride = LensSettings.OverrideModes.Orthographic; lens.OrthographicSize = 2.28f; lens.NearClipPlane = .05f; lens.FarClipPlane = 45; view.Lens = lens;
            var g = new YanYanaGraphAuthor(scene, "Açılış kadrajını ekran genişliğine uyarla");
            var tick = g.Add(new Unity.VisualScripting.Update());
            var ratio = g.Binary<ScalarDivide>(g.Get(typeof(Screen), "height"), g.Get(typeof(Screen), "width"));
            var factor = g.Call(typeof(Mathf), "Max", null, new[] { typeof(float), typeof(float) }, 1f, g.Binary<ScalarMultiply>(ratio, 9f / 16)).result;
            var setter = g.Add(new SetMember(new Member(typeof(LensSettings), "OrthographicSize")) { chainable = true }); g.Bind(setter.target, g.Get(typeof(CinemachineCamera), "Lens", view)); g.Bind(setter.input, g.Binary<ScalarMultiply>(2.28f, factor)); g.Link(tick.trigger, setter.assign);
            g.Set(setter.assigned, typeof(CinemachineCamera), "Lens", view, setter.targetOutput); g.Dirty();
        }
        static void ConnectVisibility(Transform safe, Transform title, Transform pause, Transform ending)
        {
            // Keep gameplay graphs and their object references intact. Only group render/input state.
            var hud = safe.Find("AD · oyun arayüzü");
            if (!hud) hud = Rect("AD · oyun arayüzü", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            foreach (Transform t in safe.Cast<Transform>().Where(t => t != title && t != pause && t != ending && t != hud && t.name != "Dönüp deneyebileceğin kararlar").ToArray()) t.SetParent(hud, false);
            hud.SetAsFirstSibling(); var group = hud.GetComponent<CanvasGroup>(); if (!group) group = hud.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
            var previous = safe.GetComponents<ScriptMachine>().FirstOrDefault(m => m.graph.title == "Arayüz katmanlarının görünürlüğü"); if (previous) UnityEngine.Object.DestroyImmediate(previous);
            var g = new YanYanaGraphAuthor(safe.gameObject, "Arayüz katmanlarının görünürlüğü"); var tick = g.Add(new Unity.VisualScripting.Update());
            var menu = g.Get(typeof(GameObject), "activeSelf", title.gameObject);
            var hidden = g.Binary<Or>(menu, g.Binary<Or>(g.Get(typeof(GameObject), "activeSelf", pause.gameObject), g.Get(typeof(GameObject), "activeSelf", ending.gameObject)));
            var branch = g.Branch(tick.trigger, hidden);
            var p = g.Set(branch.ifTrue, typeof(CanvasGroup), "alpha", group, 0f); p = g.Set(p, typeof(CanvasGroup), "interactable", group, false); g.Set(p, typeof(CanvasGroup), "blocksRaycasts", group, false);
            p = g.Set(branch.ifFalse, typeof(CanvasGroup), "alpha", group, 1f); p = g.Set(p, typeof(CanvasGroup), "interactable", group, true); g.Set(p, typeof(CanvasGroup), "blocksRaycasts", group, true);
            var titleWorld = Find(VignetteName).gameObject;
            var titleTick = g.Add(new Unity.VisualScripting.Update()); g.Active(titleTick.trigger, titleWorld, menu);
            g.Dirty();
        }
    }
}
