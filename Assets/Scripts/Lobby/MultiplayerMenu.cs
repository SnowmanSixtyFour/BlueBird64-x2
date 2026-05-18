using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MultiplayerMenu : MonoBehaviour
{
    private string
        // GameModes
        gamemode1 = "Competition",

        // Maps
        map1 = "Forest";

    private Lobby
        hostLobby,
        joinedLobby;
    private float
        heartbeat, lobbyUpdateTimer;

    private string playerName;

    // Buttons
    [SerializeField] private Button
        backButton,
        exitButton,
        createLobbyButton,
        joinLobbyButton,
        joinViaCodeButton,
        codeBackButton,
        refreshButton,
        randomLobbyButton,
        deleteLobbyButton;
    [SerializeField] private TextMeshProUGUI
        codeInput,
        playerNameInput,
        playerCount,
        lobbyCode;
    [SerializeField] private GameObject
        searchUI,
        joiningLobbyUI,
        joinViaCodeUI,
        hostLobbyUI;

    private async void Awake()
    {
        // Set Variables

        backButton.onClick.AddListener(BackButtonClicked);

        exitButton.onClick.AddListener(ExitButtonClicked);
        createLobbyButton.onClick.AddListener(CreateLobbyClicked);
        refreshButton.onClick.AddListener(RefreshButtonClicked);

        joinLobbyButton.onClick.AddListener(JoinButtonClicked);
        joinViaCodeButton.onClick.AddListener(JoinViaCodeButtonClicked);
        codeBackButton.onClick.AddListener(CodeBackButtonClicked);

        randomLobbyButton.onClick.AddListener(RandomLobbyButtonClicked);

        deleteLobbyButton.onClick.AddListener(DeleteLobbyButtonClicked);

        await UnityServices.InitializeAsync();

        // Login to Multiplayer

        if (!AuthenticationService.Instance.IsSignedIn) // If not already logged in
        {
            // Login

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId);
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Set Player Name
        playerName = RandomName();

        NetworkManager.Singleton.StartHost();
    }

    private string RandomName()
    {
        return "Player " + UnityEngine.Random.Range(100, 999);
    }

    private void JoinButtonClicked()
    {
        searchUI.SetActive(false);
        joinViaCodeUI.SetActive(true);
    }

    private void CodeBackButtonClicked()
    {
        searchUI.SetActive(true);
        joinViaCodeUI.SetActive(false);
    }

    private void JoinViaCodeButtonClicked()
    {
        // Get User Input for Code
        string code = codeInput.text;

        // Attempt to Join Lobby
        try
        {
            JoinLobbyByCode(code);
        }

        // Error (Invalid code)
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void BackButtonClicked()
    {
        SceneManager.LoadScene("Title");
    }

    private void CreateLobbyClicked()
    {
        CreateLobby();
    }

    private void RefreshButtonClicked()
    {
        ListLobbies();
    }

    private void RandomLobbyButtonClicked()
    {
        JoinRandomLobby();
    }

    private void ExitButtonClicked()
    {
        LeaveLobby();
    }

    private void DeleteLobbyButtonClicked()
    {
        DeleteLobby();
    }

    private async void CreateLobby()
    {
        // Create Lobby
        try
        {
            string lobbyName = "Lobby";
            int maxPlayers = 8;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,

                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, gamemode1) },
                    { "Map", new DataObject(DataObject.VisibilityOptions.Public, map1) }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            hostLobby = lobby;
            joinedLobby = hostLobby;

            PrintPlayers(hostLobby);

            Debug.Log("Created lobby \"" + lobbyName + "\" with a limit of " + maxPlayers + " players. ID: " + lobby.Id + " " + lobby.LobbyCode);

            ValidateUsername();
        }

        // Error when Creating Lobby
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void Update()
    {
        // Update UI
        if (joinedLobby != null) // Joined to Lobby
        {
            searchUI.SetActive(false);
            joiningLobbyUI.SetActive(true);

            joinViaCodeUI.SetActive(false);

            if (hostLobby != null) // If Hosting Lobby
            {
                hostLobbyUI.SetActive(true);
            }
            else // If Not Hosting Lobby
            {
                hostLobbyUI.SetActive(false);
            }
        }
        else // Browsing for Lobby
        {
            if (!joinViaCodeUI.activeSelf) searchUI.SetActive(true);
            else searchUI.SetActive(false);

            joiningLobbyUI.SetActive(false);
        }

        // Update Lobby
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();

        // Update UI
        HandleLobbyDisplayText();
    }

    private async void HandleLobbyHeartbeat()
    {
        // Lobby Heartbeat

        if (hostLobby != null)
        {
            heartbeat -= Time.deltaTime;

            if (heartbeat < 0f)
            {
                float heartbeatMax = 15;

                heartbeat = heartbeatMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    private async void HandleLobbyPollForUpdates()
    {
        // Lobby Updates

        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;

            if (lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 1.1f;

                lobbyUpdateTimer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
            }
        }
    }

    private void HandleLobbyDisplayText()
    {
        if (joinedLobby != null)
        {
            // Reset List of Players
            playerCount.text = "";

            // Add Player to List
            foreach (Player player in joinedLobby.Players)
            {
                playerCount.text += player.Data["PlayerName"].Value.ToString() + "\n";
            }

            // Set Lobby Code
            lobbyCode.text = joinedLobby.LobbyCode.ToString();
        }
    }

    private void ValidateUsername()
    {
        // Make sure username cannot be blank

        if (!string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            playerName = playerNameInput.text;
        }

        // Make sure username is not too long
        int maxPlayerNameLength = 10;
        if (playerName.Length > maxPlayerNameLength)
        {
            playerName = playerName.Substring(0, maxPlayerNameLength);
        }

        // Set Username to chosen text
        UpdatePlayerName(playerName);
    }

    private async void ListLobbies()
    {
        // Search for all Lobbies
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            Debug.Log("Lobbies: " + queryResponse.Results.Count);

            foreach (var lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value);
            }
        }

        // Error when Searching
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void JoinLobbyByCode(string lobbyCode)
    {
        // Join Lobby by Code
        try
        {
            ValidateUsername();

            // Set entered code to all caps
            lobbyCode = lobbyCode.ToUpper();

            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode.ToString(), joinLobbyByCodeOptions);

            joinedLobby = lobby;

            Debug.Log("Joined Lobby with code " + lobbyCode);
            PrintPlayers(lobby);

            NetworkManager.Singleton.StartClient();

            HandleLobbyDisplayText();
        }

        // Error when Joining Lobby
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
    }

    private async void JoinRandomLobby()
    {
        ValidateUsername();

        joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

        HandleLobbyDisplayText();
    }

    private void PrintPlayers()
    {
        PrintPlayers(joinedLobby);
    }

    private void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Players in Lobby \"" + lobby.Name + "\", playing in " + lobby.Data["GameMode"].Value + " mode, on map " + lobby.Data["Map"].Value);

        foreach (Player player in lobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
        }
    }

    private async void UpdateLobbyGameMode(string gameMode)
    {
        // Change Lobby GameMode
        try
        {
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode) }
            }
            });

            joinedLobby = hostLobby;
        }

        // Error when Changing Lobby GameMode
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void UpdatePlayerName(string newPlayerName)
    {
        // Update Player Name
        try
        {
            playerName = newPlayerName;

            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
            });
        }

        // Error when Updating Player Name
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            // Leave the currently joined Lobby
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                // Nullify Joined Lobby
                joinedLobby = null;
            }

            // Error
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }

            NetworkManager.Singleton.Shutdown();
        }
    }

    private async void KickPlayer()
    {
        // Kick the Selected Player
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[1].Id);
        }

        // Error
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void ChangeLobbyHost()
    {
        // Change Lobby Host
        try
        {
            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = joinedLobby.Players[1].Id // WIP CODE
            });

            joinedLobby = hostLobby;
        }

        // Error when Changing Lobby Host (somehow)
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void DeleteLobby()
    {
        // Delete the Current Lobby
        try
        {
            // Delete Lobby
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);

            // Nullify Joined Lobby
            joinedLobby = null;

            NetworkManager.Singleton.Shutdown();
        }

        // Error
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}
