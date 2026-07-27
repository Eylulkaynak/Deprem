using System.IO;
using System.Linq;
using Deprem.Story;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public sealed class StoryAudioIntegrationTests
{
    private const string LicensedRoot = "Assets/Story/Audio/ThirdParty/rubberduck_sfx100v2";

    [Test]
    public void LicensedSfxPack_HasCc0NoticeAndAllOriginalClips()
    {
        Assert.That(File.Exists(LicensedRoot + "/CC0_NOTICE.md"), Is.True);
        Assert.That(
            AssetDatabase.FindAssets("t:AudioClip", new[] { LicensedRoot }),
            Has.Length.EqualTo(100));
    }

    [TestCase("Assets/Scenes/Story_01_RebuildPreview.unity", "Story01_ObjectInteractionAudio")]
    [TestCase("Assets/Scenes/Story_02_RebuildPreview.unity", "Story02_ObjectInteractionAudio")]
    [TestCase("Assets/Scenes/Story_03_RebuildPreview.unity", "Story03_ObjectInteractionAudio")]
    [TestCase("Assets/Scenes/Story_04_RebuildPreview.unity", "Story04_ObjectInteractionAudio")]
    public void RebuildPreview_EveryWorldInteractionHasSceneAuthoredSpatialAudio(
        string scenePath,
        string audioRootName)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        StoryInteractable[] interactions = Object.FindObjectsByType<StoryInteractable>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        Assert.That(interactions.Length, Is.GreaterThan(5), scenePath);

        Transform audioRoot = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == audioRootName);
        Assert.That(audioRoot, Is.Not.Null, scenePath);

        AudioSource[] sources = audioRoot.GetComponentsInChildren<AudioSource>(true);
        Assert.That(sources, Has.Length.EqualTo(interactions.Length), scenePath);
        Assert.That(sources.All(source => source.clip != null), Is.True, scenePath);
        Assert.That(sources.All(source => source.spatialBlend > 0f), Is.True, scenePath);
        Assert.That(
            sources.All(source =>
                AssetDatabase.GetAssetPath(source.clip).StartsWith(LicensedRoot)),
            Is.True,
            scenePath);

        foreach (StoryInteractable interaction in interactions)
        {
            bool hasSceneAudioListener = Enumerable.Range(0, interaction.OnInteracted.GetPersistentEventCount())
                .Any(index =>
                    interaction.OnInteracted.GetPersistentTarget(index) is AudioSource &&
                    interaction.OnInteracted.GetPersistentMethodName(index) == "Play");
            Assert.That(
                hasSceneAudioListener,
                Is.True,
                scenePath + " / " + interaction.InteractionId);
        }
    }

    [TestCase("Assets/Scenes/Story_01_RebuildPreview.unity")]
    [TestCase("Assets/Scenes/Story_02_RebuildPreview.unity")]
    [TestCase("Assets/Scenes/Story_03_RebuildPreview.unity")]
    [TestCase("Assets/Scenes/Story_04_RebuildPreview.unity")]
    public void RebuildPreview_HasLicensedLoopingAmbience(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        AudioSource[] ambience = Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .Where(source =>
                source.loop &&
                source.playOnAwake &&
                source.clip != null &&
                AssetDatabase.GetAssetPath(source.clip).StartsWith(LicensedRoot))
            .ToArray();

        Assert.That(ambience, Is.Not.Empty, scenePath);
    }

    [Test]
    public void QuakeRebuildTimeline_HasAuthoredImpactAndDebrisAudioCues()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/Story_03_RebuildPreview.unity",
            OpenSceneMode.Single);

        PlayableDirector director = Object.FindObjectsByType<PlayableDirector>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .First(candidate => candidate.name == "EarthquakeTimeline_Rebuild_48s");
        TimelineAsset timeline = director.playableAsset as TimelineAsset;
        Assert.That(timeline, Is.Not.Null);

        AudioTrack audioTrack = timeline.GetOutputTracks()
            .OfType<AudioTrack>()
            .FirstOrDefault(track => track.name.Contains("Darbe"));
        Assert.That(audioTrack, Is.Not.Null);
        Assert.That(audioTrack.GetClips().Count(), Is.EqualTo(6));
        Assert.That(
            audioTrack.GetClips()
                .Select(clip => clip.asset as AudioPlayableAsset)
                .All(asset =>
                    asset != null &&
                    asset.clip != null &&
                    AssetDatabase.GetAssetPath(asset.clip).StartsWith(LicensedRoot)),
            Is.True);
    }
}
