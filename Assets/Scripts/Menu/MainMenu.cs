using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button
        onlineButton,
        classicButton,
        creditsButton,
        quitButton,

        gameJamButton;

    void Start()
    {
        onlineButton.onClick.AddListener(OnlineButtonClicked);
        classicButton.onClick.AddListener(ClassicButtonClicked);
        creditsButton.onClick.AddListener(CreditsButtonClicked);
        quitButton.onClick.AddListener(QuitButtonClicked);

        gameJamButton.onClick.AddListener(GameJamButtonClicked);
    }

    private void OnlineButtonClicked()
    {
        SceneManager.LoadScene("Multiplayer");
    }

    private void ClassicButtonClicked()
    {
        SceneManager.LoadScene("Classic");
    }

    private void CreditsButtonClicked()
    {
        SceneManager.LoadScene("Credits");
    }

    private void QuitButtonClicked()
    {
        Application.Quit();
    }

    private void GameJamButtonClicked()
    {
        Application.OpenURL("https://itch.io/jam/make-literally-anything-jam-2026");
    }
}
