using UnityEngine;
using UnityEngine.UI;

public class ScrollingBG : MonoBehaviour
{
    [SerializeField] private RawImage bgImg;
    [SerializeField] private float x, y;

    void Update()
    {
        // Move BG
        bgImg.uvRect = new Rect
            (bgImg.uvRect.position + new Vector2(x, y) * Time.deltaTime,
            bgImg.uvRect.size);
    }
}
