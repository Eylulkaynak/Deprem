// Editor-only adaptation of this project's approved custom character art.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace YanYana.Editor
{
    public static class YanYanaCharacterStyle
    {
        public const string Root = "Assets/YanYana/Characters/ApprovedStyle";
        public static readonly string[] Names = { "Ada", "Efe", "Derya", "Emre", "Yusuf", "Idil", "Bora" };
        static readonly string[] Bases = { "Komsu", "Can", "Anne", "Baba", "Baba", "Firefighter", "RescueWorker" };
        public static readonly float[] Heights = { 1.46f, 1.25f, 1.70f, 1.82f, 1.73f, 1.75f, 1.80f };

        [MenuItem("Tools/Yan Yana/Art/Prepare Approved Character Style")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave play mode before authoring character prefabs.");
            Directory.CreateDirectory(Root); Directory.CreateDirectory("ArtDirection/YanYana/Characters/ApprovedStyle");
            var scene = EditorSceneManager.NewPreviewScene(); var report = new List<string>();
            try
            {
                for (int i = 0; i < Names.Length; i++)
                {
                    string source = "Assets/Story/Art/KKTC/Characters/" + Bases[i] + ".prefab";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
                    if (!prefab) throw new FileNotFoundException(source);
                    var actor = UnityEngine.Object.Instantiate(prefab); SceneManager.MoveGameObjectToScene(actor, scene); actor.name = Names[i];
                    // These are visual prefabs. Chapter scripts and hit targets never cross scenes.
                    foreach (var component in actor.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(component);
                    foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                    foreach (var body in actor.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
                    var animator = actor.GetComponentInChildren<Animator>(true);
                    animator.applyRootMotion = false; animator.stabilizeFeet = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.runtimeAnimatorController = YanYanaAdultLocomotion.ForCharacter(Names[i],
                        AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(YanYanaAdultLocomotion.BaseControllerPath));
                    foreach (var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var mesh = UnityEngine.Object.Instantiate(renderer.sharedMesh); mesh.name = Names[i] + "_Surface";
                        string meshPath = Root + "/" + mesh.name + ".asset";
                        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if (saved) { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); } else { AssetDatabase.CreateAsset(mesh, meshPath); saved = mesh; }
                        renderer.sharedMesh = saved;
                        var materials = renderer.sharedMaterials;
                        for (int j = 0; j < materials.Length; j++)
                        {
                            var mat = new Material(materials[j]); mat.name = Names[i] + "_ClothesAndFace_" + j; mat.SetFloat("_BumpScale", .25f); mat.SetFloat("_Smoothness", .25f);
                            string matPath = Root + "/" + mat.name + ".mat"; var savedMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                            if (savedMat) { EditorUtility.CopySerialized(mat, savedMat); UnityEngine.Object.DestroyImmediate(mat); } else { AssetDatabase.CreateAsset(mat, matPath); savedMat = mat; }
                            materials[j] = savedMat;
                        }
                        renderer.sharedMaterials = materials; renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
                    }
                    Fit(actor, Heights[i]);
                    PrefabUtility.SaveAsPrefabAsset(actor, Root + "/" + Names[i] + ".prefab");
                    report.Add(Names[i] + " | approved base=" + Bases[i] + " | height=" + Heights[i] + " m | own mesh/material copies; source unchanged");
                    UnityEngine.Object.DestroyImmediate(actor);
                }
                AssetDatabase.SaveAssets();
                File.WriteAllLines("ArtDirection/YanYana/Characters/ApprovedStyle/Provenance.txt", report);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            RenderReview();
        }

        public static void Fit(GameObject actor, float height)
        {
            actor.transform.localScale = Vector3.one;
            var renderers = actor.GetComponentsInChildren<Renderer>(); var bounds = BoundsOf(renderers);
            actor.transform.localScale = Vector3.one * (height / bounds.size.y);
            var animator = actor.GetComponentInChildren<Animator>(); animator.Rebind(); animator.Update(0f);
            bounds = BoundsOf(renderers);
            animator.transform.position += Vector3.up * (-.004f - bounds.min.y);
            NormalizeRoot(actor);
        }
        public static void NormalizeRoot(GameObject actor)
        {
            var scale=actor.transform.localScale;
            foreach(var child in actor.transform.Cast<Transform>().ToArray())
            {
                child.localPosition=Vector3.Scale(child.localPosition,scale);
                child.localScale=Vector3.Scale(child.localScale,scale);
            }
            actor.transform.localScale=Vector3.one;
        }
        static Bounds BoundsOf(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds); return bounds;
        }
        [MenuItem("Tools/Yan Yana/Art/Render Character Review")]
        public static void RenderReview()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            Directory.CreateDirectory("ClientExports/YanYana/ArtReview");
            try
            {
                var camObject = new GameObject("Character review camera"); SceneManager.MoveGameObjectToScene(camObject, scene);
                var camera = camObject.AddComponent<Camera>(); camera.cameraType = CameraType.Preview; camera.scene = scene;
                camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.85f, .88f, .83f); camera.nearClipPlane = .1f; camera.farClipPlane = 30;
                var lightObject = new GameObject("Review key"); SceneManager.MoveGameObjectToScene(lightObject, scene); var key = lightObject.AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.15f; key.color = new Color(1, .95f, .88f); key.transform.rotation = Quaternion.Euler(35, -25, 0);
                var fillObject = new GameObject("Review fill"); SceneManager.MoveGameObjectToScene(fillObject, scene); var fill = fillObject.AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = .65f; fill.color = new Color(.76f, .88f, 1); fill.transform.rotation = Quaternion.Euler(20, 150, 0);
                var people = new List<GameObject>();
                for (int i = 0; i < Names.Length; i++)
                {
                    var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + Names[i] + ".prefab")); SceneManager.MoveGameObjectToScene(actor, scene);
                    actor.transform.position = new Vector3((i - 3) * 1.1f, 0, 0); actor.transform.rotation = Quaternion.Euler(0, 180, 0);
                    var animator = actor.GetComponentInChildren<Animator>(); animator.Rebind(); animator.Update(0);
                    people.Add(actor);
                }
                camera.transform.position = new Vector3(0, 2, -8); camera.transform.LookAt(new Vector3(0, .9f, 0)); camera.orthographicSize = 1.36f;
                Render(camera, 2100, 760, "ClientExports/YanYana/ArtReview/ApprovedStyle_Lineup.png");
                for (int i = 0; i < people.Count; i++)
                {
                    foreach (var actor in people) actor.SetActive(actor == people[i]);
                    var head = people[i].GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
                    var center = head.position + Vector3.up * Heights[i] * .06f;
                    camera.transform.position = center + new Vector3(.12f, .04f, -3); camera.transform.LookAt(center); camera.orthographicSize = Heights[i] * .23f;
                    camera.backgroundColor = new Color(0, 0, 0, 0);
                    Render(camera, 384, 384, "Assets/YanYana/UI/Portraits/" + Names[i] + ".png");
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            AssetDatabase.Refresh(); Debug.Log("YAN YANA approved-style character review rendered.");
        }
        static void Render(Camera camera, int width, int height, string path)
        {
            var texture = new RenderTexture(width, height, 24); camera.targetTexture = texture; camera.aspect = width / (float)height; camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = texture; var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0); result.Apply(); File.WriteAllBytes(path, result.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(result); UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
