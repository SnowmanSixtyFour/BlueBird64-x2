using UnityEngine;

public class CameraClassic : MonoBehaviour
{
    [Header("Target")]
    public Transform target;               // Bird to follow

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0f, 2f, -5f); // Relative position to the bird
    public float followSpeed = 5f;         // Smooth follow speed
    public float rotationSpeed = 2f;       // Optional: rotation smoothing

    private void LateUpdate()
    {
        if (target == null) return;

        // Smooth position
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Optional: smooth rotation to look at the bird
        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
    }
}