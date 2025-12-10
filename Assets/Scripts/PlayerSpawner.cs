using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Assignments")]
    public GameObject playerPrefab; // The player object you want to spawn
    public Transform spawnPoint;    // The empty GameObject where the player will appear

    void Start()
    {
        // Checks if you assigned the variables to prevent errors
        if (playerPrefab != null && spawnPoint != null)
        {
            // Spawns the player at the spawnPoint's position and rotation
            Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError("PlayerSpawner: Please assign the Player Prefab and Spawn Point in the Inspector.");
        }
    }
}