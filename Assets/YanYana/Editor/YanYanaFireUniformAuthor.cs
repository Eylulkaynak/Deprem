// Editor-only asset authoring. Original clothing topology and native Unity skinning are preserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Deprem.Minigames;

namespace YanYana.Editor
{
    public static partial class YanYanaFirePresentation
    {
        static Mesh StoreUniformMesh(Mesh generated,string path)
        {
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!saved){AssetDatabase.CreateAsset(generated,path);return generated;}
            saved.Clear(false);saved.name=generated.name;saved.vertices=generated.vertices;saved.normals=generated.normals;
            saved.tangents=generated.tangents;saved.uv=generated.uv;saved.boneWeights=generated.boneWeights;saved.bindposes=generated.bindposes;
            saved.subMeshCount=generated.subMeshCount;
            for(int sub=0;sub<generated.subMeshCount;sub++)saved.SetTriangles(generated.GetTriangles(sub),sub);
            saved.ClearBlendShapes();
            for(int shape=0;shape<generated.blendShapeCount;shape++)for(int frame=0;frame<generated.GetBlendShapeFrameCount(shape);frame++)
            {
                var points=new Vector3[generated.vertexCount];var normals=new Vector3[generated.vertexCount];var tangents=new Vector3[generated.vertexCount];
                generated.GetBlendShapeFrameVertices(shape,frame,points,normals,tangents);
                saved.AddBlendShapeFrame(generated.GetBlendShapeName(shape),generated.GetBlendShapeFrameWeight(shape,frame),points,normals,tangents);
            }
            saved.RecalculateBounds();EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(generated);return saved;
        }
        [MenuItem("Tools/Yan Yana/Presentation/Connect Uniform Arms")]
        public static void ConnectUniformArms()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring sleeves.");
            AuthorUniformArms();YanYanaAdventureBuilder.RefreshImmersiveFireInput();AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            ReviewPose();ReviewConnectedSleeves();
            Debug.Log("Original connected uniform, corrected sleeve skin weights and editor hose grip saved.");
        }
        static void AuthorBoundUniformSurface()
        {
            var actor=Find("Idil");var animator=actor.GetComponentInChildren<Animator>();var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
            animator.Rebind();animator.Update(0);
            var source=AssetDatabase.LoadAssetAtPath<Mesh>(YanYanaInteractionHands.Root+"/Idil_InteractionSurface.asset");
            var points=YanYanaEditableCharacterExport.EvaluateSurface(skin,source);var weights=source.boneWeights;
            var hands=new HashSet<int>(YanYanaInteractionHands.Read("Idil").hands.SelectMany(h=>h.indices));var removed=new HashSet<int>();
            Remove(actor,"İskeleti takip eden itfaiye kolları");var root=Group("İskeleti takip eden itfaiye kolları",actor);
            var red=Material("FirefighterUniformSleeve","Universal Render Pipeline/Lit",new Color(.62f,.016f,.033f));
            var yellow=Material("FirefighterUniformReflector","Universal Render Pipeline/Lit",new Color(.81f,.71f,.25f));
            var silver=Material("FirefighterUniformReflectorSilver","Universal Render Pipeline/Lit",new Color(.44f,.48f,.45f));
            foreach(var material in new[]{red,yellow,silver}){material.SetFloat("_Smoothness",.12f);material.SetFloat("_Cull",2);EditorUtility.SetDirty(material);}
            foreach(bool right in new[]{false,true})
            {
                string side=right?"Sağ":"Sol";
                var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                var shoulder=animator.GetBoneTransform(right?HumanBodyBones.RightShoulder:HumanBodyBones.LeftShoulder);
                Remove(upper,side+" üst kol kumaşı");Remove(lower,side+" dirsek kumaşı");
                var ids=new HashSet<int>(new[]{Array.IndexOf(skin.bones,upper),Array.IndexOf(skin.bones,lower),Array.IndexOf(skin.bones,shoulder)});
                var axis=(hand.position-lower.position).normalized;
                for(int i=0;i<points.Length;i++)
                {
                    if(hands.Contains(i))continue;var w=weights[i];float influence=(ids.Contains(w.boneIndex0)?w.weight0:0)+(ids.Contains(w.boneIndex1)?w.weight1:0)+(ids.Contains(w.boneIndex2)?w.weight2:0)+(ids.Contains(w.boneIndex3)?w.weight3:0);
                    float along=Vector3.Dot(points[i]-lower.position,axis);float radial=(points[i]-lower.position-axis*along).magnitude;
                    if(influence>.35f&&along>-.012f&&along<Vector3.Distance(hand.position,lower.position)+.06f&&radial<.105f)removed.Add(i);
                }
                var definition=YanYanaInteractionHands.Read("Idil").hands.Single(h=>h.right==right);
                int sampleCount=Mathf.Max(12,definition.indices.Length/8);
                var wrist=definition.indices.OrderBy(i=>Vector3.Distance(points[i],lower.position)).Take(sampleCount).Select(i=>points[i]).Aggregate(Vector3.zero,(a,b)=>a+b)/sampleCount;
                var direction=(wrist-lower.position).normalized;float length=Vector3.Distance(wrist,lower.position);
                Remove(lower,side+" kumaş dirseği");Remove(hand,side+" kumaş bileği");
                var elbowBone=Group(side+" kumaş dirseği",lower);elbowBone.position=lower.position;
                var cuffBone=Group(side+" kumaş bileği",hand);cuffBone.position=wrist;
                elbowBone.rotation=cuffBone.rotation=Quaternion.LookRotation(direction,actor.up);
                var boneArray=new[]{elbowBone,cuffBone};const int sides=20;
                var vertices=new List<Vector3>();var nativeWeights=new List<BoneWeight>();var uv=new List<Vector2>();var triangles=new[]{new List<int>(),new List<int>(),new List<int>()};
                var rings=new[]{-.15f,.05f,.22f,.42f,.62f,.67f,.71f,.78f,.84f,.92f,1f};
                var tangent=Vector3.Cross(direction,actor.forward).normalized;if(tangent.sqrMagnitude<.5f)tangent=Vector3.Cross(direction,actor.right).normalized;
                var bitangent=Vector3.Cross(direction,tangent).normalized;
                for(int ring=0;ring<rings.Length;ring++)
                {
                    float t=rings[ring];var center=lower.position+direction*(t<0?-.022f:t*length);float radius=Mathf.Lerp(.065f,.0312f,Mathf.Clamp01(t));
                    float handPart=Mathf.Clamp01(t);
                    var weight=handPart>.5f?new BoneWeight{boneIndex0=1,boneIndex1=0,weight0=handPart,weight1=1-handPart}:new BoneWeight{boneIndex0=0,boneIndex1=1,weight0=1-handPart,weight1=handPart};
                    for(int sideIndex=0;sideIndex<sides;sideIndex++)
                    {
                        float angle=sideIndex*2*Mathf.PI/sides;vertices.Add(root.InverseTransformPoint(center+radius*(tangent*Mathf.Cos(angle)+bitangent*Mathf.Sin(angle))));nativeWeights.Add(weight);uv.Add(new Vector2(sideIndex/(float)sides,t));
                    }
                    if(ring==0)continue;float band=(t+rings[ring-1])*.5f;int sub=band>=.71f&&band<.78f?2:band>=.67f&&band<.84f?1:0;
                    for(int sideIndex=0;sideIndex<sides;sideIndex++){int a=(ring-1)*sides+sideIndex,b=(ring-1)*sides+(sideIndex+1)%sides,c=ring*sides+sideIndex,d=ring*sides+(sideIndex+1)%sides;triangles[sub].AddRange(new[]{a,b,c,b,d,c});}
                }
                foreach(int ring in new[]{0,rings.Length-1})
                {
                    int cap=vertices.Count;var center=vertices.Skip(ring*sides).Take(sides).Aggregate(Vector3.zero,(a,b)=>a+b)/sides;vertices.Add(center);nativeWeights.Add(nativeWeights[ring*sides]);uv.Add(new Vector2(.5f,ring==0?0:1));
                    for(int sideIndex=0;sideIndex<sides;sideIndex++){int a=ring*sides+sideIndex,b=ring*sides+(sideIndex+1)%sides;triangles[0].AddRange(ring==0?new[]{cap,b,a}:new[]{cap,a,b});}
                }
                var mesh=new Mesh{name="FirefighterConnected"+(right?"Right":"Left")+"Forearm"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.boneWeights=nativeWeights.ToArray();mesh.bindposes=boneArray.Select(b=>b.worldToLocalMatrix*root.localToWorldMatrix).ToArray();mesh.subMeshCount=3;
                for(int sub=0;sub<3;sub++)mesh.SetTriangles(triangles[sub],sub);mesh.RecalculateNormals();mesh.RecalculateBounds();
                string path="Assets/YanYana/Art/Models/"+mesh.name+".asset";var saved=StoreUniformMesh(mesh,path);
                var cloth=Group(side+" kesintisiz kol kumaşı",root).gameObject.AddComponent<SkinnedMeshRenderer>();cloth.sharedMesh=saved;cloth.bones=boneArray;cloth.rootBone=lower;cloth.quality=SkinQuality.Bone4;cloth.sharedMaterials=new[]{red,yellow,silver};cloth.updateWhenOffscreen=true;cloth.localBounds=new Bounds(Vector3.up*.65f,Vector3.one*2.5f);
            }
            var body=UnityEngine.Object.Instantiate(source);body.name="FirefighterConnectedUniform";var originalBody=source.GetTriangles(0);var kept=new List<int>();
            for(int i=0;i<originalBody.Length;i+=3)if(!removed.Contains(originalBody[i])&&!removed.Contains(originalBody[i+1])&&!removed.Contains(originalBody[i+2]))kept.AddRange(new[]{originalBody[i],originalBody[i+1],originalBody[i+2]});
            body.SetTriangles(kept,0);body.SetTriangles(source.GetTriangles(1),1);string bodyPath=YanYanaInteractionHands.Root+"/"+body.name+".asset";var savedBody=StoreUniformMesh(body,bodyPath);
            skin.sharedMesh=savedBody;skin.quality=SkinQuality.Bone4;EditorUtility.SetDirty(skin);Directory.CreateDirectory("ClientExports/YanYana/Reports");
            File.WriteAllText("ClientExports/YanYana/Reports/connected-fire-sleeves.txt","Original shoulder/upper-arm surface preserved; native skinned forearms replace "+removed.Count+" incorrectly bound cloth vertices.\n");
        }
        static void BakeHosePreview()
        {
            var actor=Find("Idil");var animator=actor.GetComponentInChildren<Animator>();var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
            var nozzle=Find("İdil’in gerçek hortum başlığı");var placement=actor.Find("Hortum başlığının gövde önündeki konumu").localPosition;
            var left=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);var right=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var forward=Vector3.Cross(right.position-left.position,actor.up).normalized;
            nozzle.position=(left.position+right.position)*.5f+forward*placement.z+actor.TransformDirection(new Vector3(placement.x,placement.y,0));
            nozzle.rotation=Quaternion.LookRotation(actor.forward+Vector3.down*.15f,actor.up);
            var data=YanYanaInteractionHands.Read("Idil");var solve=typeof(FirefighterExtinguishManager).GetMethod("SolveArm",BindingFlags.Static|BindingFlags.NonPublic);
            for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)skin.SetBlendShapeWeight(i,0);
            foreach(bool isRight in new[]{true,false})
            {
                var definition=data.hands.Single(h=>h.right==isRight);var pose=definition.poses.Single(p=>p.name==(isRight?"Right_Loop":"Left_Cylinder"));
                var hand=animator.GetBoneTransform(isRight?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                var upper=animator.GetBoneTransform(isRight?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(isRight?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
                var grip=Find(isRight?"Sağ el hortum kavrama":"Sol el hortum kavrama");
                var axis=isRight?nozzle.up:nozzle.forward;var palm=isRight?nozzle.forward:nozzle.TransformDirection(nozzle.Find("Sol hortum avuç yönü").localPosition);
                var rotation=Quaternion.LookRotation(axis,palm)*Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(definition.forward,definition.normal).normalized,definition.normal));
                hand.rotation=rotation;
                var hint=actor.TransformDirection(actor.Find(isRight?"Sağ hortum dirseğinin yönü":"Sol hortum dirseğinin yönü").localPosition);
                solve.Invoke(null,new object[]{upper,lower,hand,grip.position-hand.TransformVector(pose.center),hint});hand.rotation=rotation;
                skin.SetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(pose.name),100);
            }
            foreach(bool isRight in new[]{false,true})
            {
                string side=isRight?"Sağ":"Sol";
                var elbow=animator.GetBoneTransform(isRight?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm).Find(side+" kumaş dirseği");
                var cuff=animator.GetBoneTransform(isRight?HumanBodyBones.RightHand:HumanBodyBones.LeftHand).Find(side+" kumaş bileği");
                elbow.rotation=cuff.rotation=Quaternion.LookRotation(cuff.position-elbow.position,actor.up);
            }
            foreach(var transform in animator.GetComponentsInChildren<Transform>())
            {EditorUtility.SetDirty(transform);PrefabUtility.RecordPrefabInstancePropertyModifications(transform);}
            EditorUtility.SetDirty(skin);EditorUtility.SetDirty(nozzle);
            var hose=All<LineRenderer>().First(l=>l.name=="Zeminden gelen besleme hortumu");hose.positionCount=25;
            var rear=nozzle.TransformPoint(new Vector3(0,0,-.062f));var outward=nozzle.TransformPoint(nozzle.Find("Hortumun kol dışındaki kavis kontrolü").localPosition);
            var hip=actor.TransformPoint(new Vector3(.44f,.42f,.25f));var knee=actor.TransformPoint(new Vector3(.54f,.10f,.20f));var floor=actor.TransformPoint(new Vector3(.80f,.035f,.12f));var bend=actor.TransformPoint(new Vector3(1.28f,.035f,-.04f));var end=actor.TransformPoint(new Vector3(2.2f,.035f,-1.15f));
            for(int i=0;i<25;i++)
            {
                float t=i/24f,q;Vector3 a,b,c;
                if(t<.375f){a=rear;b=outward;c=hip;q=t/.375f;}else if(t<.708333f){a=hip;b=knee;c=floor;q=(t-.375f)/.333333f;}else{a=floor;b=bend;c=end;q=(t-.708333f)/.291667f;}
                hose.SetPosition(i,Vector3.Lerp(Vector3.Lerp(a,b,q),Vector3.Lerp(b,c,q),q));
            }
            EditorUtility.SetDirty(hose);
        }
        static bool ConnectedClothContains(Vector3 point,Vector3[] vertices,int[] triangles)
        {
            var ray=new Vector3(1,.371f,.529f).normalized;int hits=0;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=vertices[triangles[i]];var e1=vertices[triangles[i+1]]-a;var e2=vertices[triangles[i+2]]-a;
                var h=Vector3.Cross(ray,e2);float det=Vector3.Dot(e1,h);if(Mathf.Abs(det)<1e-9f)continue;
                float inverse=1/det;var s=point-a;float u=Vector3.Dot(s,h)*inverse;if(u<0||u>1)continue;
                var cross=Vector3.Cross(s,e1);float v=Vector3.Dot(ray,cross)*inverse;
                if(v>=0&&u+v<=1&&Vector3.Dot(e2,cross)*inverse>1e-6f)hits++;
            }
            return hits%2==1;
        }
        public static void ReviewConnectedSleeves()
        {
            var actor=Find("Idil");var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>();var body=skins.First();
            if(body.sharedMesh.name!="FirefighterConnectedUniform")throw new InvalidOperationException("Missing bound uniform.");
            if(actor.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.name.Contains("kol kumaşı")||f.name.Contains("dirsek kumaşı")))throw new InvalidOperationException("Detached primitive sleeve remains.");
            foreach(var item in skins)File.AppendAllText("ClientExports/YanYana/Reports/connected-fire-sleeves.txt",item.name+" mesh="+item.sharedMesh.name+" bones="+item.bones.Length+" bind="+item.sharedMesh.bindposes.Length+" vertices="+item.sharedMesh.vertexCount+" slots="+item.sharedMaterials.Length+" submeshes="+item.sharedMesh.subMeshCount+" quality="+item.quality+" projectSkinWeights="+QualitySettings.skinWeights+"\n");
            var sleeves=skins.Where(s=>s.name.EndsWith("kesintisiz kol kumaşı")).ToArray();if(sleeves.Length!=2)throw new InvalidOperationException("Missing native forearm skin.");
            foreach(var sleeve in sleeves)
            {
                var mesh=sleeve.sharedMesh;var edges=new Dictionary<(int,int),int>();var triangles=mesh.triangles;
                var baked=new Mesh();sleeve.BakeMesh(baked);var drawn=baked.vertices.Select(v=>sleeve.transform.TransformPoint(v)).ToArray();var expected=YanYanaEditableCharacterExport.EvaluateSurface(sleeve);
                float renderedError=drawn.Select((v,i)=>Vector3.Distance(v,expected[i])).Max();UnityEngine.Object.DestroyImmediate(baked);
                float maxRadiusError=0;var rings=new[]{-.15f,.05f,.22f,.42f,.62f,.67f,.71f,.78f,.84f,.92f,1f};
                for(int ring=0;ring<rings.Length;ring++)
                {
                    var points=drawn.Skip(ring*20).Take(20).ToArray();var center=points.Aggregate(Vector3.zero,(a,b)=>a+b)/20;
                    float radius=points.Select(v=>Vector3.Distance(v,center)).Average();float desired=Mathf.Lerp(.065f,.0312f,Mathf.Clamp01(rings[ring]));
                    maxRadiusError=Mathf.Max(maxRadiusError,Mathf.Abs(radius-desired));
                }
                File.AppendAllText("ClientExports/YanYana/Reports/connected-fire-sleeves.txt","Rendered skin difference="+renderedError+" radiusError="+maxRadiusError+"\n");
                if(renderedError>.001f||maxRadiusError>.003f||Quaternion.Angle(sleeve.bones[0].rotation,sleeve.bones[1].rotation)>.25f)throw new InvalidOperationException("Forearm cloth collapsed or disagrees with rendered skin.");
                for(int i=0;i<triangles.Length;i+=3)for(int e=0;e<3;e++){int a=triangles[i+e],b=triangles[i+(e+1)%3];var edge=a<b?(a,b):(b,a);edges.TryGetValue(edge,out int count);edges[edge]=count+1;}
                if(edges.Values.Any(n=>n!=2)||mesh.boneWeights.Any(w=>Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.001f))throw new InvalidOperationException("Open seam or invalid forearm binding.");
                File.AppendAllText("ClientExports/YanYana/Reports/connected-fire-sleeves.txt","PASS "+sleeve.name+" closed native surface bound to elbow and glove; no detached primitive.\n");
            }
        }
    }
}
