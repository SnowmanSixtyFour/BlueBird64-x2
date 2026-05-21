using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameObject birdPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayer(clientId);
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        Vector3 spawnPos = new Vector3(
            Random.Range(-2f, 2f),
            1f,
            Random.Range(-2f, 2f)
        );

        GameObject player = Instantiate(birdPrefab, spawnPos, Quaternion.identity);

        player.GetComponent<NetworkObject>()
            .SpawnAsPlayerObject(clientId, true);
    }
}