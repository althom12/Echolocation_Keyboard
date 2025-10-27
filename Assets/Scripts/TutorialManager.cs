using UnityEngine;
using StarterAssets;

public class TutorialManager : MonoBehaviour
{
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
        Chapter29_Outro,
        Chapter30_Complete
    }

    [Header("Core References")]
    public FirstPersonController playerController;
    public ObstacleManager obstacleManager;
    public PlayerAudio playerAudio;

    [Header("Audio Events")]
    public AK.Wwise.Event introductoryAudio;
    public AK.Wwise.Event[] instructionSounds;

    [Header("Pause/Resume Events")]
    public AK.Wwise.Event pauseEvent;
    public AK.Wwise.Event resumeEvent;

    [Header("Tutorial Settings")]
    public Transform[] spawnPoints;
    public AK.Wwise.Event tutorialCompleteSound;

    [Header("Start Point")]
    [Tooltip("Where to teleport player when ending tutorial")]
    public Transform startPoint;

    private TutorialState currentState;
    private bool isAudioPaused = false;
    private int replayIndex = -1;
    private bool isTutorialActive = false;

    public static TutorialManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (playerAudio == null || obstacleManager == null || playerController == null)
        {
            Debug.LogError("TUTORIAL MANAGER ERROR: Core references are not assigned!", this.gameObject);
            this.enabled = false;
            return;
        }

        int instructionalStateCount = System.Enum.GetNames(typeof(TutorialState)).Length - 2;
        if (instructionSounds.Length != instructionalStateCount || spawnPoints.Length != instructionalStateCount)
        {
            Debug.LogError($"TUTORIAL MANAGER ERROR: Mismatch between array sizes and TutorialState enum count. Expecting {instructionalStateCount} items in each array. Got {instructionSounds.Length} sounds and {spawnPoints.Length} spawns.", this.gameObject);
            this.enabled = false;
            return;
        }

        // AUTO-START TUTORIAL - Add these lines:
        isTutorialActive = true;
        currentState = TutorialState.Chapter01_Intro;

        if (obstacleManager != null)
        {
            obstacleManager.enabled = false;
        }

        introductoryAudio?.Post(gameObject);
        Debug.Log("Game started. Tutorial auto-playing intro audio. Press BackQuote (`) to progress.");

       
    }

    void Update()
    {
        if (isTutorialActive && currentState != TutorialState.Chapter30_Complete && Input.GetKeyDown(KeyCode.BackQuote))
        {
            GoToNextState();
        }

        if (currentState == TutorialState.Chapter30_Complete && Input.GetKeyDown(KeyCode.Tab))
        {
            CycleInstructionReplay();
        }

        // ADD THIS: Numpad 0 for pause/resume
        if (isTutorialActive && Input.GetKeyDown(KeyCode.Keypad0))
        {
            ToggleAudioPause();
        }
    }

    public void StartTutorial()
    {
        Debug.Log("Starting Tutorial from beginning...");

        isTutorialActive = true;
        currentState = TutorialState.Chapter01_Intro;
        replayIndex = -1;

        if (obstacleManager != null)
        {
            obstacleManager.enabled = false;
        }

        introductoryAudio?.Post(gameObject);

        Debug.Log("Tutorial started. Press BackQuote (`) to progress.");
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

        if (obstacleManager != null)
        {
            obstacleManager.enabled = true;
        }

        if (playerAudio != null)
        {
            playerAudio.ResetPitchRTPC();
        }

        if (startPoint != null)
        {
            playerController.Teleport(startPoint.position, startPoint.rotation);
        }

        Debug.Log("Tutorial ended. Returned to start point.");
    }

    void CycleInstructionReplay()
    {
        StopAndResumeAudio();

        replayIndex++;

        if (replayIndex >= instructionSounds.Length)
        {
            replayIndex = -1;
            Debug.Log("Instruction Replay Stopped.");
            return;
        }

        instructionSounds[replayIndex]?.Post(gameObject);
        string stateName = ((TutorialState)(replayIndex + 1)).ToString();
        Debug.Log($"Replaying: {stateName}");
    }

    void GoToNextState()
    {
        StopAndResumeAudio();
        replayIndex = -1;

        currentState++;

        Debug.Log($"Proceeding to: {currentState}");

        if (currentState == TutorialState.Chapter30_Complete)
        {
            isTutorialActive = false;
            obstacleManager.enabled = true;
            playerAudio.ResetPitchRTPC();
            Debug.Log("Tutorial Complete! Starting Main Game.");
            tutorialCompleteSound?.Post(gameObject);
        }

        int currentIndex = (int)currentState - 1;

        if (currentIndex < 0 || currentIndex >= instructionSounds.Length)
        {
            if (currentState == TutorialState.Chapter01_Intro) return;
            Debug.LogWarning($"Tutorial Manager: No instruction/spawn for state {currentState}.");
            return;
        }

        playerController.Teleport(spawnPoints[currentIndex].position, spawnPoints[currentIndex].rotation);
        instructionSounds[currentIndex]?.Post(gameObject);
    }

    public void ToggleAudioPause()
    {
        Debug.Log("ToggleAudioPause method was successfully called!");

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

    private void StopAndResumeAudio()
    {
        AkSoundEngine.StopAll(gameObject);
        if (isAudioPaused)
        {
            resumeEvent?.Post(gameObject);
            isAudioPaused = false;
        }
    }

    public bool IsTutorialActive()
    {
        return isTutorialActive;
    }
}