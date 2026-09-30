// Records actual rendered camera movement in the Editor; excluded from the player.
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Cinemachine;

namespace YanYana.Editor
{
    public static class YanYanaCameraMotionQA
    {
        static StringBuilder rows;
        static Vector3 position;
        static Quaternion rotation;
        static double lastTime;
        static string lastCamera;
        static bool ortho;
        static int frames, projectionChanges, jumps;
        static float maxStep, maxAngle;
        static string filename;
        [MenuItem("Tools/Yan Yana/QA/Start Rendered Camera Trace")]
        static void Start()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Start Play first.");
            RenderPipelineManager.endCameraRendering -= Frame;
            rows=new StringBuilder("frame,time,dt,camera,blending,ortho,x,y,z,size,step,rotationStep\n");
            frames=projectionChanges=jumps=0;maxStep=maxAngle=0;lastTime=0;
            filename="ClientExports/YanYana/Reports/camera-motion-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            RenderPipelineManager.endCameraRendering+=Frame;
        }
        static void Frame(ScriptableRenderContext context, Camera cam)
        {
            if (!EditorApplication.isPlaying || cam!=Camera.main) return;
            var brain=cam.GetComponent<CinemachineBrain>();
            string name=brain.ActiveBlend?.CamB?.Name??brain.ActiveVirtualCamera?.Name??"none";
            double now=Time.realtimeSinceStartupAsDouble,dt=lastTime==0?0:now-lastTime;
            float step=lastTime==0?0:Vector3.Distance(position,cam.transform.position);
            float angle=lastTime==0?0:Quaternion.Angle(rotation,cam.transform.rotation);
            bool titleChange=name=="AD · mahalle açılış kadrajı"||lastCamera=="AD · mahalle açılış kadrajı";
            if(lastTime!=0&&!titleChange)
            {
                if(ortho!=cam.orthographic)projectionChanges++;
                if(dt<.09&&(step>.75f||angle>12f))jumps++;
                if(dt<.09){maxStep=Mathf.Max(maxStep,step);maxAngle=Mathf.Max(maxAngle,angle);}
            }
            rows.AppendFormat(CultureInfo.InvariantCulture,"{0},{1:F5},{2:F5},{3},{4},{5},{6:F5},{7:F5},{8:F5},{9:F5},{10:F5},{11:F5}\n",frames++,now,dt,name.Replace(',',' '),brain.IsBlending,cam.orthographic,cam.transform.position.x,cam.transform.position.y,cam.transform.position.z,cam.orthographicSize,step,angle);
            lastTime=now;lastCamera=name;position=cam.transform.position;rotation=cam.transform.rotation;ortho=cam.orthographic;
        }
        [MenuItem("Tools/Yan Yana/QA/Stop Rendered Camera Trace")]
        static void Stop()
        {
            RenderPipelineManager.endCameraRendering-=Frame;
            if(rows==null)return;
            File.WriteAllText(filename+".csv",rows.ToString());
            File.WriteAllText("ClientExports/YanYana/Reports/camera-motion-latest.txt",$"Frames={frames}\nProjectionChanges={projectionChanges}\nAbruptRenderedSteps={jumps}\nMaxStepUnder90ms={maxStep.ToString(CultureInfo.InvariantCulture)}\nMaxRotationUnder90ms={maxAngle.ToString(CultureInfo.InvariantCulture)}\nCsv={filename}.csv\nScope=Actual Editor render frames; title/world cut and frames above 90 ms are excluded from abrupt-step count. Device FPS is not measured.\n");
            rows=null;
        }
    }
}
