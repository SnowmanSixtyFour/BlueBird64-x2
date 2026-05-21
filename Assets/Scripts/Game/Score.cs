using TMPro;
using UnityEngine;

public class Score : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private void Update()
    {
        scoreText.text = "Score: " + GameManager.score;
    }
}