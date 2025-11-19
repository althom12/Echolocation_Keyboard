using UnityEngine;
using StarterAssets;

/// <summary>
/// Tutorial Manager - Refactored for Logic/Audio Separation
/// 
/// RESPONSIBILITIES (Logic Only):
/// - Tutorial state machine progression
/// - Player spawning and teleportation
/// - Input handling (backtick to advance, Numpad 0 to pause)
/// - Tutorial activation/deactivation
/// - Component enabling/disabling (ObstacleManager, PlayerAudio)
/// 
/// AUDIO DELEGATION:
/// - WwiseSequentialEventPlayer: Handles instruction sound playback
/// - WwiseStateManager: Handles pause/resume states
/// 
/// This script no longer contains ANY Wwise event posting.
/// All audio is delegated to specialized audio components.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // ???????????????????????????????????????????????????????????
    // SINGLETON (SCENE-BASED - NO DontDestroyOnLoad)
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Scene-based singleton instance. Does NOT persist across scene loads.
    /// This manager holds references to scene objects (player, spawnPoints, obstacleManager)
    /// that would be lost if the manager persisted.
    /// </summary>
    public static TutorialManager Instance { get; private set; }

    // ???????????????????????????????????????????????????????????
    // TUTORIAL STATE ENUM
    // ???????????????????????????????????????????????????????????

    public enum TutorialState
    {
        Chapter01_Intro,
        Chapter02_EcholocationClick,
        Chapter03_ForwardMovement,
        Chapter04_ForwardMovementAndCollision,
        Chapter05_ContinuedCollisions,
        Chapter06_BackwardMovement,
        Chapter07_SidewaysMovement,
        Chapter08_MovementPractice,
        Chapter09_RotationIntro,
        Chapter10_SonicCompass,
        Chapter11_RotationAndMovementPractice,
        Chapter12_SpawnAtStart,
        Chapter13_MainChamberSpawn,
        Chapter14_SpawnPointsAndMovement,
        Chapter15_Section01Review,
        Chapter16_UIIntro,
        Chapter17_UIMainMenuNav,
        Chapter18_EnteringACategory,
        Chapter19_NavigatingSubmenus,
        Chapter20_AdjustingSliders,
        Chapter21_UIReview,
        Chapter22_ObsPresets,
        Chapter23_ObsCustom,
        Chapter24_ObsNavigation,
        Chapter25_HearObs,
        Chapter26_ObsPresetDesc,
        Chapter27_ObsReview,
        Chapter28_AudioLandmarks,
        Chapter29_AudioLandmarksSliders,
        Chapter30_Outro,
        Chapter31_Complete
    }

    // ???????????????????????????????????????????????????????????
    // INSPECTOR FIELDS - CORE REFERENCES
    // ???????????????????????????????????????????????????????????

    [Header("Core References")]
    [Tooltip("Reference to the player controller")]
    public FirstPersonController playerController;

    [Tooltip("Reference to the obstacle manager")]
    public ObstacleManager obstacleManager;

    [Tooltip("Reference to the player audio script")]
    public PlayerAudio playerAudio;

    [Header("Audio Components")]
    [Tooltip("Handles sequential instruction playback")]
    public WwiseSequentialEventPlayer instructionAudioPlayer;

    [Tooltip("Plays the pause event")]
    public WwiseEventPlayer pauseEventPlayer;

    [Tooltip("Plays the resume event")]
    public WwiseEventPlayer resumeEventPlayer;

    [Tooltip("Manages pause/resume audio states")]
    public WwiseStateManager pauseStateManager;

    [Tooltip("Plays the introductory audio")]
    public WwiseEventPlayer introAudioPlayer;

    [Tooltip("Plays the tutorial complete sound")]
    public WwiseEventPlayer completeAudioPlayer;

    [Header("Tutorial Settings")]
    [Tooltip("Spawn points for each tutorial chapter")]
    public Transform[] spawnPoints;

    [Tooltip("Where to teleport player when ending tutorial")]
    public Transform startPoint;

    [Header("Auto-Start Settings")]
    [Tooltip("Should the tutorial automatically start when the scene loads?")]
    public bool autoStartTutorial = true;

    // ???????????????????????????????????????????????????????????
    // PRIVATE FIELDS
    // ???????????????????????????????????????????????????????????

    private TutorialState currentState;
    private int replayIndex = -1;
    private bool isTutorialActive = false;

    // ???????????????????????????????????????????????????????????
    // UNITY LIFECYCLE
    // ???????????????????????????????????????????????????????????

    private void Awake()
    {
        // Singleton setup - Scene-based (no DontDestroyOnLoad)
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[TutorialManager] Duplicate instance found on '{gameObject.name}'. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Validate core references
        if (playerAudio == null || obstacleManager == null || playerController == null)
        {
            Debug.LogError("TUTORIAL MANAGER ERROR: Core references are not assigned!", this.gameObject);
            this.enabled = false;
            return;
        }

        // Validate audio components
        if (instructionAudioPlayer == null || pauseStateManager == null ||
            introAudioPlayer == null || completeAudioPlayer == null)
        {
            Debug.LogError("TUTORIAL MANAGER ERROR: Audio component references are not assigned!", this.gameObject);
            this.enabled = false;
            return;
        }

        // Validate array sizes
        int instructionalStateCount = System.Enum.GetNames(typeof(TutorialState)).Length - 2;
        if (spawnPoints.Length != instructionalStateCount)
        {
            Debug.LogError($"TUTORIAL MANAGER ERROR: spawnPoints array size mismatch. Expected {instructionalStateCount}, got {spawnPoints.Length}", this.gameObject);
            this.enabled = false;
            return;
        }

        // Auto-start tutorial if configured
        if (autoStartTutorial)
        {
            isTutorialActive = true;
            currentState = TutorialState.Chapter01_Intro;

            if (obstacleManager != null)
            {
                obstacleManager.enabled = false;
            }

            // Play intro audio through the audio component
            introAudioPlayer.PlayEvent();

            Debug.Log("Tutorial auto-started. Press BackQuote (`) to progress.");
        }
    }

    void Update()
    {
        // Progression input (backtick key)
        if (isTutorialActive && currentState != TutorialState.Chapter31_Complete && Input.GetKeyDown(KeyCode.BackQuote))
        {
            GoToNextState();
        }

        // Replay input (Tab key) - only in complete state
        if (currentState == TutorialState.Chapter31_Complete && Input.GetKeyDown(KeyCode.Tab))
        {
            CycleInstructionReplay();
        }

        // Pause/Resume input (Numpad 0)
        if (isTutorialActive && Input.GetKeyDown(KeyCode.Keypad0))
        {
            ToggleAudioPause();
        }
    }

    // ???????????????????????????????????????????????????????????
    // PUBLIC API - Called by UI Buttons
    // ???????????????????????????????????????????????????????????

    public void StartTutorial()
    {
        Debug.Log("=== STARTING TUTORIAL ===");
        Debug.Log($"Tutorial was active: {isTutorialActive}");

        // Stop all audio
        AkSoundEngine.StopAll(gameObject);

        // Reset pause state through the state manager
        pauseStateManager.SetToPrimaryState(); // Sets to "Playing"

        // Reset tutorial state
        isTutorialActive = true;
        currentState = TutorialState.Chapter01_Intro;
        replayIndex = -1;

        // Disable obstacle manager during tutorial
        if (obstacleManager != null)
        {
            obstacleManager.enabled = false;
        }

        // Play intro audio
        introAudioPlayer.PlayEvent();

        Debug.Log("=== TUTORIAL START COMPLETE ===");
    }

    public void EndTutorial()
    {
        if (!isTutorialActive)
        {
            Debug.Log("Tutorial is not active.");
            return;
        }

        Debug.Log("Ending Tutorial...");

        isTutorialActive = false;
        AkSoundEngine.StopAll(gameObject);

        // Re-enable obstacle manager
        if (obstacleManager != null)
        {
            obstacleManager.enabled = true;
        }

        // Reset player pitch
        if (playerAudio != null)
        {
            playerAudio.ResetPitchRTPC();
        }

        // Teleport to start point
        if (startPoint != null)
        {
            playerController.Teleport(startPoint.position, startPoint.rotation);
        }

        Debug.Log("Tutorial ended. Returned to start point.");
    }

    public void ToggleAudioPause()
    {
        Debug.Log("ToggleAudioPause called - delegating to WwiseStateManager");

        // Toggle the state manager (for tracking)
        pauseStateManager.ToggleState();

        // CRITICAL: Also post the pause/resume events (for actual Wwise behavior)
        if (pauseStateManager.IsInPrimaryState())
        {
            // We're now in Playing state
            resumeEventPlayer?.PlayEvent();
            Debug.Log("Tutorial audio RESUMED (Playing state)");
        }
        else
        {
            // We're now in Paused state
            pauseEventPlayer?.PlayEvent();
            Debug.Log("Tutorial audio PAUSED (Paused state)");
        }
    }

    public bool IsTutorialActive()
    {
        return isTutorialActive;
    }

    // ???????????????????????????????????????????????????????????
    // PRIVATE METHODS - Tutorial Flow
    // ???????????????????????????????????????????????????????????

    private void GoToNextState()
    {
        // Stop and resume audio (clears any paused state)
        StopAndResumeAudio();
        replayIndex = -1;

        // Advance state
        currentState++;

        Debug.Log($"Proceeding to: {currentState}");

        // Check if tutorial is complete
        if (currentState == TutorialState.Chapter31_Complete)
        {
            isTutorialActive = false;
            obstacleManager.enabled = true;
            playerAudio.ResetPitchRTPC();

            Debug.Log("Tutorial Complete! Starting Main Game.");

            // Play completion sound
            completeAudioPlayer.PlayEvent();

            return;
        }

        // Get the index for arrays (state enum starts at 0 = Intro, but arrays start at Chapter02)
        int currentIndex = (int)currentState - 1;

        // Validate index
        if (currentIndex < 0 || currentIndex >= spawnPoints.Length)
        {
            if (currentState == TutorialState.Chapter01_Intro) return;
            Debug.LogWarning($"Tutorial Manager: No spawn point for state {currentState}.");
            return;
        }

        // Teleport player
        playerController.Teleport(spawnPoints[currentIndex].position, spawnPoints[currentIndex].rotation);

        // Play instruction audio through the sequential player
        instructionAudioPlayer.PlayAtIndex(currentIndex);
    }

    private void CycleInstructionReplay()
    {
        StopAndResumeAudio();

        replayIndex++;

        // Check if we've reached the end of the replay sequence
        if (replayIndex >= instructionAudioPlayer.GetEventCount())
        {
            replayIndex = -1;
            Debug.Log("Instruction Replay Stopped.");
            return;
        }

        // Play the instruction at the current replay index
        instructionAudioPlayer.PlayAtIndex(replayIndex);

        string stateName = ((TutorialState)(replayIndex + 1)).ToString();
        Debug.Log($"Replaying: {stateName}");
    }

    private void StopAndResumeAudio()
    {
        // Stop all audio on this GameObject
        AkSoundEngine.StopAll(gameObject);

        // Ensure we're in the "Playing" state (not paused)
        if (pauseStateManager.IsInSecondaryState()) // If paused
        {
            pauseStateManager.SetToPrimaryState(); // Resume to Playing
        }
    }

    void OnGUI()
    {
        if (isTutorialActive)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;

            string stateText = pauseStateManager.IsInPrimaryState() ? "PLAYING" : "PAUSED";
            GUI.Label(new Rect(10, 10, 400, 30), $"Tutorial State: {stateText}", style);

            // Test button
            if (GUI.Button(new Rect(10, 50, 200, 40), "Toggle Pause (Test)"))
            {
                ToggleAudioPause();
            }
        }
    }
}