using UnityEngine;

public class TouchManager : MonoBehaviour
{
    public Camera cam;

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                WobbleFixer w = hit.collider.GetComponent<WobbleFixer>();
                if (w != null) w.OnTapped();

                HazardMover m = hit.collider.GetComponent<HazardMover>();
                if (m != null) m.OnTapped();

                DrillFixSequence d = hit.transform.GetComponentInParent<DrillFixSequence>();
                if (d != null) { d.OnTapped(); return; }
            }
        }
    }
}