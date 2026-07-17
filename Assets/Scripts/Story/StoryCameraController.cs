using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace Deprem.Story
{
    [Serializable]
    public struct StoryCameraBinding
    {
        public StoryCameraZoneId zone;
        public CinemachineCamera camera;
    }

    [DisallowMultipleComponent]
    public sealed class StoryCameraController : MonoBehaviour
    {
        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private StoryCameraBinding[] cameras;
        [SerializeField] private StoryCameraZoneId initialZone = StoryCameraZoneId.RoomOverview;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int standbyPriority;
        [SerializeField, Range(0f, 1f)] private float normalShakeGain = 0.12f;
        [SerializeField, Range(0f, 1f)] private float reducedShakeGain = 0.03f;
        [SerializeField, Min(0f)] private float transitionInputPadding = 0.1f;

        private CinemachineBlendDefinition defaultBlend;
        private bool reducedShake;
        private bool impulseEnabled = true;
        private Coroutine navigationBlockRoutine;
        private bool worldNavigationBlocked;
        private bool hasActiveZone;
        private StoryCameraZoneId activeZone;

        public bool WorldNavigationBlocked => worldNavigationBlocked;
        public StoryCameraZoneId ActiveZone => activeZone;

        private void Awake()
        {
            if (brain != null)
                defaultBlend = brain.DefaultBlend;
            SetReducedShake(PlayerPrefs.GetInt("story.reduceShake", 0) == 1);
            ActivateZone(initialZone, true);
        }

        public void ActivateZone(StoryCameraZoneId zone)
        {
            ActivateZone(zone, false);
        }

        public void ActivateZone(StoryCameraZoneId zone, bool instant)
        {
            if (cameras == null)
                return;

            bool hasValidCamera = false;
            foreach (StoryCameraBinding binding in cameras)
            {
                if (binding.zone == zone && binding.camera != null)
                {
                    hasValidCamera = true;
                    break;
                }
            }
            if (!hasValidCamera)
            {
                Debug.LogError($"Story camera zone '{zone}' has no authored camera binding. Keeping '{activeZone}' active.", this);
                return;
            }

            bool zoneChanged = !hasActiveZone || activeZone != zone;
            foreach (StoryCameraBinding binding in cameras)
            {
                if (binding.camera != null)
                    binding.camera.Priority = binding.zone == zone ? activePriority : standbyPriority;
            }

            activeZone = zone;
            hasActiveZone = true;
            if (zoneChanged)
            {
                if (instant || !Application.isPlaying)
                    EndNavigationBlock();
                else
                    BeginNavigationBlock();
            }

            if (instant && brain != null && isActiveAndEnabled && Application.isPlaying)
                StartCoroutine(CutForOneFrame());
        }

        private void BeginNavigationBlock()
        {
            if (navigationBlockRoutine != null)
                StopCoroutine(navigationBlockRoutine);

            float blendSeconds = Mathf.Max(0f, defaultBlend.Time);
            worldNavigationBlocked = true;
            navigationBlockRoutine = StartCoroutine(ReleaseNavigationAfterBlend(blendSeconds + transitionInputPadding));
        }

        private IEnumerator ReleaseNavigationAfterBlend(float duration)
        {
            float releaseAt = Time.unscaledTime + Mathf.Max(0.05f, duration);
            while (Time.unscaledTime < releaseAt)
                yield return null;

            navigationBlockRoutine = null;
            worldNavigationBlocked = false;
        }

        private void EndNavigationBlock()
        {
            if (navigationBlockRoutine != null)
            {
                StopCoroutine(navigationBlockRoutine);
                navigationBlockRoutine = null;
            }
            worldNavigationBlocked = false;
        }

        public void SetReducedShake(bool reduced)
        {
            reducedShake = reduced;
            ApplyImpulseGain();
        }

        public void SetImpulseEnabled(bool enabled)
        {
            impulseEnabled = enabled;
            ApplyImpulseGain();
        }

        private void ApplyImpulseGain()
        {
            float gain = impulseEnabled ? (reducedShake ? reducedShakeGain : normalShakeGain) : 0f;
            if (cameras == null)
                return;

            foreach (StoryCameraBinding binding in cameras)
            {
                if (binding.camera == null)
                    continue;
                CinemachineImpulseListener listener = binding.camera.GetComponent<CinemachineImpulseListener>();
                if (listener != null)
                {
                    listener.enabled = impulseEnabled;
                    listener.Gain = gain;
                }
            }
        }

        private IEnumerator CutForOneFrame()
        {
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            yield return null;
            brain.DefaultBlend = defaultBlend;
        }

        private void OnDisable()
        {
            EndNavigationBlock();
        }
    }
}
