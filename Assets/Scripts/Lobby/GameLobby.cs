using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameLobby : MonoBehaviour
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
    [SerializeField] private Button backButton;

    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button refreshButton;

    [SerializeField] private Button randomLobbyButton;

    private async void Start()
    {
        // Set Variables

        backButton.onClick.AddListener(BackButtonClicked);

        createLobbyButton.onClick.AddListener(CreateLobbyClicked);
        refreshButton.onClick.AddListener(RefreshButtonClicked);

        randomLobbyButton.onClick.AddListener(RandomLobbyButtonClicked);

        // Login to Multiplayer

        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // Set Player Name
        playerName = "Player " + UnityEngine.Random.Range(100, 999);
        Debug.Log("Your username is " + playerName);
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

    private void BackButtonClicked()
    {
        LeaveLobby();

        SceneManager.LoadScene("Title");
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
        }

        // Error when Creating Lobby
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void Update()
    {
        // Update Lobby
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
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
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);

            joinedLobby = lobby;

            Debug.Log("Joined Lobby with code " + lobbyCode);
            PrintPlayers(lobby);
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
        await LobbyService.Instance.QuickJoinLobbyAsync();
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
        // Leave the currently joined Lobby
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }

        // Error
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
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
                HostId = joinedLobby.Players[1].Id
            });

            joinedLobby = hostLobby;
        }

        // Error when Changing Lobby Host (somehow)
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void DeleteLobby()
    {
        // Delete the Current Lobby
        try
        {
            LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
        }

        // Error
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}
