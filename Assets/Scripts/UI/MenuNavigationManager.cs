using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events; // <-- 1. ADD THIS NAMESPACE

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

    // 2. ADD THE UNITY EVENT
    [Header("Menu Events")]
    public UnityEvent OnMenuFullyClosed;

    private CustomInputActions _input;
    private GameObject _lastSelectedMainSettingsButton;
    private GameObject _activeSubWindow;
    private uint mainMenuOpenEventPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;

    private void Awake()
    {
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
        Debug.Log($"ToggleSettingsPanel CALLED at Time: {Time.unscaledTime}");

        if (mainSettingsPanel.activeSelf | _activeSubWindow != null)
        {
            // --- CLOSE EVERYTHING (NOW A COROUTINE) ---
            StartCoroutine(CloseMenuWithAudio());
        }
        else
        {
            // --- OPEN THE MAIN MENU WITH AUDIO ORCHESTRATION ---
            OpenMainMenuWithAudio();
        }
    }

    private IEnumerator CloseMenuWithAudio()
    {
        Debug.Log($" Coroutine STARTED at Time: {Time.unscaledTime}");

        // --- STEP 1: STOP ALL EXISTING AUDIO ---
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            AkSoundEngine.StopAll(audioManager.gameObject);
            Debug.Log($"---> Stopped all audio on AudioManager");
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
        Debug.Log($" EndOfFrame reached. Posting close event at Time: {Time.unscaledTime}");

        // --- STEP 3: POST THE CLOSE EVENT ---
        if (mainMenuCloseEvent != null)
        {
            mainMenuCloseEvent.Post(this.gameObject);
            Debug.Log($"---> Posted main menu close event");
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
        // Adjust "0.5f" to be the length of your mainMenuCloseEvent sound.
        yield return new WaitForSecondsRealtime(0.5f);
        Debug.Log($" Unscaled wait finished. Resuming game time at Time: {Time.unscaledTime}");

        // --- STEP 6: UNPAUSE THE GAME (LAST) ---
        Time.timeScale = 1f;

        // --- 3. STEP 7: INVOKE EVENT ---
        // Notify any listeners that the menu is now fully closed and game is un-paused.
        Debug.Log("Menu fully closed, invoking OnMenuFullyClosed event.");
        OnMenuFullyClosed?.Invoke();
    }

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
            mainMenuOpenEventPlayingID = mainMenuOpenEvent.Post(
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
            Debug.LogWarning($"[MenuNavManager] Missing references for audio orchestration:");
            Debug.LogWarning($"  - AudioManager: {(audioManager != null ? "OK" : "NULL")}");
            Debug.LogWarning($"  - mainMenuOpenEvent: {(mainMenuOpenEvent != null ? "OK" : "NULL")}");
            Debug.LogWarning($"  - firstElement: {(firstElement != null ? "OK" : "NULL")}");

            if (firstElement != null)
            {
                EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
            }
        }
    }

    private void ManuallyTriggerButtonAudio(WwiseMainMenuButton button, AudioManager audioManager)
    {
        AudioEventChannelSO.WwiseEventPacket packet = new AudioEventChannelSO.WwiseEventPacket
        {
            WwiseEvent = button.selectionEvent,
            WwiseSwitch = button.normalSwitch,
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

    /// <summary>
    /// Closes both subwindow and main menu entirely. Now starts the audio coroutine.
    /// </summary>
    public void CloseEntireMenu()
    {
        // 4. --- REPLACED LOGIC ---
        // Instead of synchronous, silent closing, just call the
        // same coroutine that the "Esc" key uses.
        // This will play the audio, wait, un-pause, and fire the
        // OnMenuFullyClosed event, all in the correct order.
        Debug.Log("CloseEntireMenu called - starting CloseMenuWithAudio coroutine...");
        StartCoroutine(CloseMenuWithAudio());
    }
}