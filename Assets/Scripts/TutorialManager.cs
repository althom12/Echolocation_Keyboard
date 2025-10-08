using UnityEngine;
using StarterAssets;

public class TutorialManager : MonoBehaviour
{
    // --- MODIFIED: Expanded the enum to match your 7 stages ---
    public enum TutorialState
    {
        Intro,
        BasicMechanics,
        RotationAndCompass,
        SpawnPositions,
        Obstacles,
        AdvancedMechanics,
        Complete // The final state after the last instruction
    }

    // --- MODIFIED: Expanded the replay enum ---
    private enum ReplayState
    {
        Off,
        Playing_Basic,
        Playing_Rotation,
        Playing_Spawns,
        Playing_Obstacles,
        Playing_Advanced
    }

    [Header("Core References")]
    public FirstPersonController playerController;
    public ObstacleManager obstacleManager;
    public PlayerAudio playerAudio;

    [Header("Audio Events")]
    [Tooltip("Plays when the game opens (e.g., Play_01_Intro)")]
    public AK.Wwise.Event introductoryAudio;
    [Tooltip("Assign all 7 instructional sounds in order, starting with Basic Mechanics.")]
    public AK.Wwise.Event[] instructionSounds;

    [Header("Pause/Resume Events")]
    public AK.Wwise.Event pauseEvent;
    public AK.Wwise.Event resumeEvent;

    [Header("Tutorial Settings")]
    [Tooltip("Assign all 7 spawn points in order.")]
    public Transform[] spawnPoints;

    private TutorialState currentState;
    private bool isAudioPaused = false;
    private ReplayState _currentReplayState = ReplayState.Off;

    void Start()
    {
        if (playerAudio == null || obstacleManager == null || playerController == null)
        {
            Debug.LogError("TUTORIAL MANAGER ERROR: One or more core references are not assigned in the Inspector!", this.gameObject);
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

        // Handles pausing/resuming with Spacebar (via ObstacleLocatorAudio script)

        // Handles replaying instructions with Tab key
        if (currentState == TutorialState.Complete && Input.GetKeyDown(KeyCode.Tab))
        {
            CycleInstructionReplay();
        }
    }

    // --- MODIFIED: Expanded replay logic for the new stages ---
    void CycleInstructionReplay()
    {
        AkSoundEngine.StopAll(gameObject);
        if (isAudioPaused)
        {
            resumeEvent?.Post(gameObject);
            isAudioPaused = false;
        }
        switch (_currentReplayState)
        {
            case ReplayState.Off:
                _currentReplayState = ReplayState.Playing_Basic;
                instructionSounds[0]?.Post(gameObject); // Basic Mechanics
                Debug.Log("Replaying: Basic Mechanics");
                break;
            case ReplayState.Playing_Basic:
                _currentReplayState = ReplayState.Playing_Rotation;
                instructionSounds[1]?.Post(gameObject); // Rotation and Compass
                Debug.Log("Replaying: Rotation and Compass");
                break;
            case ReplayState.Playing_Rotation:
                _currentReplayState = ReplayState.Playing_Spawns;
                instructionSounds[2]?.Post(gameObject); // Spawn Positions
                Debug.Log("Replaying: Spawn Positions");
                break;
            case ReplayState.Playing_Spawns:
                _currentReplayState = ReplayState.Playing_Obstacles;
                instructionSounds[3]?.Post(gameObject); // Obstacles
                Debug.Log("Replaying: Obstacles");
                break;
            case ReplayState.Playing_Obstacles:
                _currentReplayState = ReplayState.Playing_Advanced;
                instructionSounds[4]?.Post(gameObject); // Advanced Mechanics
                Debug.Log("Replaying: Advanced Mechanics");
                break;
            case ReplayState.Playing_Advanced:
                _currentReplayState = ReplayState.Off;
                Debug.Log("Instruction Replay Stopped.");
                break;
        }
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

    // --- MODIFIED: Greatly expanded the state machine for all 7 stages ---
    void GoToNextState()
    {
        AkSoundEngine.StopAll(gameObject);
        _currentReplayState = ReplayState.Off;
        if (isAudioPaused)
        {
            resumeEvent?.Post(gameObject);
            isAudioPaused = false;
        }

        switch (currentState)
        {
            case TutorialState.Intro:
                currentState = TutorialState.BasicMechanics;
                playerController.Teleport(spawnPoints[0].position, spawnPoints[0].rotation);
                instructionSounds[0]?.Post(gameObject); // Play_02_BasicMechanics
                Debug.Log("Tutorial Started: Basic Mechanics.");
                break;

            case TutorialState.BasicMechanics:
                currentState = TutorialState.RotationAndCompass;
                playerController.Teleport(spawnPoints[1].position, spawnPoints[1].rotation);
                instructionSounds[1]?.Post(gameObject); // Play_03_RotationAndSonicCompass
                Debug.Log("Proceeding to: Rotation and Compass.");
                break;

            case TutorialState.RotationAndCompass:
                currentState = TutorialState.SpawnPositions;
                playerController.Teleport(spawnPoints[2].position, spawnPoints[2].rotation);
                instructionSounds[2]?.Post(gameObject); // Play_04_SpawnPositions
                Debug.Log("Proceeding to: Spawn Positions.");
                break;

            case TutorialState.SpawnPositions:
                currentState = TutorialState.Obstacles;
                playerController.Teleport(spawnPoints[3].position, spawnPoints[3].rotation);
                instructionSounds[3]?.Post(gameObject); // Play_05_Obstacles
                Debug.Log("Proceeding to: Obstacles.");
                break;

            case TutorialState.Obstacles:
                currentState = TutorialState.AdvancedMechanics;
                playerController.Teleport(spawnPoints[4].position, spawnPoints[4].rotation);
                instructionSounds[4]?.Post(gameObject); // Play_06_AdvancedMechanics
                Debug.Log("Proceeding to: Advanced Mechanics.");
                break;

            case TutorialState.AdvancedMechanics:
                currentState = TutorialState.Complete;
                playerController.Teleport(spawnPoints[5].position, spawnPoints[5].rotation);
                instructionSounds[5]?.Post(gameObject); // Play_07_MainGame
                // Assuming obstacles are active in the main game area
                obstacleManager.enabled = true;
                playerAudio.ResetPitchRTPC();
                Debug.Log("Tutorial Complete! Starting Main Game.");
                break;

            case TutorialState.Complete:
                Debug.Log("Tutorial is already complete.");
                break;
        }
    }
}