using UnityEngine;

public class Bolum5Hazard : MonoBehaviour
{
    [TextArea]
    public string warningMessage;

    public void ShowWarning()
    {
        Bolum5UIManager.Instance.UyariGoster(warningMessage);
    }
}