// Editor-only preparation authoring and evidence. No player component is added.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using Unity.VisualScripting;
using TMPro;
using UnityEngine.EventSystems;

namespace YanYana.Editor
{
    public static class YanYanaPreparationPolish
    {
        public const string Evidence = "ClientExports/YanYana/PreparationReview";
        static T[] All<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        static Transform Find(string name) => All<Transform>().Single(t => t.name == name);
        static Transform FlashlightPart(string name) => Find("Fenerin pil yuvası ve gerçek anahtarı").Cast<Transform>().First(t => t.name == name);
        static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
        static Bounds BoundsOf(Transform root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponent<MeshFilter>() != null).ToArray();
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static readonly string[] RequiredItems = { "Water", "Food", "Flashlight", "Radio", "FirstAid", "Blanket", "FamilyCard", "Whistle" };
        static GameObject Flow => All<Transform>().First(t => t.name.StartsWith("01 Akış")).gameObject;
        static TMP_Text Dialogue => Find("Kısa konuşma").GetComponentsInChildren<TMP_Text>(true).First(t => !t.name.StartsWith("KKTC_"));
        static object Constant(ValueInput input)
        {
            if (input.hasValidConnection && input.connection.source.unit is Literal literal) return literal.value;
            return input.unit.defaultValues.TryGetValue(input.key, out var value) ? value : null;
        }
        static ValueOutput Is(YanYanaGraphAuthor g, object a, object b) => g.Binary<Equal>(a, b);
        static ControlOutput Say(YanYanaGraphAuthor g, ControlOutput p, string text) => g.Set(p, typeof(TMP_Text), "text", Dialogue, text);
        static void RewriteConstants(Func<object, object> rewrite, IEnumerable<ScriptMachine> machines = null)
        {
            foreach (var machine in machines ?? All<ScriptMachine>())
            {
                if (machine.graph == null) continue;
                foreach (var unit in machine.graph.units)
                {
                    if (unit is Literal literal) literal.value = rewrite(literal.value);
                    foreach (var key in unit.defaultValues.Keys.ToArray()) unit.defaultValues[key] = rewrite(unit.defaultValues[key]);
                }
                EditorUtility.SetDirty(machine);
            }
        }
        static void MoveRest(Transform item, Vector3 destination)
        {
            var old = item.position;
            if ((old - destination).sqrMagnitude < .00000001f) return;
            RewriteConstants(v => v is Vector3 p && (p - old).sqrMagnitude < .00000001f ? destination : v);
            item.position = destination;
        }
        static void Ground(Transform item, float surface, Vector3? horizontal = null)
        {
            var bounds = BoundsOf(item);
            var target = item.position + Vector3.up * (surface + .001f - bounds.min.y);
            if (horizontal.HasValue) { target.x += horizontal.Value.x - bounds.center.x; target.z += horizontal.Value.z - bounds.center.z; }
            MoveRest(item, target);
        }
        static void ColliderToVisual(Transform root)
        {
            var hit = root.GetComponent<BoxCollider>();
            if (!hit) return;
            var bounds = BoundsOf(root); var local = new Bounds(root.InverseTransformPoint(bounds.center), Vector3.zero);
            for (int i = 0; i < 8; i++) local.Encapsulate(root.InverseTransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            hit.center = local.center; hit.size = Vector3.Max(local.size, Vector3.one * .14f);
        }
        static GameObject Block(string name, Transform parent, Vector3 center, Vector3 size, Material material)
        {
            var old = parent.Find(name); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true); go.transform.position = center; go.transform.rotation = Quaternion.identity; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        [MenuItem("Tools/Yan Yana/Art/Polish Preparation And Save")]
        public static void ApplyAndSave()
        {
            Apply(); EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            Inspect(); CaptureViews("after");
        }
        public static void Apply()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != YanYanaAdventureBuilder.ScenePath)
                throw new InvalidOperationException("Open the adventure in Edit mode before preparation authoring.");
            var version = Variables.Object(Flow);
            if (!version.IsDefined("PreparationGeometryV3"))
            {
                GroundRoomItems(); GroundPacking(); GroundDevices(); GroundSupplies();
                version.Set("PreparationGeometryV3", true);
            }
            if (!version.IsDefined("PreparationReadablePropsV4"))
            {
                FaceReadableDetailsUp(); MakeFlashlightControlsAccessible();
                version.Set("PreparationReadablePropsV4", true);
            }
            AuthorPreparationRules(); AuthorReadableFeedback(); EditorUtility.SetDirty(Flow.GetComponent<Variables>());
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        static void GroundRoomItems()
        {
            float bench = BoundsOf(Find("PackingBench_Pad")).max.y;
            float counter = BoundsOf(Find("Workbench_Top")).max.y;
            var food = Find("Odada bulunacak · Food");
            // Preserve the FBX's own unit/axis conversion inside the existing interaction root.
            foreach (var child in food.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            food.localScale = Vector3.one; food.rotation = Quaternion.identity;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/YanYana/Art/Models/Food.fbx"), food);
            visual.transform.localScale *= .23f / BoundsOf(visual.transform).size.y;
            var materials = AssetDatabase.FindAssets("t:Material", new[]{"Assets/YanYana/Art/Materials"}).Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).GroupBy(m => m.name).ToDictionary(g => g.Key, g => g.First());
            foreach (var r in visual.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(m => m && materials.TryGetValue(m.name, out var own) ? own : m).ToArray();
            Ground(food, counter, new Vector3(-2.30f, 0, 2.70f)); ColliderToVisual(food);
            Ground(Find("Odada bulunacak · Water"), counter);
            Ground(Find("Odada bulunacak · FirstAid"), BoundsOf(Find("ShelfBoard.002")).max.y);
            Ground(Find("Odada bulunacak · Blanket"), BoundsOf(Find("Sofa_Seat.001")).max.y, new Vector3(1.97f, 0, -.32f));
            Ground(Find("Odada bulunacak · ComfortFox"), BoundsOf(Find("SafeTable_Top")).max.y);
            var card = Find("Odada bulunacak · FamilyCard"); card.rotation = Quaternion.Euler(90, 180, 0);
            Ground(card, bench, new Vector3(1.98f, 0, -2.57f)); ColliderToVisual(card);
            Ground(Find("Odada bulunacak · Whistle"), bench, new Vector3(1.99f, 0, -2.20f));
        }
        static void GroundPacking()
        {
            float bench = BoundsOf(Find("PackingBench_Pad")).max.y;
            var cloth = Find("Düzenleme bezi"); Ground(cloth.parent, bench);
            float clothTop = BoundsOf(cloth).max.y;
            foreach (var item in All<Transform>().Where(t => t.name.StartsWith("Yerleşim ·")).ToArray())
            {
                var pad = item.Find("Yumuşak eşya altlığı");
                var model = item.Cast<Transform>().First(t => t != pad);
                model.position += Vector3.up * (BoundsOf(pad).max.y + .0005f - BoundsOf(model).min.y);
                MoveRest(item, item.position + Vector3.up * (clothTop + .0005f - BoundsOf(pad).min.y));
                ColliderToVisual(item);
            }
            // Imported FBX axes differ from world axes. Keep its mesh proportions and
            // author the continuous lower padding in world space beneath the real bag.
            var basePart = Find("PackingBagShell").Find("SoftBagBack");
            var source = PrefabUtility.GetCorrespondingObjectFromSource(basePart);
            if (source) basePart.localScale = source.localScale;
            var bounds = BoundsOf(basePart);
            Block("Çantanın alt dolgusu", basePart.parent.parent, new Vector3(bounds.center.x, (bench + .001f + bounds.min.y) * .5f, bounds.center.z), new Vector3(bounds.size.x, Mathf.Max(.002f, bounds.min.y - bench - .001f), bounds.size.z), basePart.GetComponent<Renderer>().sharedMaterial);
            Ground(Find("Çantanın kapanan dış yüzü"), bench, Find("Anchor_BagWork").position + Vector3.forward * .12f);
        }
        static void GroundDevices()
        {
            float counter = BoundsOf(Find("Workbench_Top")).max.y;
            foreach (string name in new[]{"AA pil 1", "AA pil 2"}) Ground(Find(name), counter);
            var cover = Find("Kaydırılabilir pil kapağı");
            var center = Find("Anchor_FlashlightWork").position + Vector3.up * .04f;
            var old = center + new Vector3(.21f, .014f, -.04f);
            var rest = new Vector3(old.x, counter + .010f, old.z);
            RewriteConstants(v => v is Vector3 p && (p - old).sqrMagnitude < .00000001f ? rest : v);
            var body = Find("Gövde tabanı"); var bottom = BoundsOf(body).min.y;
            foreach (float x in new[]{-.05f, .05f})
                Block("Fenerin kauçuk desteği " + x, body.parent, new Vector3(center.x + x, (bottom + counter + .001f) * .5f, center.z), new Vector3(.025f, bottom - counter - .001f, .24f), body.GetComponent<Renderer>().sharedMaterial);
            var radio = Find("Ayarlanabilir radyo");
            // The imported body was offset through the bench and doubled the authored tuning face.
            foreach (var r in radio.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var face = Find("Ayar yüzü"); var cb = BoundsOf(face); var anchor = Find("Anchor_RadioWork").position;
            Block("Radyonun taşıyıcı gövdesi", radio.parent, new Vector3(anchor.x, (counter + .001f + cb.min.y) * .5f, anchor.z), new Vector3(.38f, cb.min.y - counter - .001f, .24f), face.GetComponent<Renderer>().sharedMaterial);
        }
        static void GroundSupplies()
        {
            float counter = BoundsOf(Find("Workbench_Top")).max.y;
            foreach (string key in new[]{"Water", "Food"})
            {
                var root = Find("Ambalaj incelemesi · " + key);
                var mat = root.Find("Temiz karşılaştırma yüzeyi");
                float shift = counter + .001f - BoundsOf(mat).min.y;
                root.position += Vector3.up * shift;
                float surface = BoundsOf(mat).max.y;
                float oldPlane = Find("Anchor_RadioWork").position.y + .115f;
                float newPlane = surface + .001f;
                var machines = All<ScriptMachine>().Where(m => m.transform.IsChildOf(root) || m.gameObject == Flow).ToArray();
                RewriteConstants(v => v is Vector3 p && Mathf.Abs(p.y - oldPlane) < .00001f && p.x > -2.5f && p.x < -1.2f && p.z > 1.7f && p.z < 2.8f ? new Vector3(p.x, newPlane, p.z) : v, machines);
                RewriteConstants(v => v is float f && Mathf.Abs(f - oldPlane) < .00001f ? newPlane : v, root.GetComponentsInChildren<ScriptMachine>(true));
                foreach (var candidate in root.Cast<Transform>().Where(t => t.name.StartsWith("İncelenen "))) Ground(candidate, surface);
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    var p = label.transform.position; p.y = surface + .002f; label.transform.position = p;
                    if (label.text.StartsWith("BUGÜN:")) label.text = "ÖRNEK: 09 / 2026";
                }
                foreach (string name in new[]{"Seçtiğin malzemenin yeri", "Ambalajı çevir · " + key}) Ground(root.Find(name), surface);
            }
        }
        static Material MaterialNamed(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/YanYana/Art/Materials/YY_" + name + ".mat");
        static void SurfaceText(string name, Transform parent, string text, Vector3 position, Vector2 size)
        {
            var go = new GameObject("KKTC_" + name); go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(90, 180, 0));
            var label = go.AddComponent<TextMeshPro>(); label.font = Dialogue.font; label.text = text;
            label.color = new Color(.08f, .19f, .19f); label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false; label.fontSize = .26f;
            label.rectTransform.sizeDelta = size; label.enableWordWrapping = false;
            label.ForceMeshUpdate(true);
        }
        static void FaceReadableDetailsUp()
        {
            foreach (var item in All<Transform>().Where(t => t.name.StartsWith("Yerleşim ·")).ToArray())
            {
                string key = item.name.Substring("Yerleşim · ".Length);
                var pad = item.Find("Yumuşak eşya altlığı");
                var model = item.Cast<Transform>().First(t => t != pad);
                if (key == "Food")
                {
                    foreach (var child in item.Cast<Transform>().Where(t => t != pad).ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                    model = new GameObject("Food").transform; model.SetParent(item, false);
                    var food = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/YanYana/Art/Models/Food.fbx"), model);
                    foreach (var r in food.GetComponentsInChildren<Renderer>())
                        r.sharedMaterials = r.sharedMaterials.Select(m => m ? MaterialNamed(m.name.Replace("YY_", "")) ?? m : m).ToArray();
                }
                float tilt = key == "Blanket" || key == "Whistle" ? 0 : -90;
                float turn = key == "FamilyCard" ? 0 : key == "Radio" || key == "Food" ? 90 : 180;
                model.localRotation = Quaternion.Euler(tilt, turn, 0);
                var b = BoundsOf(model); var p = BoundsOf(pad);
                float fit = Mathf.Min((p.size.x - .006f) / b.size.x, (p.size.z - .006f) / b.size.z, .12f / b.size.y);
                model.localScale *= fit; b = BoundsOf(model);
                model.position += new Vector3(item.position.x - b.center.x, p.max.y + .0005f - b.min.y, item.position.z - b.center.z);
                if (key == "Food")
                {
                    b = BoundsOf(model);
                    var label = Block("Gıda ambalajı etiketi", item, new Vector3(b.center.x, b.max.y + .0006f, b.center.z), new Vector3(b.size.x * .68f, .001f, b.size.z * .58f), MaterialNamed("cream"));
                    SurfaceText("Pişirmeden yenebilir gıda", item, "GIDA", label.transform.position + Vector3.up * .0008f, new Vector2(b.size.x * .65f, b.size.z * .48f));
                }
                ColliderToVisual(item);
            }
            var card = Find("Odada bulunacak · FamilyCard"); card.rotation = Quaternion.Euler(-90, 0, 0);
            Ground(card, BoundsOf(Find("PackingBench_Pad")).max.y, new Vector3(1.98f, 0, -2.57f)); ColliderToVisual(card);
            var face = Find("Ayar yüzü"); var fb = BoundsOf(face); var dark = MaterialNamed("navy");
            // An attached carry handle and telescopic aerial make the workbench device readable as a radio.
            float z = fb.max.z + .018f, y = fb.center.y;
            foreach (float x in new[]{fb.center.x - .10f, fb.center.x + .10f})
                Block("Radyo kulp ayağı " + x, face.parent, new Vector3(x, y, z), new Vector3(.018f, .035f, .055f), dark);
            Block("Radyo taşıma kulpu", face.parent, new Vector3(fb.center.x, y, z + .024f), new Vector3(.218f, .035f, .018f), dark);
            var start = new Vector3(fb.min.x + .025f, fb.center.y, fb.max.z - .03f);
            var end = start + new Vector3(-.035f, .045f, .19f);
            var previousAerial = face.parent.Find("Radyonun teleskopik anteni"); if (previousAerial) UnityEngine.Object.DestroyImmediate(previousAerial.gameObject);
            var aerial = GameObject.CreatePrimitive(PrimitiveType.Cylinder); aerial.name = "Radyonun teleskopik anteni";
            UnityEngine.Object.DestroyImmediate(aerial.GetComponent<Collider>()); aerial.transform.SetParent(face.parent, true);
            aerial.transform.position = (start + end) * .5f;
            aerial.transform.rotation = Quaternion.FromToRotation(Vector3.up, end - start);
            aerial.transform.localScale = new Vector3(.009f, Vector3.Distance(start, end) * .5f, .009f);
            aerial.GetComponent<Renderer>().sharedMaterial = MaterialNamed("metal");
        }
        static void MakeFlashlightControlsAccessible()
        {
            var counter = Find("Workbench_Top"); var head = FlashlightPart("Lamba başlığı");
            Ground(head, BoundsOf(counter).max.y);
            var center = Find("Anchor_FlashlightWork").position + Vector3.up * .04f;
            foreach (var part in new[]{("Mercek", .235f), ("Merceğin yanan yüzü", .243f)})
                FlashlightPart(part.Item1).position = new Vector3(center.x, head.position.y, center.z + part.Item2);
            Find("Fener ışığı").position = new Vector3(center.x, head.position.y, center.z + .246f);
            var button = Find("Gerçek açma anahtarı"); var b = BoundsOf(head);
            button.position = new Vector3(b.center.x, b.max.y + BoundsOf(button).size.y * .5f + .0005f, b.center.z - .015f);
            button.GetComponent<BoxCollider>().size = new Vector3(1.7f, 1.4f, 1.25f);
            Ground(Find("Işığı görme kartı"), BoundsOf(counter).max.y);
            var cover = Find("Kaydırılabilir pil kapağı"); float oldHeight = cover.position.y;
            var closed = cover.position + Vector3.up * (BoundsOf(FlashlightPart("Gövde kenarı")).max.y + .001f - BoundsOf(cover).min.y);
            MoveRest(cover, closed);
            RewriteConstants(v => v is float f && Mathf.Abs(f - oldHeight) < .000001f ? closed.y : v, cover.GetComponents<ScriptMachine>());
            foreach (string name in new[]{"AA pil 1", "AA pil 2"})
            foreach (var label in Find(name).GetComponentsInChildren<TextMeshPro>(true))
            {
                label.fontSize = .32f; var p = label.transform.localPosition; p.y = .0155f; label.transform.localPosition = p; label.ForceMeshUpdate(true);
            }
        }
        // Insert native graph gates into the existing production path, preserving every binding.
        static If GateBefore(YanYanaGraphAuthor g, ControlInput destination, object condition)
        {
            var incoming = g.Graph.controlConnections.Where(c => c.destination == destination).ToArray();
            var gate = g.Branch(null, condition);
            foreach (var c in incoming) { var source = c.source; g.Graph.controlConnections.Remove(c); g.Link(source, gate.enter); }
            g.Link(gate.ifTrue, destination); return gate;
        }
        static void AuthorPreparationRules()
        {
            foreach (var machine in All<ScriptMachine>().Where(m => m.graph != null && !m.graph.variables.IsDefined("PreparationRulesV2")).ToArray())
            {
                var title = machine.graph.title; var g = new YanYanaGraphAuthor(machine); bool changed = false;
                if (title == "Kesintisiz macera · gerçek nesne durumları")
                {
                    foreach (string item in new[]{"Flashlight", "Radio"})
                    foreach (var set in g.Graph.units.OfType<SetVariable>().Where(u => (string)Constant(u.name) == "Found." + item && Equals(Constant(u.input), 1)).ToArray())
                    {
                        var gate = GateBefore(g, set.assign, Is(g, g.Var(item + "Ready", Flow), 1));
                        Say(g, gate.ifFalse, item == "Flashlight" ? "Derya: Feneri çantaya koymadan önce pilleriyle çalıştırıp ışığını deneyelim." : "Derya: Radyoyu çantaya koymadan önce düğmesini çevirip net yayını bulalım.");
                    }
                    var open = g.Graph.units.OfType<CustomEvent>().First(u => (string)Constant(u.name) == "OpenBagFit");
                    var connection = g.Graph.controlConnections.First(c => c.source == open.trigger); var target = connection.destination;
                    g.Graph.controlConnections.Remove(connection); var p = open.trigger;
                    foreach (string item in RequiredItems)
                    {
                        var gate = g.Branch(p, g.Binary<GreaterOrEqual>(g.Var("Pack." + item + ".X", Flow), 0));
                        Say(g, gate.ifFalse, "Derya: Önce " + ItemLabel(item) + " çantaya yerleştir. " + Purpose(item)); p = gate.ifTrue;
                    }
                    foreach (string key in new[]{"FlashlightReady", "RadioReady", "WaterReady", "FoodReady"})
                    {
                        var gate = g.Branch(p, Is(g, g.Var(key, Flow), 1));
                        Say(g, gate.ifFalse, "Derya: Ekipmanı çalıştırıp su ve gıdanın ambalajını kontrol edelim."); p = gate.ifTrue;
                    }
                    g.Link(p, target); changed = true;
                }
                if (title == "Ambalajın durumuyla malzeme seç; sonuç daha sonra görünür")
                {
                    string key = machine.name.Contains("Water") ? "Water" : "Food";
                    bool intact = machine.name.EndsWith(" 1");
                    var set = g.Graph.units.OfType<SetVariable>().First(u => (string)Constant(u.name) == "Found." + key);
                    var viewed = GateBefore(g, set.assign, g.Binary<And>(intact, Is(g, g.Var("SupplyViewed" + key, Flow), 1)));
                    var p = g.Set(viewed.ifFalse, typeof(Transform), "position", machine.transform, machine.transform.position);
                    Say(g, p, !intact ? (key == "Water" ? "Derya: Kapak açılmış ve tarih geçmiş. Bunu çantaya koymayalım; kapalı şişeyi incele." : "Derya: Paket yırtık ve tarih geçmiş. Bunu çantaya koymayalım; sağlam paketi incele.") : "Derya: Önce ambalajı çevir. Kapağı veya paketi ve üzerindeki tarihi kontrol et."); changed = true;
                }
                if (title.StartsWith("Çantada alan, yön ve çakışma · "))
                {
                    string key = machine.name.Substring("Yerleşim · ".Length);
                    var select = g.Graph.units.OfType<SetVariable>().First(u => (string)Constant(u.name) == "SelectedItem");
                    var gate = GateBefore(g, select.assign, Is(g, g.Var("Dragging", Flow), false));
                    var accepted = g.Graph.controlConnections.First(c => c.source == gate.ifTrue); g.Graph.controlConnections.Remove(accepted);
                    var p = g.SetVar(gate.ifTrue, "Dragging", true, Flow);
                    var down = g.Graph.units.OfType<OnPointerDown>().Single();
                    p = g.SetVar(p, "PackingFinger", g.Get(typeof(PointerEventData), "pointerId", down.data));
                    p = Say(g, p, ItemLabel(key) + ": " + Purpose(key)); g.Link(p, select.assign);
                    g.Initial("PackingFinger", -999);
                    var up = g.Add(new OnPointerUp()); g.Bind(up.target, machine.gameObject);
                    var release = g.Branch(up.trigger, g.Binary<And>(Is(g, g.Var("PackingFinger"), g.Get(typeof(PointerEventData), "pointerId", up.data)), Is(g, g.Get(typeof(PointerEventData), "dragging", up.data), false)));
                    p = g.SetVar(release.ifTrue, "Dragging", false); g.SetVar(p, "Dragging", false, Flow);
                    var drag = g.Graph.units.OfType<OnDrag>().Single(); var end = g.Graph.units.OfType<OnEndDrag>().Single();
                    foreach (var entry in new[]{(drag.trigger, drag.data), (end.trigger, end.data)})
                    {
                        var connection = g.Graph.controlConnections.First(c => c.source == entry.Item1); var target = connection.destination;
                        g.Graph.controlConnections.Remove(connection);
                        var own = g.Branch(entry.Item1, Is(g, g.Var("PackingFinger"), g.Get(typeof(PointerEventData), "pointerId", entry.Item2)));
                        if (entry.Item1 == end.trigger) { p = g.SetVar(own.ifTrue, "Dragging", false, Flow); g.Link(p, target); }
                        else g.Link(own.ifTrue, target);
                    }
                    changed = true;
                }
                if (changed) { g.Graph.variables.Set("PreparationRulesV2", true); g.Dirty(); }
            }
        }
        static string ItemLabel(string key)
        {
            switch(key) { case "Water": return "su şişesini"; case "Food": return "dayanıklı gıdayı"; case "Flashlight": return "el fenerini"; case "Radio": return "pilli radyoyu"; case "FirstAid": return "ilk yardım setini"; case "Blanket": return "hafif battaniyeyi"; case "FamilyCard": return "aile iletişim kartını"; case "Whistle": return "düdüğü"; default: return "küçük oyuncağı"; }
        }
        static string Purpose(string key)
        {
            switch(key) {
                case "Water": return "Kapalı şişe seç; ailedeki herkes için yeterli suyu yetişkininle planla.";
                case "Food": return "Sağlam ambalajlı, tarihi geçmemiş ve pişirmeden yenebilen gıda seç.";
                case "Flashlight": return "Elektrik kesilirse yolu aydınlatır. Pil yönünü ve ışığını deneyerek kontrol et.";
                case "Radio": return "Telefon bağlantısı kesilse de resmî duyuruları dinlemeye yardım eder.";
                case "FirstAid": return "Yaralanmada yetişkine haber ver; ilk yardım malzemesi kolay ulaşılır yerde dursun.";
                case "Blanket": return "Hafif, katlanmış battaniye ısı kaybını azaltmaya yardım eder.";
                case "FamilyCard": return "Buluşma yerini ve ortak iletişim kişisini ailece belirleyip koruyucu kılıfa koy.";
                case "Whistle": return "Sesini duyurmaya yardım eder; kolay ulaşılır yerde olsun.";
                default: return "Küçük bir tanıdık eşya rahatlatabilir; temel ihtiyaçlardan sonra yer ayır.";
            }
        }
        static void AuthorReadableFeedback()
        {
            // Preserve full explanations for adults. The existing child presentation keeps
            // captions of at most four words; longer unvoiced feedback becomes generic.
            var cues = new Dictionary<string, (string caption, string voice)>();
            void Cue(string full, string brief, string voice = null) => cues.Add(full, (brief, voice));
            const string packVoice = "Ada: Eşyayı seçip çevirelim. Boş bölmelere yerleştirelim.";
            const string batteryVoice = "Efe: Kapağı aç. Pillerin yönünü incele.";
            const string radioVoice = "Ada: Denediğimiz radyo yanımızda. Resmî duyuruyu birlikte dinleyelim.";
            foreach (var entry in new[]{
                ("Water", "Su: herkese yeterli olmalı.", "Su"), ("Food", "Gıda: ambalajı kontrol et.", "Gıda"),
                ("Flashlight", "Fener: pil yönünü dene.", "Fener"), ("Radio", "Radyo: resmî haberleri dinle.", "Radyo"),
                ("FirstAid", "İlk yardım: yetişkine danış.", "İlk yardım"), ("Blanket", "Battaniye: sıcak kalmaya yardım.", "Battaniye"),
                ("FamilyCard", "Kart: buluşma yerini hatırlatır.", "Aile kartı"), ("Whistle", "Düdük: sesini duyur.", "Düdük"),
                ("ComfortFox", "Oyuncak: ihtiyaçlardan sonra.", "Oyuncak")})
            {
                string voice = entry.Item1 == "Flashlight" ? batteryVoice : entry.Item1 == "Radio" ? radioVoice : null;
                Cue(ItemLabel(entry.Item1) + ": " + Purpose(entry.Item1), entry.Item2, voice);
                Cue("Derya: Önce " + ItemLabel(entry.Item1) + " çantaya yerleştir. " + Purpose(entry.Item1), entry.Item3 + " eksik. Yerleştirelim.", packVoice);
            }
            Cue("Derya: Feneri çantaya koymadan önce pilleriyle çalıştırıp ışığını deneyelim.", "Önce feneri çalıştır.", batteryVoice);
            Cue("Derya: Radyoyu çantaya koymadan önce düğmesini çevirip net yayını bulalım.", "Önce net yayını bul.", radioVoice);
            Cue("Derya: Ekipmanı çalıştırıp su ve gıdanın ambalajını kontrol edelim.", "Ekipmanı ve ambalajı denetle.");
            Cue("Derya: Kapak açılmış ve tarih geçmiş. Bunu çantaya koymayalım; kapalı şişeyi incele.", "Kapak açık. Yeniden seç.", "Derya: Kapağı ve tarihi kontrol et. Kapalı şişeyi seçelim.");
            Cue("Derya: Paket yırtık ve tarih geçmiş. Bunu çantaya koymayalım; sağlam paketi incele.", "Paket yırtık. Yeniden seç.", "Derya: Paketi ve tarihi kontrol et. Yırtık paketi kullanmayalım.");
            Cue("Derya: Önce ambalajı çevir. Kapağı veya paketi ve üzerindeki tarihi kontrol et.", "Önce ambalajı çevir.", "Derya: Ambalajı çevir. Tarihi ve açık olup olmadığını karşılaştır.");
            foreach (var machine in All<ScriptMachine>().Where(m => m.graph != null && !m.graph.variables.IsDefined("PreparationFeedbackV1")).ToArray())
            {
                var setters = machine.graph.units.OfType<SetMember>().Where(s => Constant(s.input) is string text && cues.ContainsKey(text)).ToArray();
                if (setters.Length == 0) continue;
                var g = new YanYanaGraphAuthor(machine);
                foreach (var setter in setters)
                {
                    string full = (string)Constant(setter.input); var cue = cues[full];
                    if (cue.caption.Length > 32 || cue.caption.Split(' ').Length > 4) throw new InvalidOperationException("Child feedback must remain visible: " + cue.caption);
                    foreach (var connection in g.Graph.valueConnections.Where(c => c.destination == setter.input).ToArray()) g.Graph.valueConnections.Remove(connection);
                    var select = g.Add(new SelectUnit()); g.Bind(select.condition, g.Get(typeof(Deprem.Accessibility.ReadingFree3D), "Enabled", null));
                    g.Bind(select.ifTrue, cue.caption); g.Bind(select.ifFalse, full); g.Bind(setter.input, select.selection);
                    if (cue.voice != null)
                    {
                        var next = g.Graph.controlConnections.Where(c => c.source == setter.assigned).ToArray();
                        foreach (var connection in next) g.Graph.controlConnections.Remove(connection);
                        var p = g.Send(setter.assigned, Flow, "SpeakPhysicalLine", cue.voice);
                        foreach (var connection in next) g.Link(p, connection.destination);
                    }
                }
                g.Graph.variables.Set("PreparationFeedbackV1", true); g.Dirty();
            }
            foreach (var label in All<TextMeshPro>().Where(t => t.name == "Pişirmeden yenebilir gıda")) label.name = "KKTC_Pişirmeden yenebilir gıda";
        }
        [MenuItem("Tools/Yan Yana/QA/Preparation Geometry")]
        public static void Inspect()
        {
            Directory.CreateDirectory(Evidence);
            var lines = new List<string>{"name\tactive\tposition\tmin\tmax\tsize"};
            foreach (var t in All<Transform>().Where(t => t.name.StartsWith("Odada bulunacak") || t.name.StartsWith("Yerleşim ·") || t.name.StartsWith("Anchor_") || t.name.StartsWith("İncelenen ") || t.name == "Düzenleme bezi" || t.name == "Çanta içi" || t.name == "Aile çantası" || t.name == "Pilli fener" || t.name == "Ayarlanabilir radyo"))
            {
                var b = BoundsOf(t);
                lines.Add($"{t.name}\t{t.gameObject.activeSelf}\t{t.position:F4}\t{b.min:F4}\t{b.max:F4}\t{b.size:F4}");
            }
            File.WriteAllLines(Evidence + "/geometry.tsv", lines);
            lines = new List<string>{"mesh\tmin\tmax\tsize"};
            foreach (var r in All<MeshRenderer>().Where(r => r.transform.root.name.StartsWith("02 Dünya") && r.bounds.center.z > -7 && r.bounds.center.z < 7 && r.bounds.center.y < 2))
                lines.Add($"{PathOf(r.transform)}\t{r.bounds.min:F4}\t{r.bounds.max:F4}\t{r.bounds.size:F4}");
            File.WriteAllLines(Evidence + "/surfaces.tsv", lines);
            ValidateSupports();
            Debug.Log("Preparation geometry exported to " + Evidence);
        }
        static void ValidateSupports()
        {
            var lines = new List<string>{"Authored visible mesh contact in Edit mode; metres. Maximum gap 2 mm; maximum penetration 1 mm."};
            var failures = new List<string>();
            void Contact(Transform item, Transform support)
            {
                float gap = BoundsOf(item).min.y - BoundsOf(support).max.y;
                bool pass = gap >= -.0011f && gap <= .0021f;
                string result = (pass ? "PASS " : "FAIL ") + item.name + " on " + support.name + " gap=" + gap.ToString("F5", System.Globalization.CultureInfo.InvariantCulture);
                lines.Add(result); if (!pass) failures.Add(result);
            }
            var bench = Find("PackingBench_Pad"); var counter = Find("Workbench_Top");
            foreach (string key in new[]{"Water", "Food"}) Contact(Find("Odada bulunacak · " + key), counter);
            Contact(Find("Odada bulunacak · FirstAid"), Find("ShelfBoard.002"));
            Contact(Find("Odada bulunacak · Blanket"), Find("Sofa_Seat.001"));
            Contact(Find("Odada bulunacak · ComfortFox"), Find("SafeTable_Top"));
            foreach (string key in new[]{"FamilyCard", "Whistle"}) Contact(Find("Odada bulunacak · " + key), bench);
            Contact(Find("Düzenleme bezi"), bench); Contact(Find("Çantanın alt dolgusu"), bench);
            Contact(Find("Çantanın kapanan dış yüzü"), bench);
            foreach (string name in new[]{"AA pil 1", "AA pil 2", "Radyonun taşıyıcı gövdesi", "Işığı görme kartı"}) Contact(Find(name), counter);
            Contact(FlashlightPart("Lamba başlığı"), counter);
            Contact(Find("Kaydırılabilir pil kapağı"), FlashlightPart("Gövde kenarı"));
            Contact(Find("Gerçek açma anahtarı"), FlashlightPart("Lamba başlığı"));
            foreach (var item in All<Transform>().Where(t => t.name.StartsWith("Yerleşim ·")))
            {
                var pad = item.Find("Yumuşak eşya altlığı");
                Contact(pad, Find("Düzenleme bezi")); Contact(item.Cast<Transform>().First(t => t != pad), pad);
            }
            foreach (string key in new[]{"Water", "Food"})
            {
                var root = Find("Ambalaj incelemesi · " + key); var mat = root.Find("Temiz karşılaştırma yüzeyi"); Contact(mat, counter);
                foreach (var item in root.Cast<Transform>().Where(t => t.name.StartsWith("İncelenen "))) Contact(item, mat);
            }
            File.WriteAllLines(Evidence + "/support-checks.txt", lines);
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n", failures));
        }
        [MenuItem("Tools/Yan Yana/QA/Preparation Before Views")]
        public static void BeforeViews() => CaptureViews("before");
        public static void CaptureViews(string prefix)
        {
            Directory.CreateDirectory(Evidence);
            var camera = Camera.main;
            var position = camera.transform.position; var rotation = camera.transform.rotation;
            var orthographic = camera.orthographic; var size = camera.orthographicSize; var fov = camera.fieldOfView;
            var prior = camera.targetTexture; var active = RenderTexture.active;
            var target = RenderTexture.GetTemporary(1000, 1000, 24);
            var packed = All<Transform>().Where(t => t.name.StartsWith("Yerleşim ·")).ToArray();
            var states = packed.Select(t => t.gameObject.activeSelf).ToArray();
            try
            {
                foreach (var t in packed) t.gameObject.SetActive(true);
                foreach (string key in new[]{"bag", "flashlight", "radio", "map"})
                {
                    var view = All<CinemachineCamera>().First(c => c.name == "Yakından incele · " + key);
                    camera.transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
                    camera.orthographic = false; camera.fieldOfView = view.Lens.FieldOfView;
                    camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                    var texture = new Texture2D(1000, 1000, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0, 0, 1000, 1000), 0, 0); texture.Apply();
                    File.WriteAllBytes(Evidence + "/" + prefix + "-" + key + ".png", texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                for (int i = 0; i < packed.Length; i++) packed[i].gameObject.SetActive(states[i]);
                camera.transform.SetPositionAndRotation(position, rotation); camera.orthographic = orthographic;
                camera.orthographicSize = size; camera.fieldOfView = fov; camera.targetTexture = prior;
                RenderTexture.active = active; RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
