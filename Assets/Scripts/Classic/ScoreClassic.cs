using UnityEngine;
using TMPro;

public class ScoreClassic : MonoBehaviour
{
    private BirdClassic bird;
    public TextMeshProUGUI text;

    // Update Text
    public void Awake()
    {
        // Get Bird Script
        bird = GameObject.Find("Player").GetComponent<BirdClassic>();
    }
    public void Update()
    {
        // Update Score Text
        text.text = ("Score: " + bird.score);
    }
}
