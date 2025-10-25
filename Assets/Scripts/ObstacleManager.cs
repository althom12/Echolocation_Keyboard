using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    [Header("Game Objects")]
    [Tooltip("Assign your 5 obstacle set parent objects here.")]
    public GameObject[] obstacleSets;

    [Header("Material Manager")]
    [Tooltip("Drag the GameObject with the ObstacleMaterialManager script here.")]
    public ObstacleMaterialManager materialManager;

    // --- ADD THIS ---
    [Header("Size Prefabs")]
    [Tooltip("Assign your size prefabs here (e.g., verysmall, small, medium, large)")]
    public GameObject[] sizePrefabs;

    [Header("Wwise Events")]
    [Tooltip("Assign 6 Wwise events. Index 0 is for 'no obstacles', Index 1 is for the first set, etc.")]
    public AK.Wwise.Event[] activationSounds;

    // --- ADD THIS ---
    // This will keep track of which size we are currently using
    private int currentSizeIndex = -1;

    void Start()
    {
        // When the game starts, set the "no obstacles" state without playing a sound.
        ActivateObstacleSet(-1);
    }

    void Update()
    {
        // Listen for number key presses and call our new controller function.
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectLayout(-1, 0); // Obstacle: None, Sound: Index 0
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectLayout(0, 1);  // Obstacle: Index 0, Sound: Index 1
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectLayout(1, 2);  // Obstacle: Index 1, Sound: Index 2
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectLayout(2, 3);  // Obstacle: Index 2, Sound: Index 3
        }
        // --- ADD THIS NEW 'ELSE IF' BLOCK ---
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            CycleObstacleSize();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            // Activate customizable set (e.g., index 3)
            SelectLayout(3, 4);

            // Tell the material manager to set carpet
            if (materialManager != null)
            {
                materialManager.SetAllMaterialsToCarpet();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            // Activate the *SAME* customizable set (index 3)
            SelectLayout(3, 5); // Note: We use 3 again, not 4!

            // Tell the material manager to set concrete
            if (materialManager != null)
            {
                materialManager.SetAllMaterialsToConcrete();
            }
        }
    }

    /// <summary>
    /// A new controller function to activate a layout AND play a sound.
    /// </summary>
    public void SelectLayout(int obstacleIndex, int soundIndex)
    {
        // Stop any sounds that were previously played by this object.
        AkSoundEngine.StopAll(gameObject);

        // First, play the sound. Check if the index is valid for the array.
        if (soundIndex >= 0 && soundIndex < activationSounds.Length)
        {
            activationSounds[soundIndex]?.Post(gameObject);
        }

        // Then, activate the correct obstacle set.
        ActivateObstacleSet(obstacleIndex);
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

    // --- ADD THIS ENTIRE NEW FUNCTION ---
    /// <summary>
    /// Cycles through the size prefabs and applies their scale to the children
    /// of the customizable obstacle set (obstacleSets[3]).
    /// </summary>
    public void CycleObstacleSize()
    {
        // 1. Make sure we have prefabs to use
        if (sizePrefabs.Length == 0)
        {
            Debug.LogWarning("No size prefabs assigned to ObstacleManager.");
            return;
        }

        // 2. Increment and wrap the index. 
        // Starts at -1, so first press becomes 0.
        currentSizeIndex++;
        if (currentSizeIndex >= sizePrefabs.Length)
        {
            currentSizeIndex = 0;
        }

        // 3. Get the source prefab and target obstacle set
        GameObject sourceSizePrefab = sizePrefabs[currentSizeIndex];
        GameObject targetObstacleSet = obstacleSets[3];

        if (sourceSizePrefab == null || targetObstacleSet == null)
        {
            Debug.LogError("A size prefab or obstacleSets[3] is not assigned.");
            return;
        }

        // 4. Activate the customizable set (index 3)
        // We'll use sound index 4 (from key '9') as the sound
        SelectLayout(3, 4);

        // 5. Get the parent transforms
        Transform sourceParent = sourceSizePrefab.transform;
        Transform targetParent = targetObstacleSet.transform;

        // 6. Safety Check: Ensure they have the same number of children
        if (sourceParent.childCount != targetParent.childCount)
        {
            Debug.LogError("Size prefab '" + sourceSizePrefab.name +
                           "' and 'obstacleSets[3]' have a different number of children. Cannot apply scale.");
            return;
        }

        // 7. Loop through and apply the scale from each source child to each target child
        for (int i = 0; i < targetParent.childCount; i++)
        {
            Transform targetChild = targetParent.GetChild(i);
            Transform sourceChild = sourceParent.GetChild(i);

            // This applies just the scale (dimensions)
            targetChild.localScale = sourceChild.localScale;
        }

        Debug.Log("Set obstacle size to: " + sourceSizePrefab.name);
    }

    public void SelectCarpetLayout()
    {
        // This is your logic from KeyCode.Alpha9
        SelectLayout(3, 4);
        if (materialManager != null)
        {
            materialManager.SetAllMaterialsToCarpet();
        }
    }

    public void SelectConcreteLayout()
    {
        // This is your logic from KeyCode.Alpha0
        SelectLayout(3, 5);
        if (materialManager != null)
        {
            materialManager.SetAllMaterialsToConcrete();
        }
    }
}