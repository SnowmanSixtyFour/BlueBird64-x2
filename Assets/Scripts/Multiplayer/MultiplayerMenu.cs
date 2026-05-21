using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MultiplayerMenu : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    private Button backButton, exitButton, createLobbyButton, joinLobbyButton,
        joinViaCodeButton, codeBackButton, refreshButton, randomLobbyButton, deleteLobbyButton, startGameButton;

    [SerializeField] private TMP_InputField codeInput, playerNameInput;
    [SerializeField] private TMP_Text playerCount, lobbyCode;
    [SerializeField] private GameObject searchUI, joiningLobbyUI, joinViaCodeUI, hostLobbyUI;

    private Lobby hostLobby;
    private Lobby joinedLobby;
    private string playerName;
    private float heartbeatTimer;
    private float lobbyUpdateTimer;

    private async void Awake()
    {
        backButton.onClick.AddListener(() => SceneManager.LoadScene("Title"));
        exitButton.onClick.AddListener(LeaveLobby);
        createLobbyButton.onClick.AddListener(CreateLobbyClicked);
        joinLobbyButton.onClick.AddListener(() => { searchUI.SetActive(false); joinViaCodeUI.SetActive(true); });
        joinViaCodeButton.onClick.AddListener(JoinViaCodeClicked);
        codeBackButton.onClick.AddListener(() => { joinViaCodeUI.SetActive(false); searchUI.SetActive(true); });
        refreshButton.onClick.AddListener(ListLobbies);
        randomLobbyButton.onClick.AddListener(JoinRandomLobbyClicked);
        deleteLobbyButton.onClick.AddListener(DeleteLobby);
        startGameButton.onClick.AddListener(StartGame);

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        playerName = GenerateRandomName();
    }

    private string GenerateRandomName() => "Player " + UnityEngine.Random.Range(100, 999);

    private void Update()
    {
        UpdateLobbyUI();
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    #region Lobby Management

    private async void CreateLobbyClicked()
    {
        ValidatePlayerName();

        try
        {
            // Allocate Relay server for host
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(8);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Store Relay code in lobby data
            var createOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayerData(),
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Competition") },
                    { "Map", new DataObject(DataObject.VisibilityOptions.Public, "Forest") },
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            };

            hostLobby = await LobbyService.Instance.CreateLobbyAsync("Lobby", 8, createOptions);
            joinedLobby = hostLobby;
            lobbyCode.text = hostLobby.LobbyCode;

            // Configure Unity Transport for host
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            Debug.Log($"Created lobby {hostLobby.Name} with Relay join code {joinCode}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create lobby: {e}");
        }
    }

    private async void JoinViaCodeClicked()
    {
        ValidatePlayerName();
        string code = codeInput.text.Trim().ToUpper();
        await JoinLobbyByCode(code);
    }

    private async void JoinRandomLobbyClicked()
    {
        ValidatePlayerName();
        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            string relayCode = joinedLobby.Data["RelayCode"].Value;
            await JoinRelayAsClient(relayCode);
            Debug.Log($"Joined random lobby {joinedLobby.Name}");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private async Task JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
            {
                Player = GetPlayerData()
            });

            string relayCode = joinedLobby.Data["RelayCode"].Value;
            await JoinRelayAsClient(relayCode);

            Debug.Log($"Joined Lobby {joinedLobby.Name}");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private async Task JoinRelayAsClient(string relayJoinCode)
    {
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetClientRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData
        );

        NetworkManager.Singleton.StartClient();
    }

    private Player GetPlayerData()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
            }
        };
    }

    private void ValidatePlayerName()
    {
        string input = playerNameInput.text.Trim();
        playerName = string.IsNullOrEmpty(input) ? GenerateRandomName() : (input.Length > 10 ? input.Substring(0, 10) : input);
    }

    private void UpdateLobbyUI()
    {
        if (joinedLobby != null)
        {
            searchUI.SetActive(false);
            joiningLobbyUI.SetActive(true);
            joinViaCodeUI.SetActive(false);
            hostLobbyUI.SetActive(hostLobby != null);

            playerCount.text = "";
            foreach (var p in joinedLobby.Players)
            {
                if (p.Data != null && p.Data.ContainsKey("PlayerName"))
                    playerCount.text += p.Data["PlayerName"].Value + "\n";
            }
        }
        else
        {
            searchUI.SetActive(!joinViaCodeUI.activeSelf);
            joiningLobbyUI.SetActive(false);
        }
    }

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby == null) return;
        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer > 0) return;

        heartbeatTimer = 15f;
        try { await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id); }
        catch { }
    }

    private async void HandleLobbyPolling()
    {
        if (joinedLobby == null) return;
        lobbyUpdateTimer -= Time.deltaTime;
        if (lobbyUpdateTimer > 0) return;

        lobbyUpdateTimer = 1.1f;
        try { joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id); }
        catch { }
    }

    private async void ListLobbies()
    {
        try
        {
            var response = await LobbyService.Instance.QueryLobbiesAsync();
            Debug.Log($"Found {response.Results.Count} lobbies");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private async void LeaveLobby()
    {
        if (joinedLobby == null) return;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            joinedLobby = null;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        NetworkManager.Singleton?.Shutdown();
    }

    private async void DeleteLobby()
    {
        if (joinedLobby == null) return;

        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
            joinedLobby = null;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        NetworkManager.Singleton?.Shutdown();
    }

    #endregion

    #region Game Start

    public void StartGame()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;
        if (!nm.IsHost) return;
        if (nm.SceneManager == null) return;

        Debug.Log("Host loading Game scene for all clients...");
        nm.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    #endregion
}