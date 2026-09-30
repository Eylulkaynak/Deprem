using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Unity.Cinemachine;
using Deprem.Story;

namespace YanYana.Editor
{
    public static partial class YanYanaAdventureBuilder
    {
        static void CreateWorld()
        {
            var home = Group("Ada ve Efe’nin evi", world.transform);
            Shape("Ev zemini", PrimitiveType.Cube, home.transform, new Vector3(0, -.16f, 0), new Vector3(17, .3f, 15), mats["Floor"], true);
            for (int i = -8; i < 9; i++) Shape("Ahşap zemin derzi", PrimitiveType.Cube, home.transform, new Vector3(i, .004f, 0), new Vector3(.018f, .01f, 15), mats["YY_sand"]);
            Shape("Arka duvar", PrimitiveType.Cube, home.transform, new Vector3(0, 1.65f, 7.5f), new Vector3(17, 3.6f, .25f), mats["Plaster"], true);
            Shape("Yan duvar", PrimitiveType.Cube, home.transform, new Vector3(-8.5f, 1.65f, 2), new Vector3(.25f, 3.6f, 11), mats["Plaster"], true);
            Shape("Turkuaz süpürgelik", PrimitiveType.Cube, home.transform, new Vector3(0, .15f, 7.25f), new Vector3(17, .3f, .1f), mats["YY_teal"]);
            for (int i = 0; i < 3; i++)
            {
                float x = -5 + i * 5;
                Shape("Pencere çerçevesi", PrimitiveType.Cube, home.transform, new Vector3(x, 2.2f, 7.25f), new Vector3(2.7f, 1.9f, .17f), mats["YY_wood"]);
                Shape("Gökyüzü camı", PrimitiveType.Cube, home.transform, new Vector3(x, 2.2f, 7.13f), new Vector3(2.5f, 1.7f, .1f), mats["YY_glass"]);
                Shape("Pencere bölmesi", PrimitiveType.Cube, home.transform, new Vector3(x, 2.2f, 7.03f), new Vector3(.1f, 1.9f, .15f), mats["YY_white"]);
                Shape("Perde", PrimitiveType.Cube, home.transform, new Vector3(x - 1.45f, 2.25f, 6.95f), new Vector3(.48f, 2.4f, .18f), mats["YY_cream"]);
                Shape("Perde", PrimitiveType.Cube, home.transform, new Vector3(x + 1.45f, 2.25f, 6.95f), new Vector3(.48f, 2.4f, .18f), mats["YY_cream"]);
            }
            Shape("Dokuma kilim", PrimitiveType.Cube, home.transform, new Vector3(0, .015f, -.9f), new Vector3(5, .035f, 4.5f), mats["Rug"]);
            for (int i = 0; i < 6; i++) Shape("Kilim çizgisi", PrimitiveType.Cube, home.transform, new Vector3(-1.9f + i * .76f, .038f, -.9f), new Vector3(.06f, .006f, 4.3f), mats["YY_cream"]);
            var sofa = Group("Yuvarlak aile kanepesi", home.transform, new Vector3(-5, 0, 2));
            Shape("Oturma minderi", PrimitiveType.Capsule, sofa.transform, new Vector3(0, .52f, 0), new Vector3(1.3f, 1.5f, .7f), mats["YY_teal"]).transform.localRotation = Quaternion.Euler(0, 0, 90);
            Shape("Sırt", PrimitiveType.Capsule, sofa.transform, new Vector3(0, .92f, .42f), new Vector3(.65f, 1.6f, .85f), mats["YY_teal"]).transform.localRotation = Quaternion.Euler(0, 0, 90);
            Model("Cushion", sofa.transform, new Vector3(-.85f, .82f, -.06f), 1.4f, 20);
            Model("Cushion", sofa.transform, new Vector3(.8f, .82f, -.06f), 1.3f, -20);
            Model("Table", home.transform, new Vector3(0, 0, .3f), 1.05f);
            Model("Shelf", home.transform, new Vector3(-6.6f, 0, 5.7f), 1.2f);
            Model("Wardrobe", home.transform, new Vector3(5.5f, 0, 5.6f), 1.25f);
            Model("Plant", home.transform, new Vector3(-7, 0, -.5f), 2.3f);
            Model("Plant", home.transform, new Vector3(7, 0, 5.8f), 2.1f);
            Model("ToyBox", home.transform, new Vector3(-3.4f, 0, -.5f), 1.9f);
            Model("ComfortFox", home.transform, new Vector3(-3.4f, .42f, -.5f), 1.3f);
            var kitchen = Group("Mutfak köşesi", home.transform, new Vector3(6, 0, 2.5f));
            Shape("Mutfak dolabı", PrimitiveType.Cube, kitchen.transform, new Vector3(0, .52f, 0), new Vector3(3.1f, 1, 1), mats["YY_teal"]);
            Shape("Tezgâh", PrimitiveType.Cube, kitchen.transform, new Vector3(0, 1.05f, 0), new Vector3(3.3f, .1f, 1.1f), mats["YY_cream"]);
            for (int i = 0; i < 3; i++)
            {
                Shape("Dolap kapağı", PrimitiveType.Cube, kitchen.transform, new Vector3(-1 + i, .55f, -.52f), new Vector3(.94f, .84f, .04f), mats["YY_tealDark"]);
                Model("Water", kitchen.transform, new Vector3(-.8f + i * .7f, 1.1f, 0), 1.1f);
            }
            var door = Group("Ev çıkışı ve hazırlık köşesi", home.transform, new Vector3(4.5f, 0, -4.5f));
            Model("Bench", door.transform, new Vector3(1.3f, 0, 0), .9f);
            Model("BackpackClosed", door.transform, new Vector3(1.3f, .7f, 0), 1.2f);
            Model("ShoePair", door.transform, new Vector3(.9f, .02f, -1), 1.2f);
            exitBlock = Group("Bırakılmış hafif geçiş engelleri", door.transform, new Vector3(-1.5f, 0, 0));
            Model("ToyBox", exitBlock.transform, Vector3.zero, 1.7f); Model("ShoePair", exitBlock.transform, new Vector3(.9f, 0, .3f));
            shelfDamage = Group("Sabitlenmemiş rafın kapattığı alan", home.transform, new Vector3(-5.7f, .12f, 4));
            Model("Shelf", shelfDamage.transform, Vector3.zero, 1.2f).transform.localRotation = Quaternion.Euler(64, 25, 0);
            for (int i = 0; i < 6; i++) Model("Documents", shelfDamage.transform, new Vector3((i % 3) * .45f, .03f, i * .23f), .8f, i * 37);
            shelfDamage.SetActive(false);
            wardrobeDamage = Group("Sabitlenmemiş dolabın kapattığı alan", home.transform, new Vector3(4.2f, .15f, 3.7f));
            Model("Wardrobe", wardrobeDamage.transform, Vector3.zero, 1.2f).transform.localRotation = Quaternion.Euler(50, -30, 0); wardrobeDamage.SetActive(false);
            shelfFixed = Model("Bracket", home.transform, new Vector3(-6.6f, 2, 5.5f), 1.5f); shelfFixed.SetActive(false);
            wardrobeFixed = Model("SafetyStrap", home.transform, new Vector3(5.5f, 2.4f, 5.2f), 1.7f); wardrobeFixed.SetActive(false);
            darkRoot = Group("Erişilebilir acil aydınlatma", door.transform, new Vector3(0, 2.2f, .6f));
            Shape("Acil lamba gövdesi", PrimitiveType.Cube, darkRoot.transform, Vector3.zero, new Vector3(.7f, .26f, .2f), mats["Glow"]);
            torch = darkRoot.AddComponent<Light>(); torch.type = LightType.Point; torch.color = Hex("#FFDB99"); torch.intensity = 0; torch.range = 9;
            var corridor = Group("Apartman ve sahanlık", world.transform, new Vector3(0, 0, 21));
            Shape("Apartman zemini", PrimitiveType.Cube, corridor.transform, new Vector3(0, -.15f, 0), new Vector3(12, .3f, 24), mats["Floor"], true);
            Shape("Sahanlık duvarı", PrimitiveType.Cube, corridor.transform, new Vector3(-5.9f, 1.5f, 0), new Vector3(.2f, 3, 24), mats["Plaster"], true);
            for (int i = 0; i < 4; i++)
            {
                var frame = Shape("Komşu kapısı", PrimitiveType.Cube, corridor.transform, new Vector3(-5.74f, 1.1f, -8 + i * 5), new Vector3(.14f, 2.2f, 1.25f), mats["YY_tealDark"]);
                WorldText((i + 2).ToString(), frame.transform, new Vector3(1, .3f, 0), .3f, Hex("#FFE7A8"));
            }
            for (int i = 0; i < 9; i++)
                Shape("Merdiven basamağı", PrimitiveType.Cube, corridor.transform, new Vector3(3.5f, -.05f + i * .08f, 4 + i * .65f), new Vector3(2.4f, .15f + i * .16f, .65f), mats["YY_stone"]);
            Model("Plant", corridor.transform, new Vector3(-4.3f, 0, 1), 2);
            Sign("MERDİVEN", corridor.transform, new Vector3(0, 1.9f, 1), "↓");
            var street = Group("Yuvarlak sokak ve avlu", world.transform, new Vector3(0, 0, 44));
            Shape("Sokak zemini", PrimitiveType.Cube, street.transform, new Vector3(0, -.17f, 13), new Vector3(28, .32f, 62), mats["Road"], true);
            Shape("Açık yaya yolu", PrimitiveType.Cube, street.transform, new Vector3(-2, .015f, 13), new Vector3(6.7f, .045f, 61), mats["YY_stone"]);
            for (int i = 0; i < 7; i++)
            {
                Model("Facade_" + (1 + i % 4), street.transform, new Vector3(i % 2 == 0 ? -10 : 9, 0, -4 + i * 6), 1.15f, i % 2 == 0 ? 90 : -90);
                Tree(street.transform, new Vector3(i % 2 == 0 ? -5.8f : 5.8f, 0, -3 + i * 6), 1.1f + (i % 3) * .12f);
            }
            for (int i = 0; i < 15; i++) Shape("Ekip yolu çizgisi", PrimitiveType.Cube, street.transform, new Vector3(3.5f, .045f, -4 + i * 3.7f), new Vector3(.13f, .03f, 1.8f), mats["YY_cream"]);
            for (int i = 0; i < 8; i++) Model("Cone", street.transform, new Vector3(1.7f, .06f, 8 + i * 2), 1.2f);
            Model("Bench", street.transform, new Vector3(-5.1f, 0, 3), 1.2f, 90);
            Model("Plant", street.transform, new Vector3(5.4f, 0, 5), 2.5f);
            // The broken facade is outside the traversable pedestrian area.
            var danger = Group("Görevliye bildirilebilen cephe", street.transform, new Vector3(7, 0, 3));
            for (int i = 0; i < 5; i++) Shape("Cephe parçası", PrimitiveType.Cube, danger.transform, new Vector3(i * .35f, .06f, i % 2 * .4f), new Vector3(.4f, .12f, .5f), mats["YY_terracotta"]);
            mainBarricade = Group("Kontrol için kapanan ana rota", street.transform, new Vector3(0, 0, 31));
            for (int i = 0; i < 4; i++) Model("Cone", mainBarricade.transform, new Vector3(-2 + i * 1.3f, 0, 0), 1.5f);
            Sign("KONTROL", mainBarricade.transform, new Vector3(0, 1.5f, 0), "↗"); mainBarricade.SetActive(false);
            var area = Group("Ana toplanma alanı", world.transform, new Vector3(0, 0, 90));
            GatheringArea(area.transform, false);
            var alternative = Group("İkinci güvenli toplanma alanı", world.transform, new Vector3(25, 0, 92));
            GatheringArea(alternative.transform, true);
            Shape("İki alan arasındaki açık yol", PrimitiveType.Cube, world.transform, new Vector3(13, -.15f, 85), new Vector3(28, .3f, 7), mats["Floor"], true);
            familySign = Sign("BULUŞMA", area.transform, new Vector3(-2, 1.6f, 2), "✦");
            alternativeSign = Sign("YENİ ALAN", alternative.transform, new Vector3(-2, 1.6f, 2), "↗");
            radioReady = Model("Radio", area.transform, new Vector3(4, .95f, 4), 1.5f);
            var positions = new Dictionary<string, Vector3> {
                {"living",new Vector3(0,0,-1)},{"plan",new Vector3(-4,0,2.3f)},{"bag",new Vector3(2,0,.4f)},
                {"radio",new Vector3(-1,0,3.8f)},{"kitchen",new Vector3(5,0,2.7f)},{"aid",new Vector3(2,0,3.6f)},
                {"door",new Vector3(3,0,-4)},{"shelf",new Vector3(-5.3f,0,4.5f)},{"wardrobe",new Vector3(4.7f,0,4.5f)},
                {"table",new Vector3(0,0,.2f)},{"cover",new Vector3(0,0,-.45f)},
                {"corridor",new Vector3(-1,0,14)},{"stairs",new Vector3(0,0,23)},{"neighbor",new Vector3(-1,0,29)},
                {"front",new Vector3(-1,0,37)},{"street",new Vector3(-1,0,44)},{"fire_entry",new Vector3(-2,0,56)},
                {"fire",new Vector3(-1,0,65)},{"assembly",new Vector3(-2,0,88)},{"aidpoint",new Vector3(3,0,92)},
                {"alternate",new Vector3(23,0,91)},{"neighbor_final",new Vector3(-6,0,92)},{"help_final",new Vector3(4,0,87)}
            };
            foreach (var p in positions) { stations[p.Key] = p.Value; Group("Bölge · " + p.Key, interactions.transform, p.Value); }
        }

        static void GatheringArea(Transform parent, bool alternative)
        {
            Shape("Güvenli alan zemini", PrimitiveType.Cube, parent, new Vector3(0, -.15f, 0), new Vector3(22, .3f, 20), mats["Floor"], true);
            for (int i = 0; i < 6; i++) Tree(parent, new Vector3(-9 + i * 3.5f, 0, 8), 1.2f);
            var tent = Group("Yardım gölgeliği", parent, new Vector3(4, 0, 4));
            for (int i = 0; i < 4; i++) Shape("Gölgelik direği", PrimitiveType.Cylinder, tent.transform, new Vector3(i % 2 == 0 ? -2.5f : 2.5f, 1.5f, i < 2 ? -1.3f : 1.3f), new Vector3(.08f, 1.5f, .08f), mats["YY_wood"]);
            Shape("Turkuaz tente", PrimitiveType.Cube, tent.transform, new Vector3(0, 3.04f, 0), new Vector3(5.6f, .12f, 3.3f), mats["YY_teal"]);
            Model("Table", tent.transform, new Vector3(0, 0, 0), 1.4f);
            Model("Water", tent.transform, new Vector3(-.8f, 1.05f, 0), 1.1f);
            Model("FirstAid", tent.transform, new Vector3(.7f, 1.05f, 0), 1.15f);
            Model("Blanket", tent.transform, new Vector3(0, 1.05f, 0), 1.1f);
            Sign("YARDIM", tent.transform, new Vector3(0, 2.5f, -.4f), "+");
            Model("Bench", parent, new Vector3(-5, 0, 3), 1.4f, 20);
            Model("Bench", parent, new Vector3(-5, 0, -1), 1.4f, -20);
            Tree(parent, new Vector3(-2, 0, 3), 1.45f);
            Shape("Buluşma çemberi", PrimitiveType.Cylinder, parent, new Vector3(-2, .02f, 0), new Vector3(4, .015f, 4), mats[alternative ? "YY_coral" : "YY_teal"]);
        }

        static void Tree(Transform parent, Vector3 position, float scale)
        {
            var tree = Group("Ada ağacı", parent, position); tree.transform.localScale = Vector3.one * scale;
            Shape("Gövde", PrimitiveType.Cylinder, tree.transform, new Vector3(0, 1.25f, 0), new Vector3(.32f, 1.25f, .32f), mats["YY_wood"]);
            for (int i = 0; i < 5; i++) Shape("Yumuşak taç", PrimitiveType.Sphere, tree.transform, new Vector3(Mathf.Cos(i * 1.3f) * .65f, 2.9f + i % 2 * .35f, Mathf.Sin(i * 1.3f) * .65f), new Vector3(1.8f, 1.7f, 1.7f), mats[i % 2 == 0 ? "YY_leaf" : "YY_leafLight"]);
            Shape("Toprak halkası", PrimitiveType.Cylinder, tree.transform, new Vector3(0, .015f, 0), new Vector3(1.7f, .015f, 1.7f), mats["Soil"]);
        }

        static GameObject Sign(string text, Transform parent, Vector3 position, string icon)
        {
            var root = Group(text + " işareti", parent, position);
            Shape("Levha", PrimitiveType.Cube, root.transform, Vector3.zero, new Vector3(1.5f, .9f, .08f), mats["YY_tealDark"]);
            Shape("Direk", PrimitiveType.Cylinder, root.transform, new Vector3(0, -.9f, .03f), new Vector3(.07f, .9f, .07f), mats["YY_wood"]);
            WorldText(icon + "\n" + text, root.transform, new Vector3(0, 0, -.07f), .24f, Hex("#FFF0CF")); return root;
        }

        static TextMeshPro WorldText(string text, Transform parent, Vector3 position, float size, Color color)
        {
            var go = Group(text, parent, position); var t = go.AddComponent<TextMeshPro>(); t.font = font;
            t.text = SafeText(text); t.fontSize = size * 16; t.color = color; t.alignment = TextAlignmentOptions.Center;
            t.rectTransform.sizeDelta = new Vector2(3, 1); t.enableWordWrapping = true; return t;
        }

        static void CreateCharacters()
        {
            foreach (string name in Names)
            {
                Vector3 position = name == "Idil" ? stations["fire"] : name == "Bora" ? stations["aidpoint"] : name == "Yusuf" ? stations["neighbor"] : stations["living"] + new Vector3(Array.IndexOf(Names, name) * .85f - 2, 0, 1.5f);
                if (name == "Derya") position = stations["plan"] + new Vector3(-.65f, 0, .8f);
                if (name == "Emre") position = stations["plan"] + new Vector3(.65f, 0, 1.1f);
                var root = Group(name, castRoot.transform, position);
                var model = Model(name, root.transform, Vector3.zero); model.name = "Model — " + name;
                var animator = model.GetComponent<Animator>(); if (!animator) animator = model.AddComponent<Animator>();
                CorrectModelFacing(model);
                animator.runtimeAnimatorController = actorController; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                if (animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman) throw new InvalidOperationException("Humanoid mapping failed: " + name);
                var capsule = root.AddComponent<CapsuleCollider>(); capsule.radius = .24f; capsule.height = name == "Efe" ? 1.13f : name == "Ada" ? 1.36f : 1.74f; capsule.center = Vector3.up * capsule.height * .5f;
                root.transform.rotation = Quaternion.Euler(0, 180, 0); cast[name] = root;
                if (name == "Ada" || name == "Idil" || name == "Bora")
                {
                    var agent = root.AddComponent<NavMeshAgent>(); agent.speed = name == "Ada" ? 1.7f : 1.85f; agent.acceleration = 6; agent.angularSpeed = 280; agent.radius = .23f; agent.height = capsule.height; agent.stoppingDistance = .15f;
                    var mover = root.AddComponent<StoryPlayerMovement>(); Serialized(mover, "animator", animator); movers[name] = mover;
                }
                else if (name == "Efe")
                {
                    var agent = root.AddComponent<NavMeshAgent>(); agent.speed = 1.9f; agent.acceleration = 6; agent.radius = .2f; agent.height = 1.13f;
                    follower = root.AddComponent<StorySiblingFollower>(); Serialized(follower, "animator", animator);
                }
                else if (name == "Yusuf") Model("WalkingCane", root.transform, new Vector3(.32f, 0, .12f), 1.25f);
            }
            Serialized(follower, "target", cast["Ada"].transform);
            bagWorn = Model("BackpackClosed", cast["Ada"].transform, new Vector3(0, .59f, -.19f), .76f, 180); bagWorn.SetActive(false);
            comfortWorn = Model("ComfortFox", cast["Efe"].transform, new Vector3(.27f, .49f, .1f), .65f); comfortWorn.SetActive(false);
            for (int i = 0; i < 6; i++)
            {
                string source = new[] { "Derya", "Emre", "Yusuf", "Derya", "Emre", "Bora" }[i];
                var extra = Model(source, castRoot.transform, new Vector3(-7 + (i % 3) * 5, 0, 95 + (i / 3) * 4), .94f + (i % 3) * .035f, 150 + i * 21);
                extra.name = "Mahalleli " + (i + 1) + " — özgün gövde varyasyonu";
                var animator = extra.GetComponent<Animator>(); animator.runtimeAnimatorController = actorController;
                foreach (var renderer in extra.GetComponentsInChildren<Renderer>())
                {
                    var materials = renderer.sharedMaterials;
                    for (int j = 0; j < materials.Length; j++) if (materials[j].name == "YY_teal" || materials[j].name == "YY_coral" || materials[j].name == "YY_navy") materials[j] = mats[new[] { "YY_olive", "YY_mustard", "YY_blue", "YY_coral", "YY_teal", "YY_cream" }[i]];
                    renderer.sharedMaterials = materials;
                }
            }
        }

        static void CorrectModelFacing(GameObject model)
        {
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            var head = model.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "Head");
            if (!renderer || !head) return;
            int eyes = Array.FindIndex(renderer.sharedMaterials, m => m && m.name == "YY_iris");
            if (eyes < 0) return;
            var mesh = new Mesh(); renderer.BakeMesh(mesh);
            var vertices = mesh.vertices; var indices = mesh.GetTriangles(eyes);
            Vector3 sum = Vector3.zero; foreach (int i in indices) sum += renderer.transform.TransformPoint(vertices[i]);
            if (indices.Length > 0)
            {
                var face = sum / indices.Length - head.position; face.y = 0;
                if (Vector3.Dot(face, model.transform.forward) < 0) model.transform.localRotation *= Quaternion.Euler(0, 180, 0);
            }
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        static CinemachineCamera View(YYBeat beat)
        {
            string key = beat.station + "_" + beat.camera;
            if (views.TryGetValue(key, out var existing)) return existing;
            var focus = stations[beat.station] + Vector3.up * 1.05f;
            float zoom = beat.camera == "wide" ? 6.5f : beat.camera == "cover" ? 3.65f : 4.65f;
            var go = Group("Kadraj · " + key, presentation.transform, focus + new Vector3(3.8f, 7.6f, -11.5f));
            go.transform.LookAt(focus); var view = go.AddComponent<CinemachineCamera>();
            var lens = view.Lens; lens.OrthographicSize = zoom; lens.NearClipPlane = .1f; lens.FarClipPlane = 55;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic; view.Lens = lens; view.Priority = 10;
            go.SetActive(false); views[key] = view; return view;
        }
    }
}
