using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class CameraClassic : MonoBehaviour
{
    public Transform
        target; // Pivot Point

    private float
        rotationSpeed = 0.25f,
        cameraRotation = 0;

    public void Update()
    {
        // Look at Pivot Point of Player
        transform.LookAt(target.transform);

        // Get Input
        cameraRotation = Input.GetAxis("Vertical");

        // Rotate Camera
        transform.RotateAround(target.position, new Vector3(0.0f, 1.0f, 0.0f), cameraRotation * rotationSpeed);
    }
}
