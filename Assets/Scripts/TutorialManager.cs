using UnityEngine;
using StarterAssets;

public class TutorialManager : MonoBehaviour
{
    // --- MODIFIED: Expanded for 20+ stages. Add as many as you need! ---
    public enum TutorialState
    {
        Intro, // State 0
        TheEcholocationClick, // State 1
        ForwardMovement, // State 2
        Collision, // State 3
        ForwardMovementAndCollision, // State 4
        ContinuedCollisions, // State 5
        BackwardMovement,
        SidewaysMovement,
        MovementPractice,
        RotationIntro,
        SonicCompass,
        RotationAndMovementPractice,
        SpawnAtStart,
        MainChamberSpawn,
        SpawnPointsAndMovement,
        Section01Review,
        ActivatingObstacles,
        NavigatingObstacles,
        ObstacleFeedback,
        ObstacleReview,
        ClickPitch,
        Outro,
        Complete
        // Add your new tutorial parts here, for example:
        // Part6_NewConcept,
        // Part7_AnotherOne,
        // ... up to Part 20
        // This should be the last instruction before 'Complete'
        // The final state
    }

    [Header("Core References")]
    public FirstPersonController playerController;
    public ObstacleManager obstacleManager;
    public PlayerAudio playerAudio;

    [Header("Audio Events")]
    [Tooltip("Plays when the game opens (e.g., Play_01_Intro)")]
    public AK.Wwise.Event introductoryAudio;
    [Tooltip("Assign all instructional sounds in order, matching the TutorialState enum order.")]
    public AK.Wwise.Event[] instructionSounds;

    [Header("Pause/Resume Events")]
    public AK.Wwise.Event pauseEvent;
    public AK.Wwise.Event resumeEvent;

    [Header("Tutorial Settings")]
    [Tooltip("Assign all spawn points in order, matching the TutorialState enum order.")]
    public Transform[] spawnPoints;

    private TutorialState currentState;
    private bool isAudioPaused = false;

    // --- REFACTORED: Replaced ReplayState enum with a simple index ---
    private int replayIndex = -1; // -1 means replay is off

    void Start()
    {
        // --- VALIDATION: Added checks for array lengths ---
        if (playerAudio == null || obstacleManager == null || playerController == null)
        {
            Debug.LogError("TUTORIAL MANAGER ERROR: Core references are not assigned!", this.gameObject);
            this.enabled = false;
            return;
        }

        // The number of states (minus Intro and Complete) must match the number of sounds/spawns
        int instructionalStateCount = System.Enum.GetNames(typeof(TutorialState)).Length - 2;
        if (instructionSounds.Length != instructionalStateCount || spawnPoints.Length != instructionalStateCount)
        {
            Debug.LogError($"TUTORIAL MANAGER ERROR: Mismatch between array sizes and TutorialState enum count. Expecting {instructionalStateCount} items in each array.", this.gameObject);
            this.enabled = false;
            return;
        }

        currentState = TutorialState.Intro;
        introductoryAudio?.Post(gameObject);
        Debug.Log("Game started. Playing intro audio. Waiting for key press to begin tutorial...");
    }

    void Update()
    {
        // Progresses the tutorial with BackQuote key
        if (currentState != TutorialState.Complete && Input.GetKeyDown(KeyCode.BackQuote))
        {
            GoToNextState();
        }

        // Handles replaying instructions with Tab key
        if (currentState == TutorialState.Complete && Input.GetKeyDown(KeyCode.Tab))
        {
            CycleInstructionReplay();
        }
    }

    // --- REFACTORED: Simplified replay logic using an index ---
    void CycleInstructionReplay()
    {
        StopAndResumeAudio();

        replayIndex++; // Move to the next instruction

        // If index goes past the last sound, turn replay off
        if (replayIndex >= instructionSounds.Length)
        {
            replayIndex = -1;
            Debug.Log("Instruction Replay Stopped.");
            return;
        }

        // Play the sound at the current index
        instructionSounds[replayIndex]?.Post(gameObject);
        // Get the name of the state from the enum value for a clear debug message
        string stateName = ((TutorialState)(replayIndex + 1)).ToString();
        Debug.Log($"Replaying: {stateName}");
    }

    // --- REFACTORED: Simplified state progression ---
    void GoToNextState()
    {
        StopAndResumeAudio();
        replayIndex = -1; // Reset replay when progressing

        // Increment the current state to the next one in the enum
        currentState++;

        Debug.Log($"Proceeding to: {currentState}");

        // Check if the tutorial is now complete
        if (currentState == TutorialState.Complete)
        {
            // --- FINAL STAGE LOGIC ---
            // This logic now only runs once when entering the 'Complete' state.
            obstacleManager.enabled = true;
            playerAudio.ResetPitchRTPC();
            Debug.Log("Tutorial Complete! Starting Main Game.");
            // Note: The final instruction and teleport are handled below, just like any other stage.
        }

        // Use the enum's integer value to get the correct array index
        // We subtract 1 because 'Intro' is state 0, but the arrays are for states 1 onwards.
        int currentIndex = (int)currentState - 1;

        if (currentIndex < 0 || currentIndex >= instructionSounds.Length)
        {
            // This case handles the transition from Intro to the first state, or if something goes wrong.
            // The first state (e.g., BasicMechanics) is handled correctly as (1-1) = index 0.
            if (currentState == TutorialState.Intro) return; // Should not happen with current logic
            Debug.LogWarning($"Tutorial Manager: No instruction/spawn for state {currentState}.");
            return;
        }

        // Teleport player and play the corresponding instruction sound
        playerController.Teleport(spawnPoints[currentIndex].position, spawnPoints[currentIndex].rotation);
        instructionSounds[currentIndex]?.Post(gameObject);
    }

    public void ToggleAudioPause()
    {
        isAudioPaused = !isAudioPaused;
        if (isAudioPaused)
        {
            pauseEvent?.Post(gameObject);
            Debug.Log("Tutorial audio PAUSED.");
        }
        else
        {
            resumeEvent?.Post(gameObject);
            Debug.Log("Tutorial audio RESUMED.");
        }
    }

    // --- NEW: Helper function to reduce code duplication ---
    private void StopAndResumeAudio()
    {
        AkSoundEngine.StopAll(gameObject);
        if (isAudioPaused)
        {
            resumeEvent?.Post(gameObject);
            isAudioPaused = false;
        }
    }
}