using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    [Header("Game Objects")]
    [Tooltip("Assign your 5 obstacle set parent objects here.")]
    public GameObject[] obstacleSets;

    [Header("Wwise Events")]
    [Tooltip("Assign 6 Wwise events. Index 0 is for 'no obstacles', Index 1 is for the first set, etc.")]
    public AK.Wwise.Event[] activationSounds;

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
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SelectLayout(3, 4);  // Obstacle: Index 3, Sound: Index 4
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SelectLayout(4, 5);  // Obstacle: Index 4, Sound: Index 5
        }
    }

    /// <summary>
    /// A new controller function to activate a layout AND play a sound.
    /// </summary>
    void SelectLayout(int obstacleIndex, int soundIndex)
    {
        // --- THIS IS THE NEW LINE ---
        // Stop any sounds that were previously played by this object.
        AkSoundEngine.StopAll(gameObject);

        // First, play the sound. Check if the index is valid for the array.
        if (soundIndex >= 0 && soundIndex < activationSounds.Length)
        {
            // The '?' prevents errors if a slot is empty.
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
}