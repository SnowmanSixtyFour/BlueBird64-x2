using Unity.Netcode;
using UnityEngine;

public class Bird : NetworkBehaviour
{
    [Header("Components")]
    public Rigidbody player;
    public AudioClip flap;

    [Header("Game State")]
    public float score;

    private float targetRotationY = 0f;

    // Movement constants
    private const float forwardSpeed = 6f;
    private const float rotationSpeed = 100f; // horizontal rotation
    private const float jumpForce = 8f;
    private const float deathBarrier = -5f;

    private void Awake()
    {
        player = GetComponent<Rigidbody>();
        player.freezeRotation = true;
        player.useGravity = true;
        player.detectCollisions = true;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            player.isKinematic = true;
            enabled = false;
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraClassic camScript = mainCam.GetComponent<CameraClassic>();
            if (camScript != null)
                camScript.target = transform;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleJump();
        HandleRotation();
        CheckDeath();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        MoveForward();
        ApplySmoothRotation();
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            // Apply upward impulse without resetting vertical velocity
            player.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            if (flap != null)
                AudioSource.PlayClipAtPoint(flap, transform.position);
        }
    }

    private void HandleRotation()
    {
        float horizontal = Input.GetAxis("Horizontal");
        // Increment target rotation smoothly
        targetRotationY += horizontal * rotationSpeed * Time.deltaTime;
    }

    private void ApplySmoothRotation()
    {
        Quaternion currentRot = player.rotation;
        Quaternion desiredRot = Quaternion.Euler(0f, targetRotationY, 0f);
        player.rotation = Quaternion.Slerp(currentRot, desiredRot, 5f * Time.fixedDeltaTime);
    }

    private void MoveForward()
    {
        Vector3 forwardMovement = transform.forward * forwardSpeed * Time.fixedDeltaTime;
        player.MovePosition(player.position + new Vector3(forwardMovement.x, 0f, forwardMovement.z));
    }

    private void CheckDeath()
    {
        if (player.position.y < deathBarrier)
            GameOver();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;

        if (collision.collider.name == "ScoreIncreaser")
        {
            score++;
            collision.collider.isTrigger = true;

            MeshRenderer renderer = collision.gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = true;
        }

        if (collision.collider.name == "TopHalf" || collision.collider.name == "BottomHalf")
            GameOver();
    }

    private void GameOver()
    {
        gameObject.SetActive(false);
    }
}