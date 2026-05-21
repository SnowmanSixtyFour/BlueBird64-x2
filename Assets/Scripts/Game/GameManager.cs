using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameObject birdPrefab;

    // Track clients that already have birds
    private HashSet<ulong> spawnedClients = new HashSet<ulong>();

    private void Start()
    {
        if (!IsServer) return;

        // Spawn birds for all currently connected clients
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnBirdForClient(clientId);
        }

        // Spawn birds for clients that join later
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        SpawnBirdForClient(clientId);
    }

    private void SpawnBirdForClient(ulong clientId)
    {
        // Prevent spawning multiple birds for the same client
        if (spawnedClients.Contains(clientId)) return;
        spawnedClients.Add(clientId);

        // FIX: Spawn Y above the floor
        Vector3 spawnPos = new Vector3(Random.Range(-2f, 2f), 5f, Random.Range(-2f, 2f));
        GameObject bird = Instantiate(birdPrefab, spawnPos, Quaternion.identity);

        // FIX: Reset Rigidbody velocity
        Rigidbody rb = bird.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        NetworkObject netObj = bird.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            // Spawn as player object and assign ownership
            netObj.SpawnAsPlayerObject(clientId, true);
        }
    }

    private void OnDestroy()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}