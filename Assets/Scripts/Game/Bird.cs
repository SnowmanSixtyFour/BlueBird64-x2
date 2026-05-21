using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bird : NetworkBehaviour
{
    [Header("Components")]
    public Rigidbody player;
    public AudioClip flap;

    [Header("Game State")]
    public NetworkVariable<int> score = new NetworkVariable<int>(0);

    private float targetRotationY = 0f;

    // Movement constants
    private const float forwardSpeed = 6f;
    private const float rotationSpeed = 100f; // horizontal rotation
    private const float jumpForce = 8f;
    private const float deathBarrier = 0.5f;

    private float cameraOrbitAngle = 0f; // For W/S camera rotation

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
        HandleCameraOrbit();
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
            player.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            if (flap != null)
                AudioSource.PlayClipAtPoint(flap, transform.position);
        }
    }

    private void HandleRotation()
    {
        float horizontal = Input.GetAxis("Horizontal");
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

    private void HandleCameraOrbit()
    {
        float verticalInput = Input.GetAxis("Vertical"); // W/S keys
        cameraOrbitAngle += verticalInput * 50f * Time.deltaTime; // Adjust speed as needed

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            // Rotate camera around bird on local Y axis
            mainCam.transform.position = player.position + Quaternion.Euler(0f, cameraOrbitAngle, 0f) * new Vector3(0f, 2f, -5f);
            mainCam.transform.LookAt(player.position + Vector3.up * 1f);
        }
    }

    private void CheckDeath()
    {
        if (player.position.y <= deathBarrier)
            GameOver();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;

        if (collision.collider.name == "ScoreIncreaser")
        {
            // Only server updates score for network syncing
            if (IsServer)
                GameManager.score++;

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

        // Only shutdown network if in the Game scene
        if (SceneManager.GetActiveScene().name == "Game")
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                NetworkManager.Singleton.Shutdown();

            // Reset Score
            GameManager.score = 0;

            SceneManager.LoadScene("Title");
        }
    }
}