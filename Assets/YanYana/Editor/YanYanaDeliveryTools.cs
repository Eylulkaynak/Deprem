using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YanYana.Editor
{
    public static class YanYanaDeliveryTools
    {
        static RecorderController recording;
        static DateTime started;
        const string ProtectionRoot=".codex_tmp/yanyana_implementation/build-working-copy";
        static double nextRestoreAt;
        static readonly HashSet<string> restoredPaths=new HashSet<string>();

        [InitializeOnLoadMethod]
        static void RecoverPendingBuildProtection()
        {
            if(!File.Exists(ProtectionRoot+"/pending"))return;
            nextRestoreAt=EditorApplication.timeSinceStartup+1;
            EditorApplication.update-=RestoreBuildWorkingCopies;
            EditorApplication.update+=RestoreBuildWorkingCopies;
        }
        static void RestoreBuildWorkingCopies()
        {
            if(EditorApplication.timeSinceStartup<nextRestoreAt||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer)return;
            nextRestoreAt=EditorApplication.timeSinceStartup+1;
            if(!File.Exists(ProtectionRoot+"/pending")){EditorApplication.update-=RestoreBuildWorkingCopies;return;}
            var paths=File.ReadAllLines(ProtectionRoot+"/manifest.txt");var pending=new List<string>();
            for(int index=0;index<paths.Length;index++)
            {
                string path=paths[index];var original=File.ReadAllBytes(ProtectionRoot+"/files/"+index.ToString("D5")+".bin");
                if(File.Exists(path)&&File.ReadAllBytes(path).SequenceEqual(original))continue;
                try
                {
                    // Replacing a file avoids truncating Unity's memory-mapped settings file.
                    string staged=ProtectionRoot+"/staged-"+index.ToString("D5")+".tmp";File.WriteAllBytes(staged,original);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    if(File.Exists(path))File.Replace(staged,path,null,true);else File.Move(staged,path);
                    restoredPaths.Add(path);
                }
                catch(IOException){pending.Add(path);}
                catch(UnauthorizedAccessException){pending.Add(path);}
            }
            File.WriteAllText("ClientExports/YanYana/Reports/build-preservation.txt","Exact pre-build working copies protected="+paths.Length+"\nStatus="+(pending.Count==0?"Complete":"Waiting for Unity file locks")+"\nRestored after build="+restoredPaths.Count+"\n"+string.Join("\n",restoredPaths)+"\nPending="+string.Join(", ",pending)+"\nFull baseline audit is separate.\n");
            if(pending.Count!=0)return;
            File.Delete(ProtectionRoot+"/pending");EditorApplication.update-=RestoreBuildWorkingCopies;AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Yan Yana/Delivery/Start Normal Speed Recording")]
        public static void StartRecording()
        {
            if(!EditorApplication.isPlaying||SceneManager.GetActiveScene().path!=YanYanaAdventureBuilder.ScenePath)throw new InvalidOperationException("Play only the new adventure first.");
            if(recording!=null&&recording.IsRecording())throw new InvalidOperationException("A recording is already active.");
            Directory.CreateDirectory("ClientExports/YanYana/Recordings");
            var settings=ScriptableObject.CreateInstance<RecorderControllerSettings>();settings.SetRecordModeToManual();settings.FrameRate=30;settings.CapFrameRate=true;settings.FrameRatePlayback=FrameRatePlayback.Variable;settings.ExitPlayMode=false;
            var movie=ScriptableObject.CreateInstance<MovieRecorderSettings>();movie.name="Yan Yana actual Game View";movie.Enabled=true;movie.CaptureAudio=true;
            movie.ImageInputSettings=new GameViewInputSettings{OutputWidth=540,OutputHeight=960};
            movie.OutputFile=Path.GetFullPath("ClientExports/YanYana/Recordings/physical-playthrough-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            settings.AddRecorderSettings(movie);recording=new RecorderController(settings);recording.PrepareRecording();if(!recording.StartRecording())throw new InvalidOperationException("Recorder failed to start.");started=DateTime.UtcNow;
        }
        [MenuItem("Tools/Yan Yana/Delivery/Stop Recording")]
        public static void StopRecording()
        {
            if(recording==null)return;recording.StopRecording();File.AppendAllText("ClientExports/YanYana/Reports/recordings.txt","Unaccelerated Game View recording, variable frame rate, 30 FPS cap. UTC start="+started.ToString("O")+" wall seconds="+(DateTime.UtcNow-started).TotalSeconds.ToString("F1")+". This records technical play, not target-age user validation.\n");recording=null;
        }
        [MenuItem("Tools/Yan Yana/Delivery/Build Windows Adventure Only")]
        public static void BuildWindows()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            File.WriteAllText("ClientExports/YanYana/Reports/windows-build.txt","Build starting at "+DateTime.UtcNow.ToString("O"));
            // delayCall depends on an Inspector repaint and can remain pending in a background Editor.
            // The native menu already runs on the main thread; build synchronously and retain the guard.
            BuildWindowsNow();
        }
        static void BuildWindowsNow()
        {
            string folder="ClientExports/YanYana/Windows";Directory.CreateDirectory(folder);
            // Explicit scene list; never changes the original EditorBuildSettings scene list.
            var options=new BuildPlayerOptions{scenes=new[]{YanYanaAdventureBuilder.ScenePath},locationPathName=folder+"/YanYana.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None};
            // URP/TMP build preprocessors save shared assets and upgrade player settings.
            // Snapshot exact working bytes (including the user's uncommitted edits), not HEAD.
            var extensions=new HashSet<string>{".asset",".unity",".prefab",".mat",".controller",".meta"};
            var protectedFiles=Directory.EnumerateFiles("Assets","*",SearchOption.AllDirectories)
                .Where(x=>!x.Replace('\\','/').StartsWith("Assets/YanYana/",StringComparison.Ordinal)&&extensions.Contains(Path.GetExtension(x)))
                .Concat(Directory.EnumerateFiles("ProjectSettings","*",SearchOption.AllDirectories))
                .ToDictionary(x=>x,File.ReadAllBytes);
            if(File.Exists(ProtectionRoot+"/pending"))throw new InvalidOperationException("Complete the prior build's file restoration first.");
            // Flat numbered copies avoid Mono/Windows path limits on deeply nested source assets.
            Directory.CreateDirectory(ProtectionRoot+"/files");int fileIndex=0;
            foreach(var pair in protectedFiles){File.WriteAllBytes(ProtectionRoot+"/files/"+fileIndex.ToString("D5")+".bin",pair.Value);fileIndex++;}
            File.WriteAllLines(ProtectionRoot+"/manifest.txt",protectedFiles.Keys);File.WriteAllText(ProtectionRoot+"/pending","Restore exact working copies after the native build and any domain reload.");restoredPaths.Clear();
            try
            {
                var report=BuildPipeline.BuildPlayer(options);var s=report.summary;
                File.WriteAllText("ClientExports/YanYana/Reports/windows-build.txt","Result="+s.result+"\nScenes="+YanYanaAdventureBuilder.ScenePath+"\nBytes="+s.totalSize+"\nErrors="+s.totalErrors+"\nWarnings="+s.totalWarnings+"\nDuration="+s.totalTime+"\n");
                if(s.result!=BuildResult.Succeeded)throw new InvalidOperationException("The isolated Windows build failed. See the report and console.");
            }
            finally
            {
                // GraphicsSettings can remain memory mapped until this native build callback returns.
                // Restore next Editor updates; a disk checkpoint survives a build-triggered reload.
                RecoverPendingBuildProtection();
            }
        }
    }
}
