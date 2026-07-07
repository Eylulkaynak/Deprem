using UnityEngine;

public class SafeArea : MonoBehaviour
{
    private bool playerInside;

    private void Update()
    {
        if (!playerInside)
            return;

        switch (EarthquakeGameManager.Instance.CurrentStep)
        {
            case 0:
                InteractionUI.Instance.Show("[E]\nÇÖK");
                break;

            case 1:
                InteractionUI.Instance.Show("[E]\nKAPAN");
                break;

            case 2:
                InteractionUI.Instance.Show("[E]\nTUTUN");
                break;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            EarthquakeGameManager.Instance.OnCorrectAction(
                EarthquakeGameManager.Instance.CurrentStep
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        InteractionUI.Instance.Hide();
    }
}