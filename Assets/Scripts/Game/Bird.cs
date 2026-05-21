using Unity.Netcode;
using UnityEngine;

public class Bird : NetworkBehaviour
{
    [Header("Components")]
    public Rigidbody player;
    public AudioClip flap;

    [Header("Game State")]
    public float score;

    // Movement constants from BirdClassic
    private const float movementSpeed = 2f;   // forward speed
    private const float rotationSpeed = 100f; // horizontal rotation speed
    private const float jumpForce = 10f;      // flap strength
    private const float gravityForce = 9.8f;  // manual gravity (optional)
    private const float deathBarrier = -5f;

    private float playerRotation = 0f;

    private void Awake()
    {
        player = GetComponent<Rigidbody>();
        player.freezeRotation = true;
        player.useGravity = true; // let Rigidbody handle gravity
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

        // Assign camera to owner
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

        HandleRotation();
        HandleJump();
        ApplyGravity();
        CheckDeath();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        MoveForward();
    }

    private void HandleRotation()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Optional vertical rotation, mainly for visuals
        playerRotation += vertical * rotationSpeed * Time.deltaTime;

        // Horizontal rotation
        transform.Rotate(0f, horizontal * rotationSpeed * Time.deltaTime, 0f);

        // Apply rotation to Rigidbody
        player.rotation = Quaternion.Euler(0f, playerRotation, 0f);
    }

    private void MoveForward()
    {
        Vector3 forwardDir = transform.forward; // flip to -transform.forward if model points backwards
        Vector3 movement = forwardDir * movementSpeed * Time.fixedDeltaTime;

        // Move horizontally, leave vertical velocity intact
        Vector3 newPos = player.position + new Vector3(movement.x, 0f, movement.z);
        player.MovePosition(newPos);
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            Vector3 v = player.linearVelocity;
            v.y = 0f; // reset Y before flap
            player.linearVelocity = v;

            player.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            if (flap != null)
                AudioSource.PlayClipAtPoint(flap, transform.position);
        }
    }

    private void ApplyGravity()
    {
        // Optional manual gravity
        //player.AddForce(Vector3.down * gravityForce, ForceMode.Acceleration);
        // Using Rigidbody gravity by default
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

            MeshRenderer meshRenderer = collision.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.enabled = true;
        }

        if (collision.collider.name == "TopHalf" || collision.collider.name == "BottomHalf")
            GameOver();
    }

    private void GameOver()
    {
        gameObject.SetActive(false);
    }
}