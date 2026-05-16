using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public float speed = 5f;

    public GameObject birdBody, birdWings, birdTail1, birdTail2, birdTail3;

    private Color hostColor = Color.red;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (IsOwner)
        {
            birdBody.GetComponent<Renderer>().material.color = hostColor;
            birdWings.GetComponent<Renderer>().material.color = hostColor;
            birdTail1.GetComponent<Renderer>().material.color = hostColor;
            birdTail2.GetComponent<Renderer>().material.color = hostColor;
            birdTail3.GetComponent<Renderer>().material.color = hostColor;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        transform.Translate(direction * speed * Time.deltaTime);
    }
}
