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

        private Coroutine placementRoutine;

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
            if (sourceRoot != null && !sourceRoot.activeSelf && value)
                sourceRoot.SetActive(true);
            interactable?.SetAvailable(value);
        }

        public void Restore(bool accepted)
        {
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
                legacyBagMotion?.ResetInstant();
                interactable?.ResetInteraction();
            }
            interactable?.SetAvailable(false);
        }

        public void Accept()
        {
            if (legacyBagMotion != null && legacyBagMotion.SendToBag())
            {
                if (placementRoutine != null)
                    StopCoroutine(placementRoutine);
                placementRoutine = StartCoroutine(FinishPhysicalPlacement());
                return;
            }

            ShowPackedState();
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
