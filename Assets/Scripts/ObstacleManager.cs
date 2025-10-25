using UnityEngine;
using AK.Wwise; // Make sure this is here for your Wwise Events

public class ObstacleManager : MonoBehaviour
{
    [Header("Game Objects")]

    public GameObject[] obstacleSets;

    [Header("Material Manager")]

    public ObstacleMaterialManager materialManager;



    public AK.Wwise.Event[] activationSounds;

    // --- We no longer need sizePrefabs or currentSizeIndex ---

    void Start()
    {
        // When the game starts, set the "no obstacles" state.
        // This will also correctly tell the MaterialManager that no set is active.
        SelectLayout(-1, 0);
    }

    // --- The Update() function is no longer needed, as the UI handles all input ---
    // void Update() {... }


    /// <summary>
    /// Activates a layout, plays a sound, AND tells the MaterialManager
    /// what to look at.
    /// </summary>
    public void SelectLayout(int obstacleIndex, int soundIndex)
    {
        AkSoundEngine.StopAll(gameObject); 

        // First, play the sound. Check if the index is valid for the array.
        if (soundIndex >= 0 && soundIndex < activationSounds.Length)
        {
            // Use the AK.Wwise.Event type to post the event [5]
            activationSounds[soundIndex]?.Post(gameObject);
        }

        // Then, activate the correct obstacle set.
        ActivateObstacleSet(obstacleIndex);

        // --- THIS IS THE CRITICAL NEW LOGIC ---
        // After activating the set, tell the MaterialManager to find its reflectors.
        if (materialManager != null)
        {
            GameObject newlyActiveSet = null;

            // Check if the index is valid for our obstacleSets array
            if (obstacleIndex >= 0 && obstacleIndex < obstacleSets.Length)
            {
                newlyActiveSet = obstacleSets[obstacleIndex];
            }

            // Pass the active set (or null if "None") to the material manager
            materialManager.FindReflectorsInSet(newlyActiveSet);
        }
    }

    /// <summary>
    /// This function is now just for activating/deactivating the GameObjects.
    /// </summary>
    public void ActivateObstacleSet(int indexToActivate)
    {
        for (int i = 0; i < obstacleSets.Length; i++)
        {
            bool shouldBeActive = (i == indexToActivate);
            if (obstacleSets[i] != null)
            {
                obstacleSets[i].SetActive(shouldBeActive);
            }
        }
    }

    // --- We no longer need the 'CycleObstacleSize()' function ---


    // --- THESE FUNCTIONS ARE NOW DECOUPLED ---
    // They ONLY change the material on the *currently active* set.

    public void SelectCarpetLayout()
    {
        // This function NO LONGER calls SelectLayout.
        if (materialManager != null)
        {
            materialManager.SetAllMaterialsToCarpet();
        }
    }

    public void SelectConcreteLayout()
    {
        // This function NO LONGER calls SelectLayout.
        if (materialManager != null)
        {
            materialManager.SetAllMaterialsToConcrete();
        }
    }
}