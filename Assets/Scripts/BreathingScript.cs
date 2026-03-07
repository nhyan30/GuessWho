using UnityEngine;
using DG.Tweening;
public class BreathingScript : MonoBehaviour
{
    public float duration = 1f;
    public float scaleAmount = 1.1f;

    void Start()
    {
        transform.DOScale(Vector3.one * scaleAmount, duration)
           .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }
}
