using System.IO.Pipes;
using Unity.VisualScripting;
using UnityEngine;

public class Bird : MonoBehaviour
{
    public Rigidbody player;
    public AudioClip flap;

    public float
        // Game Properties
        score, // Score

        // Movement
        movementSpeed, // Movement Speed

        // Rotation
        rotationSpeed, // Amount to Rotate Player by

        // Jump
        gravity, // Gravity Strength
        jump, // Strength

        // Other
        deathBarrier; // Game Over Y Position

    /*
     * Bird Speeds
     * 
     * PC:
     * Gravity - 2
     * Jump - 10
     * 
     * WEB:
     * Gravity - 8
     * Jump - 11
     */

    private float
        playerRotation = 0; // Current Rotation

    public void Awake()
    {
        // Initialize Player
        player = GetComponent<Rigidbody>();
        player.detectCollisions = true;
    }

    public void Update()
    {
        // --- Movement ---

        // Get Input
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Horizontal");

        // Calculate Movement
        playerRotation += y * rotationSpeed;

        // Apply Move
        player.MovePosition(player.position + transform.forward * movementSpeed * Time.deltaTime);

        // Rotate Player
        player.rotation = Quaternion.Euler(0, playerRotation, 0);

        // Jump
        if (Input.GetButtonDown("Jump"))
        {
            // Add Jump Force
            player.AddForce(Vector3.up * jump, ForceMode.Impulse);

            // Play SFX
            AudioSource.PlayClipAtPoint(flap, transform.position);
        }

        // Gravity
        player.AddForce(Vector3.down * gravity, ForceMode.Force);

        // Restart Game
        if (player.position.y < 0)
        {
            GameOver();
        }
    }

    public void OnCollisionEnter (Collision collision)
    {
        // Increase Score
        if (collision.collider.name == "ScoreIncreaser")
        {
            score++;

            // Disable Score Increaser (by setting to trigger)
            collision.collider.isTrigger = true;

            // Make Score Increaser Visible
            collision.gameObject.GetComponent<MeshRenderer>().enabled = true; // Enable Mesh Renderer
        }

        // Game Over
        if (collision.collider.name == "TopHalf" || collision.collider.name == "BottomHalf")
        {
            GameOver();
        }
    }

    // On Game Over
    private void GameOver()
    {
        // Go back to Title
        UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
    }
}
