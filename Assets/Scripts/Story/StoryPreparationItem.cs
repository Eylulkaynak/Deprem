using System.Collections;
using UnityEngine;

namespace Deprem.Story
{
    public enum StoryPreparationCategory
    {
        Signal = 0,
        Food = 1,
        Health = 2,
        Warmth = 3
    }

    [DisallowMultipleComponent]
    public sealed class StoryPreparationItem : MonoBehaviour
    {
        [Header("Decision")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private StoryPreparationCategory category;
        [SerializeField] private bool recommended;
        [SerializeField] private StoryFlag storyFlag = StoryFlag.None;

        [Header("Dialogue")]
        [TextArea(2, 4)] [SerializeField] private string childLine;
        [TextArea(2, 5)] [SerializeField] private string parentLine;

        [Header("Scene References")]
        [SerializeField] private StoryPreparationDirector director;
        [SerializeField] private StoryInteractable interactable;
        [SerializeField] private GameObject sourceRoot;
        [SerializeField] private GameObject packedVisual;
        [SerializeField] private GameObject consequenceRoot;
        [SerializeField] private DraggableItem legacyBagMotion;
        [SerializeField] private bool hideSourceWhenUnavailable;
        [SerializeField] private float stageDelay;

        private Coroutine placementRoutine;
        private Coroutine stagingRoutine;
        private bool enableInteractionAfterStaging;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public StoryPreparationCategory Category => category;
        public bool Recommended => recommended;
        public StoryFlag Flag => storyFlag;
        public string ChildLine => childLine;
        public string ParentLine => parentLine;
        public StoryInteractable Interactable => interactable;
        public GameObject ConsequenceRoot => consequenceRoot;
        public DraggableItem LegacyBagMotion => legacyBagMotion;

        public void Select()
        {
            director?.ResolveChoice(this);
        }

        public void SetAvailable(bool value)
        {
            if (sourceRoot != null)
            {
                if (hideSourceWhenUnavailable)
                    sourceRoot.SetActive(value);
                else if (!sourceRoot.activeSelf && value)
                    sourceRoot.SetActive(true);
            }
            interactable?.SetAvailable(value);
        }

        public void Restore(bool accepted)
        {
            if (stagingRoutine != null)
            {
                StopCoroutine(stagingRoutine);
                stagingRoutine = null;
            }
            if (placementRoutine != null)
            {
                StopCoroutine(placementRoutine);
                placementRoutine = null;
            }
            if (packedVisual != null)
                packedVisual.SetActive(accepted);
            if (consequenceRoot != null)
                consequenceRoot.SetActive(false);
            if (sourceRoot != null)
                sourceRoot.SetActive(!accepted);
            if (!accepted)
            {
                if (legacyBagMotion != null && legacyBagMotion.gameObject.activeInHierarchy)
                    legacyBagMotion.ResetInstant();
                interactable?.ResetInteraction();
            }
            interactable?.SetAvailable(false);
        }

        public void StageForPacking()
        {
            BeginStaging(true);
        }

        public void StageForSelection()
        {
            BeginStaging(false);
        }

        private void BeginStaging(bool enableAfterStaging)
        {
            if (packedVisual != null && packedVisual.activeSelf)
                return;

            if (stagingRoutine != null)
                StopCoroutine(stagingRoutine);
            enableInteractionAfterStaging = enableAfterStaging;
            if (sourceRoot != null)
                sourceRoot.SetActive(true);
            Animation stageAnimation = sourceRoot != null
                ? sourceRoot.GetComponent<Animation>()
                : null;
            if (stageAnimation != null && stageAnimation.clip != null)
            {
                stageAnimation.Rewind();
                stageAnimation.Play();
            }
            interactable?.ResetInteraction();
            interactable?.SetAvailable(false);
            stagingRoutine = StartCoroutine(FinishStaging());
        }

        public void Accept()
        {
            if (stagingRoutine != null)
            {
                StopCoroutine(stagingRoutine);
                stagingRoutine = null;
            }
            if (legacyBagMotion != null && legacyBagMotion.SendToBag())
            {
                if (placementRoutine != null)
                    StopCoroutine(placementRoutine);
                placementRoutine = StartCoroutine(FinishPhysicalPlacement());
                return;
            }

            ShowPackedState();
        }

        private IEnumerator FinishStaging()
        {
            if (stageDelay > 0f)
                yield return new WaitForSeconds(stageDelay);
            stagingRoutine = null;
            if (enableInteractionAfterStaging)
                interactable?.SetAvailable(true);
        }

        public void Reject()
        {
            if (packedVisual != null)
                packedVisual.SetActive(false);
            interactable?.SetAvailable(false);
        }

        private IEnumerator FinishPhysicalPlacement()
        {
            yield return new WaitForSeconds(legacyBagMotion.BagEntryDuration + 0.04f);
            placementRoutine = null;
            ShowPackedState();
        }

        private void ShowPackedState()
        {
            if (packedVisual != null)
                packedVisual.SetActive(true);
            if (sourceRoot != null)
                sourceRoot.SetActive(false);
        }
    }
}
