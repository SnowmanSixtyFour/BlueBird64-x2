using UnityEngine;
using UnityEngine.UI;

public class RotatingBirds : MonoBehaviour
{
    [SerializeField] private RawImage birds;

    void Update()
    {
        // Rotate Birds
        Vector3 rotation = new Vector3(2, 2, 8);
        birds.transform.Rotate(rotation, Space.Self);
        birds.transform.Rotate(-rotation, Space.Self);
    }
}
