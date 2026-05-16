using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button onlineButton;

    void Start()
    {
        onlineButton.onClick.AddListener(onlineButtonClicked);
    }

    private void onlineButtonClicked()
    {
        SceneManager.LoadScene("Lobby");
    }
}
