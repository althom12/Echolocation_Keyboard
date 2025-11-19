using UnityEngine;
using StarterAssets;

public class SpawnManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the player's main GameObject here.")]
    public FirstPersonController playerController;

    [Header("Spawn Points")]
    [Tooltip("The default spawn point for the start of the game.")]
    public Transform defaultSpawnPoint;

    [Tooltip("The second spawn location, triggered by the Left Alt key.")]
    public Transform alternateSpawnPoint;

    [Header("Wwise")]
    [Tooltip("The sound to play for the default respawn (R key).")]
    public AK.Wwise.Event RespawnSound;

    [Tooltip("The sound to play for the alternate respawn (Left Alt key).")]
    public AK.Wwise.Event AlternateRespawnSound; // NEW

    void Start()
    {
        // At the start, spawn the player at the default location.
        SpawnPlayer(defaultSpawnPoint);
    }

    public void SpawnPlayer(Transform targetSpawnPoint)
    {
        if (targetSpawnPoint == null)
        {
            Debug.LogError("Spawn point is not set!");
            return;
        }

        // Just call the new Teleport function on the player controller.
        playerController.Teleport(targetSpawnPoint.position, targetSpawnPoint.rotation);

        Debug.Log("Player has been spawned at: " + targetSpawnPoint.name);
    }

    void Update()
    {
        // Use a different key, like 'R', to return to the DEFAULT spawn.
        if (Input.GetKeyDown(KeyCode.RightAlt))
        {
            Debug.Log("'R' key pressed. Respawning player at default spawn...");
            RespawnSound?.Post(gameObject);
            SpawnPlayer(defaultSpawnPoint);
        }

        // Use the LEFT ALT key to go to the ALTERNATE spawn.
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            Debug.Log("Left Alt key pressed. Respawning player at alternate spawn...");
            AlternateRespawnSound?.Post(gameObject); // CHANGED
            SpawnPlayer(alternateSpawnPoint);
        }
    }
}