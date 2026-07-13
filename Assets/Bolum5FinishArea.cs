using UnityEngine;

public class Bolum5FinishArea : MonoBehaviour
{
    private bool finished = false;

    private void OnTriggerEnter(Collider other)
    {
        if (finished)
            return;

        if (!other.CompareTag("Player"))
            return;

        finished = true;

        Bolum5PlayerController.Instance.StopMovement();

        Bolum5FinishManager.Instance.ShowFinish();
    }
}