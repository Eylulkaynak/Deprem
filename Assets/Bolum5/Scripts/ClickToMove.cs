using UnityEngine;

public class ClickToMove : MonoBehaviour
{
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Bolum5QuestionManager.Instance != null &&
            Bolum5QuestionManager.Instance.IsQuestionOpen)
            return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            HandleClick(Input.mousePosition);
#else
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleClick(Input.GetTouch(0).position);
        }
#endif
    }

 private void HandleClick(Vector3 screenPos)
{
    Ray ray = cam.ScreenPointToRay(screenPos);

    if (!Physics.Raycast(ray, out RaycastHit hit))
        return;

    // Önce tehlikeli obje mi?
    Bolum5Hazard hazard = hit.collider.GetComponent<Bolum5Hazard>();

    if (hazard != null)
    {
        hazard.ShowWarning();
        return;
    }

    // Sonra normal hareket
    Bolum5PlayerController.Instance.MoveTo(hit.point);
}
}