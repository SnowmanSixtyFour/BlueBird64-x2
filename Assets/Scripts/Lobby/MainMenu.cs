using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button
        onlineButton,
        classicButton,
        quitButton;

    void Start()
    {
        onlineButton.onClick.AddListener(OnlineButtonClicked);
        classicButton.onClick.AddListener(ClassicButtonClicked);
        quitButton.onClick.AddListener(QuitButtonClicked);
    }

    private void OnlineButtonClicked()
    {
        SceneManager.LoadScene("Multiplayer");
    }

    private void ClassicButtonClicked()
    {
        SceneManager.LoadScene("Classic");
    }

    private void QuitButtonClicked()
    {
        Application.Quit();
    }
}
