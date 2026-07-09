using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    Vector3 startPos;

    void Awake()
    {
        Instance=this;
        startPos=transform.localPosition;
    }

    public IEnumerator Shake(float duration,float power)
    {
        float t=0;
        while(t<duration)
        {
            transform.localPosition=startPos+Random.insideUnitSphere*power;
            t+=Time.deltaTime;
            yield return null;
        }
        transform.localPosition=startPos;
    }
}
