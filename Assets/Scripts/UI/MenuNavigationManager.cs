using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

public class MenuNavigationManager : MonoBehaviour
{
    // Assign in Inspector
    public GameObject mainSettingsPanel;
    public CustomUISubmitHandler customTabSubmitHandler;

    // Assign ALL sub-panels in the Inspector
    public GameObject[] subWindowPanels;

    [Header("Main Menu Audio")]
    public AK.Wwise.Event mainMenuOpenEvent;
    public AK.Wwise.Event mainMenuCloseEvent;
    public AK.Wwise.Event stopUISelectionEvent;

    [Header("Menu Events")]
    public UnityEvent OnMenuFullyClosed;

    [Header("Tutorial Audio Settings")]
    [Tooltip("Should tutorial audio pause when menus open?")]
    public bool pauseTutorialWhenMenuOpens = true;

    private CustomInputActions _input;
    private GameObject _lastSelectedMainSettingsButton;
    private GameObject _activeSubWindow;
    private uint mainMenuOpenEventPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;

    // Track if we paused tutorial audio
    private bool didPauseTutorial = false;

    private void Awake()
    {
        Debug.Log("[MenuNavManager] Awake() called");

        // Initialize the Input Actions class
        _input = new CustomInputActions();

        // Initialize all sub-window handlers
        foreach (GameObject panel in subWindowPanels)
        {
            SubWindowInputHandler handler = panel.GetComponent<SubWindowInputHandler>();
            if (handler != null)
            {
                handler.Initialize(_input, panel);
                handler.enabled = false;
            }
        }

        // Initialize the event
        if (OnMenuFullyClosed == null)
            OnMenuFullyClosed = new UnityEvent();

        Debug.Log($"[MenuNavManager] pauseTutorialWhenMenuOpens setting: {pauseTutorialWhenMenuOpens}");
    }

    private void OnEnable()
    {
        _input.UI.Enable();
        _input.Player.Enable();
        _input.Player.ToggleSettingsMenu.performed += ToggleSettingsPanel;
        _input.UI.ToggleSettingsMenu.performed += ToggleSettingsPanel;

        if (customTabSubmitHandler != null)
        {
            customTabSubmitHandler.enabled = true;
        }
    }

    private void OnDisable()
    {
        _input.UI.Disable();
        _input.Player.Disable();

        if (_input != null)
        {
            _input.Player.ToggleSettingsMenu.performed -= ToggleSettingsPanel;
            _input.UI.ToggleSettingsMenu.performed -= ToggleSettingsPanel;
        }

        if (_activeSubWindow != null)
        {
            Time.timeScale = 1f;
        }

        if (customTabSubmitHandler != null)
        {
            customTabSubmitHandler.enabled = false;
        }
    }

    private void ToggleSettingsPanel(InputAction.CallbackContext context)
    {
        Debug.Log($"[MenuNavManager] ToggleSettingsPanel CALLED at Time: {Time.unscaledTime}");

        if (mainSettingsPanel.activeSelf | _activeSubWindow != null)
        {
            Debug.Log("[MenuNavManager] Menu is open, starting CloseMenuWithAudio...");
            StartCoroutine(CloseMenuWithAudio());
        }
        else
        {
            Debug.Log("[MenuNavManager] Menu is closed, calling OpenMainMenuWithAudio...");
            OpenMainMenuWithAudio();
        }
    }

    private IEnumerator CloseMenuWithAudio()
    {
        Debug.Log($"[MenuNavManager] ??? CloseMenuWithAudio Coroutine STARTED ???");

        // --- STEP 1: STOP ALL EXISTING AUDIO ---
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            AkSoundEngine.StopAll(audioManager.gameObject);
            Debug.Log($"[MenuNavManager] Stopped all audio on AudioManager");
        }

        if (mainMenuOpenEventPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.StopPlayingID(mainMenuOpenEventPlayingID);
            mainMenuOpenEventPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
        }

        if (_activeSubWindow != null)
        {
            BaseSubwindow subwindow = _activeSubWindow.GetComponent<BaseSubwindow>();
            if (subwindow != null)
            {
                subwindow.StopWindowAudio();
            }
        }

        // --- STEP 2: CRITICAL DECOUPLING ---
        yield return new WaitForEndOfFrame();
        Debug.Log($"[MenuNavManager] EndOfFrame reached. Posting close event at Time: {Time.unscaledTime}");

        // --- STEP 3: POST THE CLOSE EVENT ---
        if (mainMenuCloseEvent != null)
        {
            mainMenuCloseEvent.Post(this.gameObject);
            Debug.Log($"[MenuNavManager] Posted main menu close event");
        }

        // --- STEP 4: DEACTIVATE PANELS & CLEAN UP (IMMEDIATE) ---
        mainSettingsPanel.SetActive(false);
        if (_activeSubWindow != null)
        {
            SubWindowInputHandler handler = _activeSubWindow.GetComponent<SubWindowInputHandler>();
            if (handler != null) handler.enabled = false;

            _activeSubWindow.SetActive(false);
            _activeSubWindow = null;
        }

        if (customTabSubmitHandler != null) customTabSubmitHandler.enabled = false;
        _input.Player.Enable();
        EventSystem.current.SetSelectedGameObject(null);

        if (audioManager != null)
        {
            audioManager.ClearPendingSelectionAudio();
            audioManager.SetAudioState(UIAudioState.Idle);
        }

        // --- STEP 5: WAIT FOR SOUND TO PLAY BEFORE UNPAUSING ---
        yield return new WaitForSecondsRealtime(0.5f);
        Debug.Log($"[MenuNavManager] Unscaled wait finished. Resuming game time at Time: {Time.unscaledTime}");

        // --- STEP 6: UNPAUSE THE GAME (LAST) ---
        Time.timeScale = 1f;
        Debug.Log($"[MenuNavManager] Time.timeScale set to 1");

        // --- STEP 6.5: RESUME TUTORIAL AUDIO IF WE PAUSED IT ---
        Debug.Log($"[MenuNavManager] Checking tutorial audio resume...");
        Debug.Log($"[MenuNavManager]   didPauseTutorial: {didPauseTutorial}");
        Debug.Log($"[MenuNavManager]   TutorialAudioController.Instance exists: {TutorialAudioController.Instance != null}");

        if (didPauseTutorial && TutorialAudioController.Instance != null)
        {
            Debug.Log("[MenuNavManager] >>> Calling TutorialAudioController.ForceResume()");
            TutorialAudioController.Instance.ForceResume();
            didPauseTutorial = false;
            Debug.Log("[MenuNavManager] >>> Tutorial audio resume complete");
        }
        else
        {
            Debug.Log("[MenuNavManager] >>> Not resuming tutorial audio (either we didn't pause it, or controller doesn't exist)");
        }

        // --- STEP 7: INVOKE EVENT ---
        Debug.Log("[MenuNavManager] Menu fully closed, invoking OnMenuFullyClosed event");
        OnMenuFullyClosed?.Invoke();

        Debug.Log($"[MenuNavManager] ??? CloseMenuWithAudio Coroutine COMPLETE ???");
    }

    private void OpenMainMenuWithAudio()
    {
        Debug.Log($"[MenuNavManager] ??? OpenMainMenuWithAudio CALLED ???");

        // --- PAUSE TUTORIAL AUDIO WHEN MENU OPENS ---
        Debug.Log($"[MenuNavManager] Checking if should pause tutorial audio...");
        Debug.Log($"[MenuNavManager]   pauseTutorialWhenMenuOpens: {pauseTutorialWhenMenuOpens}");
        Debug.Log($"[MenuNavManager]   TutorialAudioController.Instance exists: {TutorialAudioController.Instance != null}");

        if (pauseTutorialWhenMenuOpens && TutorialAudioController.Instance != null)
        {
            bool isPaused = TutorialAudioController.Instance.IsPaused();
            Debug.Log($"[MenuNavManager]   Tutorial is currently paused: {isPaused}");

            if (!isPaused)
            {
                Debug.Log("[MenuNavManager] >>> Calling TutorialAudioController.ForcePause()");
                TutorialAudioController.Instance.ForcePause();
                didPauseTutorial = true;
                Debug.Log($"[MenuNavManager] >>> Tutorial audio paused, didPauseTutorial set to: {didPauseTutorial}");
            }
            else
            {
                Debug.Log("[MenuNavManager] >>> Tutorial already paused, not setting didPauseTutorial flag");
            }
        }
        else
        {
            Debug.Log("[MenuNavManager] >>> Not pausing tutorial audio (setting disabled or controller doesn't exist)");
        }

        AudioManager audioManager = AudioManager.Instance;
        Selectable firstElement = mainSettingsPanel.GetComponentInChildren<Selectable>();

        mainSettingsPanel.SetActive(true);
        Debug.Log("[MenuNavManager] Main settings panel activated");

        if (customTabSubmitHandler != null)
            customTabSubmitHandler.enabled = true;

        Time.timeScale = 0f;
        Debug.Log("[MenuNavManager] Time.timeScale set to 0");

        _input.Player.Disable();

        if (audioManager != null && mainMenuOpenEvent != null && firstElement != null)
        {
            Debug.Log($"[MenuNavManager] All references valid, starting audio orchestration");

            audioManager.ClearPendingSelectionAudio();
            audioManager.SetAudioState(UIAudioState.Window_Opening);

            uint flags = (uint)AkCallbackType.AK_EndOfEvent;
            mainMenuOpenEventPlayingID = mainMenuOpenEvent.Post(
                this.gameObject,
                flags,
                OnMainMenuAudioFinished,
                null
            );

            Debug.Log($"[MenuNavManager] Posted mainMenuOpenEvent, PlayingID: {mainMenuOpenEventPlayingID}");

            EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
            Debug.Log($"[MenuNavManager] Selected first element: {firstElement.name}");

            // Check for V2 component first (new standard)
            WwiseUIElementV2 buttonScriptV2 = firstElement.GetComponent<WwiseUIElementV2>();
            if (buttonScriptV2 != null)
            {
                Debug.Log($"[MenuNavManager] Manually caching button audio (WwiseUIElementV2)");
                ManuallyTriggerButtonAudioV2(buttonScriptV2, audioManager);
            }
            // Fallback to legacy component
            
        }
        else
        {
            Debug.LogWarning($"[MenuNavManager] Missing references for audio orchestration:");
            Debug.LogWarning($"  - AudioManager: {(audioManager != null ? "OK" : "NULL")}");
            Debug.LogWarning($"  - mainMenuOpenEvent: {(mainMenuOpenEvent != null ? "OK" : "NULL")}");
            Debug.LogWarning($"  - firstElement: {(firstElement != null ? "OK" : "NULL")}");

            if (firstElement != null)
            {
                EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
            }
        }

        Debug.Log($"[MenuNavManager] ??? OpenMainMenuWithAudio COMPLETE ???");
    }

    

    private void ManuallyTriggerButtonAudioV2(WwiseUIElementV2 button, AudioManager audioManager)
    {
        // Determine the correct switch based on button configuration
        AK.Wwise.Switch switchToUse = button.normalSwitch; // Default to normal (not returning)

        // If context-based mode is enabled, check the return flag
        if (button.useReturnContext && audioManager != null)
        {
            bool isReturning = audioManager.IsReturningToMainMenu();
            switchToUse = isReturning ? button.returnContextSwitch : button.normalSwitch;
        }

        AudioEventChannelSO.WwiseEventPacket packet = new AudioEventChannelSO.WwiseEventPacket
        {
            WwiseEvent = button.selectionEvent,
            WwiseSwitch = switchToUse,
            Emitter = button.gameObject
        };

        if (button.audioChannel != null)
        {
            button.audioChannel.RaiseEvent(packet);
        }
    }

    private void OnMainMenuAudioFinished(object in_cookie, AkCallbackType in_type, object in_info)
    {
        Debug.Log($"[MenuNavManager] OnMainMenuAudioFinished callback received, type: {in_type}");

        if (in_type == AkCallbackType.AK_EndOfEvent)
        {
            Debug.Log($"[MenuNavManager] Window audio finished, playing pending selection audio");

            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.PlayPendingSelectionAudio();
                audioManager.SetAudioState(UIAudioState.Idle);
            }
        }
    }

    public void OpenSubWindow(GameObject subWindowToShow)
    {
        Debug.Log($"OpenSubWindow STARTED for {subWindowToShow.name} at Time: {Time.unscaledTime}");

        _lastSelectedMainSettingsButton = EventSystem.current.currentSelectedGameObject;

        if (mainMenuOpenEventPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.StopPlayingID(mainMenuOpenEventPlayingID);
            mainMenuOpenEventPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
            Debug.Log($"---> Stopped main menu opening event");
        }

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            AkSoundEngine.StopAll(audioManager.gameObject);
            audioManager.ClearPendingSelectionAudio();
            audioManager.SetAudioState(UIAudioState.Idle);
            Debug.Log($"---> Cleared main menu audio state");
        }

        mainSettingsPanel.SetActive(false);
        _activeSubWindow = subWindowToShow;

        Time.timeScale = 0f;
        _input.Player.Disable();

        if (customTabSubmitHandler != null)
        {
            Debug.Log($"---> Disabling customTabSubmitHandler component at Time: {Time.unscaledTime}");
            customTabSubmitHandler.enabled = false;
        }

        BaseSubwindow subwindow = subWindowToShow.GetComponent<BaseSubwindow>();
        if (subwindow != null)
        {
            Debug.Log($"---> Found BaseSubwindow, calling OpenWindow()");
            subwindow.OpenWindow();
        }
        else
        {
            Debug.LogWarning($"---> No BaseSubwindow found on {subWindowToShow.name}, using fallback");
            subWindowToShow.SetActive(true);
            Selectable firstElement = subWindowToShow.GetComponentInChildren<Selectable>();
            if (firstElement != null)
            {
                EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
            }
        }

        SubWindowInputHandler handler = subWindowToShow.GetComponent<SubWindowInputHandler>();
        if (handler != null)
        {
            Debug.Log($"---> Starting Coroutine EnableSubHandlerAfterFrame at Time: {Time.unscaledTime}");
            handler.enabled = false;
            StartCoroutine(EnableSubHandlerAfterFrame(handler));
        }

        Debug.Log($"OpenSubWindow FINISHED at Time: {Time.unscaledTime}");
    }

    private IEnumerator EnableSubHandlerAfterFrame(SubWindowInputHandler handlerToEnable)
    {
        Debug.Log($"---> Coroutine WAITING for EndOfFrame at Time: {Time.unscaledTime}");
        yield return new WaitForEndOfFrame();
        Debug.Log($"---> Coroutine RESUMED after EndOfFrame at Time: {Time.unscaledTime}");
        if (handlerToEnable != null)
        {
            Debug.Log($"---> Enabling handler {handlerToEnable.gameObject.name} at Time: {Time.unscaledTime}");
            handlerToEnable.enabled = true;
        }
    }

    public void CloseActiveSubWindow()
    {
        Debug.Log($"CloseActiveSubWindow CALLED at Time: {Time.unscaledTime}");

        if (_activeSubWindow == null) return;

        SubWindowInputHandler handler = _activeSubWindow.GetComponent<SubWindowInputHandler>();
        if (handler != null) handler.enabled = false;

        BaseSubwindow subwindow = _activeSubWindow.GetComponent<BaseSubwindow>();
        if (subwindow != null)
        {
            subwindow.StopWindowAudio();
            Debug.Log($"---> Stopped subwindow audio callbacks");
        }

        if (customTabSubmitHandler != null)
        {
            Debug.Log($"---> Re-enabling customTabSubmitHandler component at Time: {Time.unscaledTime}");
            customTabSubmitHandler.enabled = true;
        }

        _activeSubWindow.SetActive(false);
        mainSettingsPanel.SetActive(true);
        _activeSubWindow = null;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.ClearPendingSelectionAudio();
            audioManager.SetAudioState(UIAudioState.Idle);
            audioManager.SetReturningToMainMenu(true);
            Debug.Log($"---> Set returning flag, state reset to Idle");
        }

        if (_lastSelectedMainSettingsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_lastSelectedMainSettingsButton);
        }
    }

    public void CloseEntireMenu()
    {
        Debug.Log("CloseEntireMenu called - starting CloseMenuWithAudio coroutine...");
        StartCoroutine(CloseMenuWithAudio());
    }
}