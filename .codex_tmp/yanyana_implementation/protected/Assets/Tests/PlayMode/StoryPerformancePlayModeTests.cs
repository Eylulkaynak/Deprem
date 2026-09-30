using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public sealed class StoryPerformancePlayModeTests
{
#if UNITY_EDITOR
    private const int WarmupFrames = 90;
    private const int SampleFrames = 240;

    [UnityTest]
    public IEnumerator RebuildPreviews_ReportIdleFrameGcAndRuntimeMemory()
    {
        ProfileResult story03 = default;
        yield return ProfileScene(
            "Assets/Scenes/Story_03_RebuildPreview.unity",
            result => story03 = result);

        ProfileResult story04 = default;
        yield return ProfileScene(
            "Assets/Scenes/Story_04_RebuildPreview.unity",
            result => story04 = result);

        TestContext.Progress.WriteLine(story03.ToString());
        TestContext.Progress.WriteLine(story04.ToString());
        Debug.Log(story03.ToString());
        Debug.Log(story04.ToString());

        Assert.That(story03.gcRecorderValid, Is.True);
        Assert.That(story04.gcRecorderValid, Is.True);
        Assert.That(story03.p95FrameMilliseconds, Is.GreaterThan(0f));
        Assert.That(story04.p95FrameMilliseconds, Is.GreaterThan(0f));
        Assert.That(story03.memoryAfterBytes, Is.GreaterThan(0));
        Assert.That(story04.memoryAfterBytes, Is.GreaterThan(0));
    }

    private static IEnumerator ProfileScene(string scenePath, Action<ProfileResult> completed)
    {
        MonoBehaviour existing = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item != null && item.GetType().Name == "StoryGameManager");
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
            yield return null;
        }

        string savePath = Path.Combine(Application.persistentDataPath, "story-session.json");
        if (File.Exists(savePath))
            File.Delete(savePath);

        Scene loaded = EditorSceneManager.LoadSceneInPlayMode(
            scenePath,
            new LoadSceneParameters(LoadSceneMode.Single));
        Assert.That(loaded.IsValid(), Is.True, scenePath);
        yield return null;

        for (int i = 0; i < WarmupFrames; i++)
            yield return null;

        long[] gcBytes = new long[SampleFrames];
        float[] frameMilliseconds = new float[SampleFrames];
        long memoryBefore = Profiler.GetTotalAllocatedMemoryLong();
        bool recorderValid;
        using (ProfilerRecorder gcRecorder = ProfilerRecorder.StartNew(
                   ProfilerCategory.Memory,
                   "GC Allocated In Frame",
                   1))
        {
            recorderValid = gcRecorder.Valid;
            for (int i = 0; i < SampleFrames; i++)
            {
                yield return null;
                gcBytes[i] = gcRecorder.LastValue;
                frameMilliseconds[i] = Time.unscaledDeltaTime * 1000f;
            }
        }

        long memoryAfter = Profiler.GetTotalAllocatedMemoryLong();
        Array.Sort(frameMilliseconds);
        int percentileIndex = Mathf.Clamp(
            Mathf.CeilToInt(SampleFrames * 0.95f) - 1,
            0,
            SampleFrames - 1);

        int allocatingFrames = 0;
        int consecutive = 0;
        int maxConsecutive = 0;
        long maximumGcBytes = 0;
        for (int i = 0; i < gcBytes.Length; i++)
        {
            maximumGcBytes = Math.Max(maximumGcBytes, gcBytes[i]);
            if (gcBytes[i] > 0)
            {
                allocatingFrames++;
                consecutive++;
                maxConsecutive = Math.Max(maxConsecutive, consecutive);
            }
            else
            {
                consecutive = 0;
            }
        }

        completed(new ProfileResult
        {
            scenePath = scenePath,
            gcRecorderValid = recorderValid,
            allocatingFrames = allocatingFrames,
            maxConsecutiveAllocatingFrames = maxConsecutive,
            maximumGcBytes = maximumGcBytes,
            p95FrameMilliseconds = frameMilliseconds[percentileIndex],
            memoryBeforeBytes = memoryBefore,
            memoryAfterBytes = memoryAfter
        });
    }

    private struct ProfileResult
    {
        internal string scenePath;
        internal bool gcRecorderValid;
        internal int allocatingFrames;
        internal int maxConsecutiveAllocatingFrames;
        internal long maximumGcBytes;
        internal float p95FrameMilliseconds;
        internal long memoryBeforeBytes;
        internal long memoryAfterBytes;

        public override string ToString()
        {
            return
                $"STORY_PERF scene={scenePath} samples={SampleFrames} " +
                $"gcFrames={allocatingFrames} maxGcRun={maxConsecutiveAllocatingFrames} " +
                $"maxGcBytes={maximumGcBytes} p95FrameMs={p95FrameMilliseconds:F2} " +
                $"memoryBeforeMB={memoryBeforeBytes / (1024f * 1024f):F1} " +
                $"memoryAfterMB={memoryAfterBytes / (1024f * 1024f):F1}";
        }
    }
#endif
}
