// Editor-only environment authoring. The player receives meshes, colliders and
// native presentation graphs, with no new runtime C# or mesh generation.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public static class YanYanaNeighborhoodDesign
    {
        const string Name = "KKTC · hacimli mahalle ve açık meydan";
        const string GraphName = "Mahalle tasarımı · yardım gölgeliklerinde açık yakın kadraj";
        const string Root = "Assets/YanYana/Art/NeighborhoodDesign/";
        const string Reports = "ClientExports/YanYana/Reports/";
        const string Screens = "ClientExports/YanYana/Screenshots/";
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static Transform world, scenery;
        static readonly List<Bounds> houses = new List<Bounds>();
        static T[] All<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        static string Vec(Vector3 v) => v.ToString("F3", CultureInfo.InvariantCulture);
        static void RequireEdit()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || SceneManager.GetActiveScene().path != YanYanaAdventureBuilder.ScenePath)
                throw new InvalidOperationException("Open the continuous Yan Yana adventure in Edit mode first.");
        }
        static bool Geometry(GameObject go, out Bounds bounds)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true).Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
            bounds = new Bounds(go.transform.position, Vector3.zero);
            if (renderers.Length == 0) return false;
            bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            return true;
        }
        [MenuItem("Tools/Yan Yana/Art/Inspect Neighborhood Design")]
        public static void Inspect()
        {
            Directory.CreateDirectory(Reports);
            var lines = new List<string> { "Neighborhood geometry and camera inspection", "Playing=" + EditorApplication.isPlaying };
            var root = All<Transform>().First(t => t.name.StartsWith("02 Dünya"));
            foreach (Transform t in root)
                if (t.name.Contains("Mahalle evi") || t.name.Contains("Facade") || t.name.Contains("AidStation") || t.name.Contains("cephesi") || t.name.Contains("çevresi") || t.name == Name)
                {
                    Geometry(t.gameObject, out var b);
                    lines.Add(t.name + " position=" + Vec(t.position) + " dimensions=" + Vec(b.size) + " active=" + t.gameObject.activeSelf);
                }
            foreach (var t in All<Transform>().Where(t => t.name == "Street_Aid" || t.name.StartsWith("Park ağacı") || t.name.StartsWith("Park bankı") || t.name.StartsWith("Çocuk parkı")))
            {
                Geometry(t.gameObject, out var b);
                lines.Add(t.name + " position=" + Vec(t.position) + " dimensions=" + Vec(b.size));
            }
            foreach (var c in All<CinemachineCamera>())
                lines.Add("CAM " + c.name + " position=" + Vec(c.transform.position) + " euler=" + Vec(c.transform.eulerAngles) + " fov=" + c.Lens.FieldOfView + " ortho=" + c.Lens.OrthographicSize);
            File.WriteAllLines(Reports + "neighborhood-design-inspection.txt", lines);
        }
        [MenuItem("Tools/Yan Yana/Art/Apply KKTC Neighborhood Design")]
        public static void ApplyAndSave()
        {
            Apply();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            RenderAfter();
            FrameScene();
            Debug.Log("KKTC NEIGHBORHOOD DESIGN SAVED — closed houses, measured landscape, clear navigation, no new player C#.");
        }
        public static void Apply()
        {
            RequireEdit();
            Directory.CreateDirectory(Root + "Meshes"); Directory.CreateDirectory(Root + "Materials"); Directory.CreateDirectory(Reports); Directory.CreateDirectory(Screens);
            AssetDatabase.Refresh(); materials.Clear(); houses.Clear();
            world = All<Transform>().First(t => t.name.StartsWith("02 Dünya"));
            var old = world.Find(Name); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach (var graph in All<ScriptMachine>().Where(m => m.graph.title == GraphName).ToArray()) UnityEngine.Object.DestroyImmediate(graph);
            scenery = Group(Name, world, Vector3.zero);
            Palette(); HideLegacyVisuals(); AuthorGround(); AuthorArchitecture(); RefinePark(); AuthorStreetTrees(); AuthorAidCanopies(); RefineTitleArchitecture(); RefineGameplayViews();
            RenderSettings.fogStartDistance = 55f; RenderSettings.fogEndDistance = 100f;
            Bake(); Validate();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        static Transform Group(string name, Transform parent, Vector3 at)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = at; return go.transform;
        }
        static Material Mat(string name, string hex)
        {
            string path = Root + "Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            UnityEngine.ColorUtility.TryParseHtmlString(hex, out var color); m.SetColor("_BaseColor", color); m.SetColor("_Color", color); m.SetFloat("_Smoothness", .18f); m.SetFloat("_Metallic", 0);
            EditorUtility.SetDirty(m); materials[name] = m; return m;
        }
        static void Palette()
        {
            foreach (var m in AssetDatabase.FindAssets("t:Material", new[] { "Assets/YanYana/Art/Materials" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>)) materials[m.name] = m;
            Mat("YY_limePlaster", "#E9DFC5"); Mat("YY_peachPlaster", "#DDB49B"); Mat("YY_paleOchre", "#DCCBA8");
            Mat("YY_roofClay", "#B87455"); Mat("YY_roofClayLight", "#C98766"); Mat("YY_roofClayDark", "#A7664C");
            Mat("ND_Paver", "#E0D4BB"); Mat("ND_PaverLight", "#E7DDC8"); Mat("ND_PaverWarm", "#D8CCB5");
            Mat("ND_StoneJoint", "#B5AC98"); Mat("ND_Limestone", "#EAE2CD"); Mat("ND_Grass", "#A8AF83"); Mat("ND_Soil", "#BDA988");
            Mat("ND_DarkLeaf", "#66806A"); Mat("ND_LightLeaf", "#8FA47A"); Mat("ND_Canvas", "#457F7D"); Mat("ND_CanvasLight", "#689A90");
            Mat("YY_oliveDark", "#64806A"); Mat("YY_oliveMid", "#7B916E"); Mat("YY_oliveLight", "#8FA47A"); Mat("YY_oliveBark", "#806347");
            Mat("ND_Sign", "#2D625A"); Mat("ND_SignWhite", "#FFF5DF");
        }
        static GameObject Shape(string name, Transform parent, Vector3 at, Vector3 size, string material, PrimitiveType type = PrimitiveType.Cube, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = at; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = materials[material];
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true; return go;
        }
        static void HideLegacyVisuals()
        {
            foreach (Transform t in world)
                if (t.name.StartsWith("Mahalle evi ") || t.name == "Sokağın güney cephesi") t.gameObject.SetActive(false);
            var surroundings = world.Find("Evin tamamlanmış yakın çevresi");
            if (surroundings) foreach (Transform t in surroundings) if (t.name.StartsWith("Facade_")) t.gameObject.SetActive(false);
            var ground = world.Find("NeighborhoodGround");
            if (ground) foreach (var r in ground.GetComponentsInChildren<Renderer>(true))
                if (new[] { "MahalleToprakTabani", "MahalleZemini", "AnaYol", "YanYol", "Baglanti", "ToplanmaZemini", "Agac", "YuvarlakTac", "AlanIsareti", "ResimliTabela" }.Any(n => r.name.StartsWith(n))) r.enabled = false;
            var park = world.Find("Mahalle Parkı · buluşma ve dayanışma");
            if (park) foreach (Transform t in park)
                if (new[] { "Park ve arka mahalle peyzajı", "Arka mahallenin yaya yolu", "Parkın yeşil tabanı", "Parkın açık buluşma meydanı", "COLLIDER_Baglanti_ParkGezinti" }.Contains(t.name))
                    foreach (var r in t.GetComponentsInChildren<Renderer>()) r.enabled = false;
        }
        static void AuthorGround()
        {
            // The collision plane joins the existing authored stair and story floor volumes.
            Shape("Mahallenin devam eden toprağı", scenery, new Vector3(0, -.72f, -13), new Vector3(110, .40f, 108), "ND_Grass", collider: true);
            // Align paving with the existing -.48 m walking plane, below fire decals.
            Paving("Meydan · taş döşeme", new Vector3(0, -.478f, -25.1f), 26, 10.2f, .90f, .75f);
            Paving("Sokağın ana yaya yolu", new Vector3(-4, -.478f, -14.5f), 4.8f, 11.0f, .90f, .75f);
            Paving("Sokağın doğu yaya yolu", new Vector3(6, -.478f, -14.75f), 3.6f, 10.5f, .90f, .75f);
            Paving("Meydana açık giriş yolu", new Vector3(2, -.478f, -14.75f), 4.4f, 10.5f, .90f, .75f);
            Paving("Parkın arkasındaki gezinti yolu", new Vector3(0, -.478f, -37.4f), 39, 2.6f, .90f, .75f);
            foreach (float side in new[] { -1f, 1f })
            {
                Paving("Mahalle evlerinin ön kaldırımı " + side, new Vector3(side * 15.25f, -.478f, -22.5f), 2.5f, 27.2f, .85f, .75f);
                Shape("Yaya yolunun alçak bordürü", scenery, new Vector3(side * 16.59f, -.42f, -23.4f), new Vector3(.14f, .13f, 29), "ND_Limestone");
                Shape("Ağaçlar için ayrılan yeşil şerit", scenery, new Vector3(side * 13.4f, -.486f, -27.4f), new Vector3(2.4f, .035f, 18), "ND_Grass");
            }
            Paving("Oyun alanının çevresindeki yürüyüş bağı", new Vector3(2.9f, -.478f, -33.15f), 2.6f, 5.9f, .90f, .75f);
            Paving("Parkın arka yatay bağlantısı", new Vector3(-4.35f, -.478f, -35.6f), 11.9f, 1.0f, .90f, .75f);
            Shape("Parkın alçak yeşil bahçesi", scenery, new Vector3(0, -.499f, -33.4f), new Vector3(27.8f, .028f, 6.5f), "ND_Grass");
        }
        static void Paving(string name, Vector3 at, float width, float depth, float tileX, float tileZ)
        {
            Shape(name + " · derz yatağı", scenery, at - Vector3.up * .013f, new Vector3(width, .021f, depth), "ND_StoneJoint");
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new[] { new List<int>(), new List<int>(), new List<int>() };
            int columns = Mathf.CeilToInt(width / tileX), rows = Mathf.CeilToInt(depth / tileZ);
            float dx = width / columns, dz = depth / rows;
            for (int z = 0; z < rows; z++) for (int x = 0; x < columns; x++)
            {
                float left = -width / 2 + x * dx + .009f, right = left + dx - .018f;
                float near = -depth / 2 + z * dz + .009f, far = near + dz - .018f;
                int i = vertices.Count; vertices.AddRange(new[] { new Vector3(left, 0, near), new Vector3(left, 0, far), new Vector3(right, 0, far), new Vector3(right, 0, near) });
                uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
                var t = tris[(x * 13 + z * 7) % 9 < 6 ? 0 : (x + z) % 2 + 1]; t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.subMeshCount = 3;
            for (int i = 0; i < 3; i++) mesh.SetTriangles(tris[i], i); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string file = Root + "Meshes/" + name + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(file);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); } else AssetDatabase.CreateAsset(mesh, file);
            var go = Group(name, scenery, at).gameObject; go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = new[] { materials["ND_Paver"], materials["ND_PaverLight"], materials["ND_PaverWarm"] }; r.shadowCastingMode = ShadowCastingMode.Off; go.isStatic = true;
        }
        static void AuthorArchitecture()
        {
            int index = 0;
            foreach (float x in new[] { -18f, -9f, 0f, 9f, 18f }) House(++index, new Vector3(x, -.48f, -41.8f), 0, 1f);
            foreach (float side in new[] { -1f, 1f }) foreach (float z in new[] { -12.0f, -22.0f, -32.0f }) House(++index, new Vector3(side * 18.2f, -.48f, z), side < 0 ? 90 : 270, 1f);
            foreach (float x in new[] { -8f, 0f, 8f }) House(++index, new Vector3(x, -.48f, 12.9f), 180, .94f);
        }
        static void House(int index, Vector3 at, float yaw, float scale)
        {
            string path = "Assets/YanYana/Art/Models/KKTC_House_" + ((index - 1) % 4 + 1) + ".fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (!prefab) throw new FileNotFoundException("Author/import the complete KKTC house models first", path);
            var parcel = Group("Konut parseli " + index.ToString("00") + " · bahçe ve tam bina", scenery, at); parcel.localRotation = Quaternion.Euler(0, yaw, 0);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parcel); go.name = "Hacimli KKTC evi " + index.ToString("00"); go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one * scale;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.sharedMaterials = r.sharedMaterials.Select(m => m && materials.TryGetValue(m.name, out var replacement) ? replacement : m).ToArray(); r.gameObject.isStatic = true; }
            // A box encloses the actual shell, not the balcony/doorstep setback.
            var hit = parcel.gameObject.AddComponent<BoxCollider>(); hit.center = new Vector3(0, 2.65f * scale, -3 * scale); hit.size = new Vector3(6.2f, 5.3f, 6.0f) * scale;
            var footprint = new Bounds(parcel.TransformPoint(hit.center), new Vector3(yaw == 90 || yaw == 270 ? hit.size.z : hit.size.x, hit.size.y, yaw == 90 || yaw == 270 ? hit.size.x : hit.size.z)); houses.Add(footprint);
            Shape("Evin taş giriş önü", parcel, new Vector3(0, .019f, .93f), new Vector3(6.5f * scale, .036f, 1.70f), "ND_PaverWarm");
            // Low garden walls frame each home without filling public footpaths.
            foreach (float x in new[] { -3.3f, 3.3f })
            {
                Shape("Bahçe yan duvarı", parcel, new Vector3(x * scale, .27f, -.1f), new Vector3(.16f, .54f, 2.4f), "ND_Limestone");
                Shape("Taş duvar kapağı", parcel, new Vector3(x * scale, .56f, -.1f), new Vector3(.22f, .06f, 2.45f), "ND_PaverLight");
                Shape("Giriş bitkisi · toprak saksı", parcel, new Vector3(x * .77f * scale, .17f, .44f), new Vector3(.42f, .17f, .42f), "YY_terracotta", PrimitiveType.Cylinder);
                Shape("Giriş bitkisi · yeşil taç", parcel, new Vector3(x * .77f * scale, .55f, .44f), new Vector3(.58f, .65f, .58f), "ND_DarkLeaf", PrimitiveType.Sphere);
            }
        }
        static void RefinePark()
        {
            var park = world.Find("Mahalle Parkı · buluşma ve dayanışma"); if (!park) return;
            foreach (var r in park.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = r.sharedMaterials.Select(m => !m ? m : m.name == "ParkPath" ? materials["ND_Paver"] : m.name == "ParkBorder" ? materials["ND_Limestone"] : m.name.StartsWith("ParkGrass") ? materials["ND_Grass"] : m.name == "ParkLeaf" ? materials["ND_DarkLeaf"] : m.name == "ParkLeafLight" ? materials["ND_LightLeaf"] : m).ToArray();
            foreach (Transform t in park)
            {
                if (t.name == "Buluşma meydanı taş çember") { var at = t.position; at.y = -.481f; t.position = at; var size = t.localScale; size.y = .004f; t.localScale = size; }
                if (t.name == "Açık buluşma merkezi") { var at = t.position; at.y = -.476f; t.position = at; var size = t.localScale; size.y = .002f; t.localScale = size; }
            }
            var trees = park.Cast<Transform>().Where(t => t.name.StartsWith("Park ağacı")).OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToArray();
            int treeIndex = 0;
            foreach (var tree in trees) { tree.gameObject.SetActive(false); Tree("Park ağacı · ayrı yeşil cep " + (++treeIndex), tree.position, treeIndex % 2 == 0); }
            var play = park.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Çocuk parkı"));
            if (play) play.position = new Vector3(-3.2f, -.48f, -32.4f);
            int bed = 0;
            foreach (var t in park.Cast<Transform>().Where(t => t.name == "Çiçekli park adası"))
            {
                t.localScale = new Vector3(.76f, .64f, .78f);
                var positions = new[] { new Vector3(-12.9f, -.48f, -25.1f), new Vector3(12.9f, -.48f, -27.4f), new Vector3(-8.5f, -.48f, -32.1f), new Vector3(8.0f, -.48f, -32.1f) };
                if (bed < positions.Length) t.position = positions[bed++];
            }
            foreach (var t in park.Cast<Transform>().Where(t => t.name.EndsWith("· park tabelası")))
            {
                // The existing solid boards already have two correctly facing texts.
                if (t.name.StartsWith("MAHALLE PARKI")) t.position = new Vector3(-.9f, -.48f, -35.3f);
                if (t.name.StartsWith("SU VE DİNLENME")) t.position = new Vector3(11.2f, -.48f, -30.3f);
            }
        }
        static void RefineTitleArchitecture()
        {
            var title = All<Transform>().FirstOrDefault(t => t.name == "Açılış · mahallede bir öğleden sonra"); if (!title) return;
            foreach (Transform t in title.Cast<Transform>().Where(t => t.name.StartsWith("AD · KKTC_House_")).ToArray()) UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach (Transform original in title.Cast<Transform>().Where(t => t.name.StartsWith("AD · Facade_")).ToArray())
            {
                string number = original.name.Substring(original.name.Length - 1);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/YanYana/Art/Models/KKTC_House_" + number + ".fbx"); if (!prefab) continue;
                var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, title); model.name = "AD · KKTC_House_" + number;
                model.transform.localPosition = original.localPosition; model.transform.localRotation = original.localRotation; model.transform.localScale = original.localScale;
                if (Mathf.Abs(original.localPosition.x) > 1) { var at = model.transform.localPosition; at.x = Mathf.Sign(at.x) * 2.8f; model.transform.localPosition = at; }
                foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(m => m && materials.TryGetValue(m.name, out var shared) ? shared : m).ToArray();
                original.gameObject.SetActive(false);
            }
        }
        static void AuthorStreetTrees()
        {
            foreach (var at in new[] { new Vector3(-10.4f, -.48f, -10.1f), new Vector3(11.6f, -.48f, -11.0f), new Vector3(-11.8f, -.48f, -17.0f), new Vector3(12.7f, -.48f, -17.3f) })
            {
                var tree = Tree("Sokak ağacı · yapıdan ayrı yeşil cep", at, at.x > 0);
                Shape("Alçak taş ağaç yatağı", tree, new Vector3(0, .02f, 0), new Vector3(1.65f, .08f, 1.65f), "ND_Limestone");
                Shape("Açık toprak", tree, new Vector3(0, .069f, 0), new Vector3(1.46f, .013f, 1.46f), "ND_Soil");
            }
        }
        static Transform Tree(string name, Vector3 at, bool carob)
        {
            var root = Group(name, scenery, at);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/YanYana/Art/Models/KKTC_" + (carob ? "CarobTree" : "OliveTree") + ".fbx");
            if (!prefab) throw new InvalidOperationException("Import the authored full-volume trees first.");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root); model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.Euler(0, at.x * 17 + at.z * 5, 0);
            foreach (var r in model.GetComponentsInChildren<Renderer>()) { r.sharedMaterials = r.sharedMaterials.Select(m => m && materials.TryGetValue(m.name, out var shared) ? shared : m).ToArray(); r.gameObject.isStatic = true; }
            var trunk = root.gameObject.AddComponent<CapsuleCollider>(); trunk.center = Vector3.up * 1.18f; trunk.height = 2.36f; trunk.radius = .16f;
            return root;
        }
        static void AuthorAidCanopies()
        {
            var owner = All<Transform>().First(t => t.name.StartsWith("01 Akış")).gameObject;
            var g = new YanYanaGraphAuthor(owner, GraphName); var update = g.Add(new Unity.VisualScripting.Update()); var p = update.trigger;
            var workspace = g.Var("Workspace", owner);
            var close = g.Binary<Or>(g.Binary<Equal>(workspace, "family"), g.Binary<Or>(g.Binary<Equal>(workspace, "aid"), g.Binary<Or>(g.Binary<Equal>(workspace, "broadcast"), g.Binary<Equal>(workspace, "relief"))));
            var roofsVisible = g.Binary<Equal>(close, false); var supportsVisible = g.Binary<Equal>(g.Binary<Equal>(workspace, "family"), false);
            var stations = world.Cast<Transform>().Where(t => t.name == "AidStation").OrderBy(t => t.position.x).ToArray();
            var font = All<TMP_Text>().First(t => t.font).font;
            for (int i = 0; i < stations.Length; i++)
            {
                var station = stations[i];
                foreach (var r in station.GetComponentsInChildren<Renderer>(true))
                    if (r.name.StartsWith("YumusakTente") || r.name.StartsWith("TenteDikisi") || r.name.StartsWith("KatlanirAyak") || r.name.StartsWith("AyakTabani")) r.gameObject.SetActive(false);
                // Retain the actual tables and every task/destination object reference.
                var root = Group("Yardım noktası " + i + " · yetişkin ölçüsünde gölgelik", scenery, station.position);
                foreach (float x in new[] { -1.13f, 1.13f }) foreach (float z in new[] { -.73f, .73f })
                {
                    var post = Shape("Gölgelik direği", root, new Vector3(x, 1.20f, z), new Vector3(.064f, 2.4f, .064f), "ND_Limestone");
                    p = g.Set(p, typeof(Renderer), "enabled", post.GetComponent<Renderer>(), supportsVisible);
                    Shape("Zemine oturan direk tabanı", root, new Vector3(x, .045f, z), new Vector3(.20f, .09f, .20f), "ND_Sign");
                }
                var roof = CanopyMesh("Tente · gerçek kumaş kalınlığı " + i, root);
                p = g.Set(p, typeof(Renderer), "enabled", roof, roofsVisible);
                foreach (float z in new[] { -.85f, .85f })
                {
                    var valance = Shape("Tentenin kumaş etekliği", root, new Vector3(0, 2.39f, z), new Vector3(2.72f, .16f, .043f), "ND_Canvas"); p = g.Set(p, typeof(Renderer), "enabled", valance.GetComponent<Renderer>(), roofsVisible);
                }
                string title = new[] { "SU VE DİNLENME", "SAĞLIK EKİBİ", "AİLE BULUŞMA" }[i];
                foreach (var t in world.Cast<Transform>().Where(t => t.name == title)) t.gameObject.SetActive(false);
                var board = Shape("Yardım başlığı · çift yüzlü levha", root, new Vector3(0, 2.03f, .87f), new Vector3(1.92f, .30f, .075f), "ND_Sign"); p = g.Set(p, typeof(Renderer), "enabled", board.GetComponent<Renderer>(), supportsVisible);
                foreach (int side in new[] { -1, 1 })
                {
                    var label = Group("Okunur yardım başlığı " + side, root, new Vector3(0, 2.03f, .87f + side * .041f)).gameObject.AddComponent<TextMeshPro>();
                    label.font = font; label.text = title; label.fontSize = 2.1f; label.enableAutoSizing = true; label.fontSizeMin = 1.4f; label.fontSizeMax = 2.1f; label.alignment = TextAlignmentOptions.Center; label.color = materials["ND_SignWhite"].color; label.rectTransform.sizeDelta = new Vector2(1.85f, .27f); label.transform.localRotation = Quaternion.Euler(0, side < 0 ? 0 : 180, 0);
                    p = g.Set(p, typeof(Renderer), "enabled", label.GetComponent<Renderer>(), supportsVisible);
                }
            }
            foreach (Transform t in world) if (t.name.StartsWith("İhtiyaç simgesi ")) t.localScale = Vector3.one * 1.0f;
            foreach (var t in All<Transform>().Where(t => t.name.StartsWith("Taşınan ihtiyaç işareti "))) t.localScale = Vector3.one * .9f;
            g.Dirty();
        }
        static void RefineGameplayViews()
        {
            var aid = All<CinemachineCamera>().FirstOrDefault(c => c.name == "Yakından incele · aid");
            if (aid) { aid.transform.position = new Vector3(3, 8.9f, -8.2f); aid.transform.LookAt(new Vector3(3, .40f, -23.2f)); }
            var relief = All<CinemachineCamera>().FirstOrDefault(c => c.name == "Yakından incele · relief");
            if (relief)
            {
                var focus = All<Transform>().First(t => t.name == "Street_Aid").position + new Vector3(.25f, .12f, 2.6f);
                relief.transform.position = focus + new Vector3(-2.6f, 4.3f, -1.2f); relief.transform.LookAt(focus);
            }
        }
        static Renderer CanopyMesh(string name, Transform parent)
        {
            var vertices = new List<Vector3>(); var faces = new List<int>();
            Vector3[] points = { new Vector3(-1.4f, 2.47f, -.87f), new Vector3(1.4f, 2.47f, -.87f), new Vector3(1.4f, 2.47f, .87f), new Vector3(-1.4f, 2.47f, .87f), new Vector3(0, 3.02f, 0) };
            for (int side = 0; side < 4; side++)
            {
                int n = vertices.Count; var a = points[side]; var b = points[(side + 1) % 4]; var c = points[4];
                vertices.AddRange(new[] { a, c, b, a - Vector3.up * .025f, b - Vector3.up * .025f, c - Vector3.up * .025f });
                faces.AddRange(new[] { n, n + 1, n + 2, n + 3, n + 4, n + 5 });
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(faces, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path = Root + "Meshes/" + name + ".asset"; var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old) { EditorUtility.CopySerialized(mesh, old); UnityEngine.Object.DestroyImmediate(mesh); mesh = old; EditorUtility.SetDirty(old); } else AssetDatabase.CreateAsset(mesh, path);
            var go = Group(name, parent, Vector3.zero).gameObject; go.AddComponent<MeshFilter>().sharedMesh = mesh; go.isStatic = true;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = materials["ND_Canvas"]; return renderer;
        }
        static void Bake()
        {
            var surface = world.GetComponent<NavMeshSurface>(); if (!surface) throw new InvalidOperationException("The original navigation surface is missing.");
            var previous = surface.navMeshData; surface.BuildNavMesh(); var result = surface.navMeshData;
            if (!result) throw new InvalidOperationException("Neighborhood navigation did not bake.");
            if (previous && AssetDatabase.Contains(previous)) { surface.RemoveData(); EditorUtility.CopySerialized(result, previous); surface.navMeshData = previous; EditorUtility.SetDirty(previous); surface.AddData(); UnityEngine.Object.DestroyImmediate(result); }
            else AssetDatabase.CreateAsset(result, "Assets/YanYana/Scenes/PhysicalAdventureNavigation.asset");
        }
        [MenuItem("Tools/Yan Yana/QA/Validate KKTC Neighborhood Design")]
        public static void Validate()
        {
            Directory.CreateDirectory(Reports); var lines = new List<string>(); bool passed = true;
            void Check(bool ok, string label) { lines.Add((ok ? "PASS " : "FAIL ") + label); passed &= ok; }
            var environment = All<Transform>().FirstOrDefault(t => t.name == Name); Check(environment, "Authored environment root present");
            if (!environment) throw new InvalidOperationException("Apply the neighborhood first.");
            var buildings = environment.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Hacimli KKTC evi ")).ToArray();
            Check(buildings.Length == 14, "14 closed, full-depth homes");
            var bodies = environment.GetComponentsInChildren<BoxCollider>().Where(c => c.name.StartsWith("Konut parseli")).ToArray();
            foreach (var house in buildings)
            {
                Geometry(house.gameObject, out var bounds); Check(bounds.size.y > 5.8f && Mathf.Min(bounds.size.x, bounds.size.z) > 5.4f, house.name + " roof/height/depth " + Vec(bounds.size));
            }
            foreach (var tree in All<Transform>().Where(t => t.name.StartsWith("Park ağacı") || t.name.StartsWith("Sokak ağacı ·")))
            {
                Geometry(tree.gameObject, out var crown); var flat = new Bounds(new Vector3(crown.center.x, 1, crown.center.z), new Vector3(crown.size.x + 1.2f, 2, crown.size.z + 1.2f));
                Check(!bodies.Any(c => { var b = c.bounds; b.center = new Vector3(b.center.x, 1, b.center.z); b.size = new Vector3(b.size.x, 2, b.size.z); return b.Intersects(flat); }), "Tree canopy/setback separate from homes: " + Vec(tree.position));
            }
            var start = new Vector3(3, -.48f, -21);
            foreach (var target in new[] { new Vector3(-3.1f, -.48f, -7.5f), new Vector3(-5.8f, -.48f, -11.5f), new Vector3(-4, -.48f, -14.2f), new Vector3(0, -.48f, -22.2f), new Vector3(3, -.48f, -23.65f), new Vector3(6, -.48f, -22.2f), new Vector3(-7, -.48f, -24.9f), new Vector3(8, -.48f, -23.9f), new Vector3(-8.2f, -.48f, -23.4f), new Vector3(-9.4f, -.48f, -21.5f), new Vector3(0, -.48f, -29), new Vector3(3, -.48f, -35) })
            {
                var path = new NavMeshPath(); bool a = NavMesh.SamplePosition(start, out var from, .7f, NavMesh.AllAreas), b = NavMesh.SamplePosition(target, out var to, .7f, NavMesh.AllAreas);
                Check(a && b && NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "Complete story/park path " + Vec(target));
            }
            Check(!All<MonoBehaviour>().Any(m => m && m.GetType().Namespace == "YanYana.Editor"), "No Editor component or new runtime C# in the scene");
            File.WriteAllLines(Reports + "neighborhood-design-validation.txt", lines);
            if (!passed) throw new InvalidOperationException("Neighborhood geometry/navigation check failed; see neighborhood-design-validation.txt");
        }
        [MenuItem("Tools/Yan Yana/Art/Render Neighborhood Before Design")]
        public static void RenderBefore() => RenderSet("before");
        [MenuItem("Tools/Yan Yana/Art/Render KKTC Neighborhood Design")]
        public static void RenderAfter() => RenderSet("kktc");
        [MenuItem("Tools/Yan Yana/Art/Show KKTC Neighborhood")]
        public static void FrameScene()
        {
            if (EditorApplication.isPlaying) return;
            Selection.activeGameObject = null;
            var view = SceneView.lastActiveSceneView; if (!view) return;
            view.orthographic = false;
            view.LookAt(new Vector3(-1, .8f, -28), Quaternion.LookRotation(new Vector3(21, -12.5f, -19)), 22f, false, true);
            view.Repaint();
        }
        static void RenderSet(string prefix)
        {
            Directory.CreateDirectory(Screens);
            Render(prefix + "-neighborhood-overview", new Vector3(-22, 13, -9), new Vector3(-1, .5f, -28), 1600, 1000, 51);
            Render(prefix + "-park-eye-level", new Vector3(-12, 3.1f, -19), new Vector3(2, .85f, -29), 1500, 1000, 57);
            Render(prefix + "-houses-three-dimensions", new Vector3(-10, 4.0f, -36.8f), new Vector3(-14, 2.2f, -43.8f), 1300, 1000, 59);
            foreach (string key in new[] { "aid", "reunion1", "reunion2", "reunion4" })
            {
                var c = All<CinemachineCamera>().FirstOrDefault(v => v.name == "Yakından incele · " + key); if (!c) continue;
                Render(prefix + "-game-camera-" + key, c.transform.position, c.transform.position + c.transform.forward * 10, 540, 960, c.Lens.FieldOfView);
            }
        }
        static void Render(string name, Vector3 eye, Vector3 focus, int width, int height, float fov)
        {
            var go = new GameObject("Temporary neighborhood design review camera"); var cam = go.AddComponent<Camera>();
            cam.transform.position = eye; cam.transform.LookAt(focus); cam.fieldOfView = fov; cam.nearClipPlane = .06f; cam.farClipPlane = 130; cam.aspect = width / (float)height;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor; cam.enabled = false;
            var data = cam.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; data.antialiasingQuality = AntialiasingQuality.High;
            var texture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var active = RenderTexture.active; Texture2D image = null;
            try { cam.targetTexture = texture; cam.Render(); RenderTexture.active = texture; image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(Screens + name + ".png", image.EncodeToPNG()); }
            finally { cam.targetTexture = null; RenderTexture.active = active; RenderTexture.ReleaseTemporary(texture); if (image) UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
