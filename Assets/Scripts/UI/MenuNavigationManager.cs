using UnityEngine;
using UnityEngine.EventSystems; // Required for EventSystem
using UnityEngine.InputSystem; // Required for InputAction.CallbackContext
using UnityEngine.UI; // Required for Selectable
using System.Collections; // Required for Coroutines

public class MenuNavigationManager : MonoBehaviour
{
    // Assign in Inspector
    public GameObject mainSettingsPanel;
    public CustomUISubmitHandler customTabSubmitHandler;

    // Assign ALL sub-panels in the Inspector
    public GameObject[] subWindowPanels; // Changed back to array if needed, adjust if not

    private CustomInputActions _input;
    private GameObject _lastSelectedMainSettingsButton;
    private GameObject _activeSubWindow;

    [Header("Main Menu Audio")]
    public AK.Wwise.Event mainMenuOpenEvent;
    public AK.Wwise.Event stopUISelectionEvent; 
    public AK.Wwise.Event mainMenuCloseEvent;

    private uint mainMenuOpenEventPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID; // ADD THIS

    private void Awake()
    {
        // Initialize the Input Actions class
        _input = new CustomInputActions();

        // Initialize all sub-window handlers
        // This gives them a reference to the input asset
        foreach (GameObject panel in subWindowPanels)
        {
            SubWindowInputHandler handler = panel.GetComponent<SubWindowInputHandler>();
            if (handler != null)
            {
                handler.Initialize(_input, panel);
                handler.enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        _input.UI.Enable();
        _input.Player.Enable();
        _input.Player.ToggleSettingsMenu.performed += ToggleSettingsPanel;
        _input.UI.ToggleSettingsMenu.performed += ToggleSettingsPanel;

        // Make sure the main submit handler is enabled when the menu opens
        if (customTabSubmitHandler != null)
        {
            customTabSubmitHandler.enabled = true; // Enable the component
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
        // Ensure the main submit handler is disabled when the menu closes entirely
        if (customTabSubmitHandler != null)
        {
            customTabSubmitHandler.enabled = false; // Disable the component
        }
    }

    private void ToggleSettingsPanel(InputAction.CallbackContext context)
    {
        Debug.Log($"ToggleSettingsPanel CALLED at Time: {Time.unscaledTime}");

        if (mainSettingsPanel.activeSelf | _activeSubWindow != null)
        {

            if (mainMenuCloseEvent != null)
            {
                mainMenuCloseEvent.Post(this.gameObject);
                Debug.Log($"---> Posted main menu close event");
            }
            // --- CLOSE EVERYTHING ---
            mainSettingsPanel.SetActive(false);
            if (_activeSubWindow != null)
            {
                SubWindowInputHandler handler = _activeSubWindow.GetComponent<SubWindowInputHandler>();
                if (handler != null) handler.enabled = false;
                _activeSubWindow.SetActive(false);
                _activeSubWindow = null;
            }
            if (customTabSubmitHandler != null) customTabSubmitHandler.enabled = false;
            Time.timeScale = 1f;
            _input.Player.Enable();
            EventSystem.current.SetSelectedGameObject(null);
        }
        else
        {
            // --- OPEN THE MAIN MENU WITH AUDIO ORCHESTRATION ---
            OpenMainMenuWithAudio(); // ? CHANGED: Call the orchestration method
        }
    }

    /// <summary>
    /// Opens the main menu with proper audio sequencing.
    /// Window open sound plays first, then first element selection sound.
    /// </summary>
    private void OpenMainMenuWithAudio()
    {
        Debug.Log($"[MenuNavManager] OpenMainMenuWithAudio CALLED at Time: {Time.unscaledTime}");

        AudioManager audioManager = AudioManager.Instance;
        Selectable firstElement = mainSettingsPanel.GetComponentInChildren<Selectable>();

        mainSettingsPanel.SetActive(true);

        if (customTabSubmitHandler != null)
            customTabSubmitHandler.enabled = true;

        Time.timeScale = 0f;
        _input.Player.Disable();

        if (audioManager != null && mainMenuOpenEvent != null && firstElement != null)
        {
            Debug.Log($"[MenuNavManager] All references valid, starting audio orchestration");

            audioManager.ClearPendingSelectionAudio();
            audioManager.SetAudioState(UIAudioState.Window_Opening);

            uint flags = (uint)AkCallbackType.AK_EndOfEvent;
            mainMenuOpenEventPlayingID = mainMenuOpenEvent.Post( // STORE THE PLAYING ID
                this.gameObject,
                flags,
                OnMainMenuAudioFinished,
                null
            );

            Debug.Log($"[MenuNavManager] Posted mainMenuOpenEvent, PlayingID: {mainMenuOpenEventPlayingID}");

            EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
            Debug.Log($"[MenuNavManager] Selected first element: {firstElement.name}");

            WwiseMainMenuButton buttonScript = firstElement.GetComponent<WwiseMainMenuButton>();
            if (buttonScript != null)
            {
                Debug.Log($"[MenuNavManager] Manually caching button audio");
                ManuallyTriggerButtonAudio(buttonScript, audioManager);
            }
        }
        else
        {
            Debug.LogWarning($"[MenuNavManager] Missing references for audio orchestration");
            if (firstElement != null)
            {
                EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
            }
        }
    }

    /// <summary>
    /// Manually triggers the button audio and caches it in the AudioManager.
    /// This is needed because OnSelect doesn't fire for initial menu opening.
    /// </summary>
    private void ManuallyTriggerButtonAudio(WwiseMainMenuButton button, AudioManager audioManager)
    {
        // Create the packet manually (same logic as button's OnSelect)
        AudioEventChannelSO.WwiseEventPacket packet = new AudioEventChannelSO.WwiseEventPacket
        {
            WwiseEvent = button.selectionEvent,
            WwiseSwitch = button.normalSwitch, // Always use normal switch for initial open
            Emitter = button.gameObject
        };

        // Raise it through the audio channel (will get cached since gate is closed)
        if (button.audioChannel != null)
        {
            button.audioChannel.RaiseEvent(packet);
        }
    }

    /// <summary>
    /// Callback when main menu window open sound finishes.
    /// </summary>
    /// <summary>
    /// Callback when main menu window open sound finishes.
    /// </summary>

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

    /// <summary>
    /// Waits one frame to ensure OnSelect has been called, then plays pending audio.
    /// </summary>
    private IEnumerator PlayPendingAudioAfterFrame()
    {
        yield return null; // Wait one frame for OnSelect to fire

        Debug.Log($"[MenuNavManager] Coroutine: Playing pending audio and releasing lock");

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.PlayPendingSelectionAudio();
            audioManager.SetAudioState(UIAudioState.Idle);
        }
    }

    /// <summary>
    /// Called by buttons on MainSettings panel.
    /// *** REVERTED VERSION - DOES NOT CHECK FOR ObstaclesSubwindow ***
    /// </summary>
    public void OpenSubWindow(GameObject subWindowToShow)
    {
        Debug.Log($"OpenSubWindow STARTED for {subWindowToShow.name} at Time: {Time.unscaledTime}");

        _lastSelectedMainSettingsButton = EventSystem.current.currentSelectedGameObject;

        // NEW: Stop the main menu opening event to prevent callback interference
        if (mainMenuOpenEventPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.StopPlayingID(mainMenuOpenEventPlayingID);
            mainMenuOpenEventPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
            Debug.Log($"---> Stopped main menu opening event");
        }

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            // Stop all audio on AudioManager
            AkSoundEngine.StopAll(audioManager.gameObject);

            // Clear any pending selection audio
            audioManager.ClearPendingSelectionAudio();

            // Reset state to Idle
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

    // Coroutine to check selection slightly later (moved here from ObstaclesSubwindow)
    private IEnumerator CheckSelectionAfterFrame(GameObject expectedSelection)
    {
        yield return null; // Wait one frame
        GameObject currentlySelected = EventSystem.current?.currentSelectedGameObject;
        if (currentlySelected == expectedSelection)
        {
            Debug.Log($"--> MenuNavManager.CheckSelectionAfterFrame: Selection SUCCESSFUL. Current selection: {currentlySelected.name}");
        }
        else
        {
            Debug.LogWarning($"--> MenuNavManager.CheckSelectionAfterFrame: Selection FAILED or CHANGED. Expected '{expectedSelection.name}', but current is '{(currentlySelected == null ? "null" : currentlySelected.name)}'");
            // If selection failed, maybe try setting it again? Or log more info.
            // EventSystem.current.SetSelectedGameObject(expectedSelection);
        }
    }

    // --- COROUTINE FUNCTION ---
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
    // --- END COROUTINE ---


    /// <summary>
    /// Called by SubWindowInputHandler to return to the main menu.
    /// </summary>
    public void CloseActiveSubWindow()
    {
        Debug.Log($"CloseActiveSubWindow CALLED at Time: {Time.unscaledTime}");

        if (_activeSubWindow == null) return;

        // 1. Disable Sub Handler
        SubWindowInputHandler handler = _activeSubWindow.GetComponent<SubWindowInputHandler>();
        if (handler != null) handler.enabled = false;

        // NEW: Stop any pending subwindow audio callbacks
        BaseSubwindow subwindow = _activeSubWindow.GetComponent<BaseSubwindow>();
        if (subwindow != null)
        {
            subwindow.StopWindowAudio();
            Debug.Log($"---> Stopped subwindow audio callbacks");
        }

        // 3. Enable Main Handler
        if (customTabSubmitHandler != null)
        {
            Debug.Log($"---> Re-enabling customTabSubmitHandler component at Time: {Time.unscaledTime}");
            customTabSubmitHandler.enabled = true;
        }

        // 4. Swap Panels
        _activeSubWindow.SetActive(false);
        mainSettingsPanel.SetActive(true);
        _activeSubWindow = null;

        // 5. Set return context flag
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            // Clear any pending audio and reset state before setting return flag
            audioManager.ClearPendingSelectionAudio();
            audioManager.SetAudioState(UIAudioState.Idle); // Make sure gate is open
            audioManager.SetReturningToMainMenu(true);
            Debug.Log($"---> Set returning flag, state reset to Idle");
        }

        // 6. Restore focus (will trigger audio with return context)
        if (_lastSelectedMainSettingsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_lastSelectedMainSettingsButton);
        }
    }
}