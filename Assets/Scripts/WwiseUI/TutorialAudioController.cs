/*
 * TutorialAudioController.cs
 * 
 * This script manages the global pause/resume state for all tutorial-related audio
 * by listening for the Numpad 0 key. It functions as a persistent singleton
 * to ensure it is always available.
 * 
 * REPORT DATE: Monday, October 27, 2025
 * LOCATION: Patchway, England
 *
 * REQUIRES:
 * 1. This script must be placed on a GameObject that persists between scenes
 *    (see Awake() method).
 * 2. The GameObject MUST also have an 'AkGameObj' component attached.
 * 3. Two Wwise Events named "Tutorial_Pause" and "Tutorial_Resume" must exist.
 * 4. Those Events must be included in a loaded SoundBank (e.g., Init.bnk).
 * 5. The 'Pause' and 'Resume' actions within those Events must have their
 *    'Scope' property set to 'Global' in Wwise.
 */

using UnityEngine;

// We do not need to import the AK.Wwise namespace if only
// using AkSoundEngine static methods with string names.
public class TutorialAudioController : MonoBehaviour
{
    // --- Singleton Pattern ---

    // Public static reference to this instance.
    public static TutorialAudioController Instance { get; private set; }

    // --- State Management ---

    // This boolean tracks the current state of the tutorial audio system.
    private bool isTutorialPaused = false;

    // --- Wwise Event Definitions ---

    // Using const strings for Event names is safer than typing string literals.
    // These names MUST exactly match the names of the Events in the Wwise project.
    private const string PAUSE_EVENT = "{Pause_Tutorial}";
    private const string RESUME_EVENT = "Resume_Tutorial";

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Used here to implement the singleton pattern.
    /// </summary>
    void Awake()
    {
        // Implement the singleton pattern
        if (Instance == null)
        {
            // This is the first and only instance.
            Instance = this;

            // Make this GameObject persistent across all scene loads.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // A duplicate instance was created (e.g., reloading the init scene).
            // Destroy the duplicate to enforce the singleton rule.
            Destroy(gameObject);
            return;
        }

        // We assume the AkGameObj component is on this same GameObject.
        // Wwise will automatically register it.
    }

    /// <summary>
    /// Update is called once per frame.
    /// Used here to listen for user input.
    /// </summary>
    void Update()
    {
        // Listen for the Numpad 0 key.
        // Input.GetKeyDown() fires only on the single frame the key is pressed down,
        // which is correct for a toggle.
        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            // Call our toggle logic method.
            ToggleTutorialPause();
        }
    }

    /// <summary>
    /// Toggles the tutorial audio pause state and posts the relevant Wwise Event.
    /// This can be called from Update() or from any other system (e.g., a UI button).
    /// </summary>
    public void ToggleTutorialPause()
    {
        // 1. Invert the state boolean
        isTutorialPaused = !isTutorialPaused;

        // 2. Post the corresponding Wwise Event based on the new state
        if (isTutorialPaused)
        {
            // --- PAUSE THE AUDIO ---

            // Post the "Tutorial_Pause" Event to the Wwise sound engine.
            // We post it on 'this.gameObject', which is our singleton controller.
            // Because the Event's Action Scope was set to 'Global' in Wwise,
            // it will correctly pause all playing instances of the
            // 'Tutorial_Audio' mixer, regardless of which GameObject
            // is posting this Event or which GameObject is playing the sound.
            AkSoundEngine.PostEvent(PAUSE_EVENT, this.gameObject); // 

            // Log to the console for debugging confirmation.
            Debug.Log(" TUTORIAL AUDIO PAUSED");
        }
        else
        {
            // --- RESUME THE AUDIO ---

            // Post the "Tutorial_Resume" Event.
            AkSoundEngine.PostEvent(RESUME_EVENT, this.gameObject); // 

            // Log to the console for debugging confirmation.
            Debug.Log(" TUTORIAL AUDIO RESUMED");
        }
    }

    // --- Public Utility Methods ---

    /// <summary>
    /// Forcibly pauses tutorial audio.
    /// Useful for other game systems (e.g., opening the main pause menu).
    /// </summary>
    public void ForcePause()
    {
        if (isTutorialPaused) return; // Already paused, do nothing.

        isTutorialPaused = true;
        AkSoundEngine.PostEvent(PAUSE_EVENT, this.gameObject);
        Debug.Log(" TUTORIAL AUDIO FORCED PAUSE");
    }

    /// <summary>
    /// Forcibly resumes tutorial audio.
    /// </summary>
    public void ForceResume()
    {
        if (!isTutorialPaused) return; // Already playing, do nothing.

        isTutorialPaused = false;
        AkSoundEngine.PostEvent(RESUME_EVENT, this.gameObject);
        Debug.Log(" TUTORIAL AUDIO FORCED RESUME");
    }
}