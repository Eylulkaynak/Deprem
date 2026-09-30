// Exports only our approved-style derivatives for editable Blender authoring.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YanYana.Editor
{
    public static class YanYanaEditableCharacterExport
    {
        [Serializable] public class Bone {public string name;public int parent;public Vector3 point;}
        [Serializable] public class Weight {public int[] bones;public float[] values;}
        [Serializable] public class Shape {public string name;public Vector3[] deltas;}
        [Serializable] public class Surface {public string name;public Vector3[] vertices;public Vector2[] uv;public FaceGroup[] groups;public Weight[] weights;public Bone[] bones;public Shape[] shapes;}
        [Serializable] public class FaceGroup {public int[] indices;public string texture,regionTexture;public Color color,wardrobeColor,greyColor;public float wardrobeMode,greyAmount;}
        [Serializable] public class Character {public string name;public string provenance;public Surface[] surfaces;}
        public static Vector3[] EvaluateSurface(SkinnedMeshRenderer renderer,Mesh mesh=null)
        {
            mesh=mesh?mesh:renderer.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;
            var matrices=renderer.bones.Select((bone,index)=>bone.localToWorldMatrix*mesh.bindposes[index]).ToArray();
            for(int i=0;i<vertices.Length;i++)
            {var w=weights[i];var v=vertices[i];vertices[i]=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;}
            return vertices;
        }
        [MenuItem("Tools/Yan Yana/Art/Export Editable Approved Characters")]
        public static void Export()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before exporting sources.");
            string output="ArtDirection/YanYana/Characters/ApprovedStyle";Directory.CreateDirectory(output+"/Textures");var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                foreach(string name in YanYanaCharacterStyle.Names.Concat(YanYanaCharacterVariants.Names))
                {
                    string root=YanYanaCharacterStyle.Names.Contains(name)?YanYanaCharacterStyle.Root:YanYanaCharacterVariants.Root;
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/"+name+".prefab");var actor=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.position=Vector3.zero;actor.transform.rotation=Quaternion.identity;
                    var animator=actor.GetComponentInChildren<Animator>();YanYanaCharacterStyle.NormalizeRoot(actor);animator.Rebind();animator.Update(0);
                    // A skinned mesh's raw vertices are in its import bind space. Export the
                    // evaluated pose and the matching bone positions, in the same metre frame.
                    float sole=float.PositiveInfinity;
                    foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {foreach(var v in EvaluateSurface(skin))sole=Mathf.Min(sole,v.y);}
                    actor.transform.position-=Vector3.up*sole;
                    var transforms=actor.GetComponentInChildren<SkinnedMeshRenderer>().bones;
                    var surfaces=actor.GetComponentsInChildren<Renderer>().Where(r=>r is SkinnedMeshRenderer||r.GetComponent<MeshFilter>()).Select(renderer=>
                    {
                        var skin=renderer as SkinnedMeshRenderer;var mesh=skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;var result=new Surface{name=renderer.name};
                        result.vertices=skin?EvaluateSurface(skin):mesh.vertices.Select(v=>renderer.transform.TransformPoint(v)).ToArray();result.uv=mesh.uv;
                        result.shapes=Enumerable.Range(0,mesh.blendShapeCount).Select(shapeIndex=>
                        {
                            var deltas=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(shapeIndex,mesh.GetBlendShapeFrameCount(shapeIndex)-1,deltas,null,null);
                            if(skin){var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();var weights=mesh.boneWeights;for(int i=0;i<deltas.Length;i++){var w=weights[i];var d=deltas[i];deltas[i]=matrices[w.boneIndex0].MultiplyVector(d)*w.weight0+matrices[w.boneIndex1].MultiplyVector(d)*w.weight1+matrices[w.boneIndex2].MultiplyVector(d)*w.weight2+matrices[w.boneIndex3].MultiplyVector(d)*w.weight3;}}
                            else for(int i=0;i<deltas.Length;i++)deltas[i]=renderer.transform.TransformVector(deltas[i]);
                            return new Shape{name=mesh.GetBlendShapeName(shapeIndex),deltas=deltas};
                        }).ToArray();
                        result.bones=transforms.Select(t=>new Bone{name=t.name,parent=Array.IndexOf(transforms,t.parent),point=t.position}).ToArray();
                        if(skin)result.weights=mesh.boneWeights.Select(w=>new Weight{bones=new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3},values=new[]{w.weight0,w.weight1,w.weight2,w.weight3}}).ToArray();
                        else
                        {
                            var anchor=renderer.transform;while(anchor&&Array.IndexOf(transforms,anchor)<0)anchor=anchor.parent;
                            int index=Mathf.Max(0,Array.IndexOf(transforms,anchor));result.weights=mesh.vertices.Select(v=>new Weight{bones=new[]{index,0,0,0},values=new[]{1f,0f,0f,0f}}).ToArray();
                        }
                        result.groups=Enumerable.Range(0,mesh.subMeshCount).Select(i=>
                        {
                            var mat=renderer.sharedMaterials[Mathf.Min(i,renderer.sharedMaterials.Length-1)];
                            var group=new FaceGroup{indices=mesh.GetTriangles(i),texture=CopyTexture(mat.mainTexture,output),color=mat.HasProperty("_BaseColor")?mat.GetColor("_BaseColor"):Color.white};
                            if(mat.HasProperty("_WardrobeRegions")){group.regionTexture=CopyTexture(mat.GetTexture("_WardrobeRegions"),output);group.wardrobeColor=mat.GetColor("_WardrobeColor");group.greyColor=mat.GetColor("_GreyColor");group.wardrobeMode=mat.GetFloat("_WardrobeMode");group.greyAmount=mat.GetFloat("_GreyAmount");}
                            return group;
                        }).ToArray();return result;
                    }).ToArray();
                    File.WriteAllText(output+"/"+name+".mesh.json",JsonUtility.ToJson(new Character{name=name,provenance="Approved project character adaptation; original Blender wardrobe models. Evaluated surface and matching skeleton in metres; original project sources unchanged.",surfaces=surfaces}));UnityEngine.Object.DestroyImmediate(actor);
                }
                Debug.Log("Exported seven main characters and six neighbors, with wardrobe meshes, skin weights and skeletons.");
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        static string CopyTexture(Texture texture,string output)
        {if(!texture)return "";string path=AssetDatabase.GetAssetPath(texture);if(!File.Exists(path))return "";string copied="Textures/"+AssetDatabase.AssetPathToGUID(path)+Path.GetExtension(path);File.Copy(path,output+"/"+copied,true);return copied;}
    }
}
