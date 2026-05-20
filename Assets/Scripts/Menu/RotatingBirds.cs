using UnityEngine;
using UnityEngine.UI;

public class BirdRotation : MonoBehaviour
{
    [SerializeField] private RawImage birds;

    [SerializeField] private float
        rotationAmount = 15f,
        rotationSpeed = 2f;

    void Update()
    {
        float zRotation = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount;

        birds.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
    }
}