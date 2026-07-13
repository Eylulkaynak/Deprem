using UnityEngine;

public class Bolum5QuestionTrigger : MonoBehaviour
{
    public int questionID;

    private bool used;

    private void OnTriggerEnter(Collider other)
    {
        if (used)
            return;

        if (!other.CompareTag("Player"))
            return;

        used = true;

        Bolum5QuestionManager.Instance.OpenQuestion(questionID);
    }
}