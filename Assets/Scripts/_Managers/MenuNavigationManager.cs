using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Menu Navigation Manager - FINAL FIX
/// Forces OnSelect to fire by clearing selection first.
/// </summary>
public class MenuNavigationManager : MonoBehaviour
{
    [Header("Main Menu")]
    public GameObject mainMenuPanel;
    public GameObject firstMainMenuButton;

    [Header("Subwindow Panels")]
    public GameObject tutorialPropertiesPanel;
    public GameObject obstaclesPanel;
    public GameObject landmarksPanel;

    [Header("First Selectables in Subwindows")]
    public GameObject tutorialFirstSelectable;
    public GameObject obstaclesFirstSelectable;
    public GameObject landmarksFirstSelectable;

    [Header("Audio")]
    public AK.Wwise.Event menuOpenEvent;
    public AK.Wwise.Event menuCloseEvent;

    [Header("Subwindow Controllers")]
    public BaseSubwindow tutorialSubwindow;
    public BaseSubwindow obstaclesSubwindow;
    public BaseSubwindow landmarksSubwindow;

    [Header("Input Handler")]
    public SubWindowInputHandler subWindowInputHandler;

    public UnityEvent OnMenuFullyClosed;

    private CustomInputActions _input;
    private GameObject _currentActiveSubwindow;
    private bool _isMenuOpen = false;
    private bool _isClosingMenu = false;

    private GameObject _lastSelectedMainMenuButton;

    // Track menu open audio playing ID
    private uint _menuOpenPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;

    private void Awake()
    {
        _input = new CustomInputActions();
    }

    private void Start()
    {
        mainMenuPanel.SetActive(false);
        tutorialPropertiesPanel.SetActive(false);
        obstaclesPanel.SetActive(false);
        landmarksPanel.SetActive(false);
    }

    private void OnEnable()
    {
        _input.UI.ToggleSettingsMenu.performed += OnToggleMenuPerformed;
        _input.UI.Enable();
    }

    private void OnDisable()
    {
        _input.UI.ToggleSettingsMenu.performed -= OnToggleMenuPerformed;
        _input.UI.Disable();
    }

    private void OnToggleMenuPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (_isClosingMenu) return;

        if (!_isMenuOpen)
        {
            OpenMainMenu();
        }
        else
        {
            CloseEntireMenu();
        }
    }

    /// <summary>
    /// Opens the main menu with proper audio sequencing.
    /// </summary>
    public void OpenMainMenu()
    {
        if (_isMenuOpen) return;

        Debug.Log("[MenuNavigationManager] Opening main menu");

        Time.timeScale = 0f;
        _isMenuOpen = true;

        mainMenuPanel.SetActive(true);

        // Pause tutorial audio if active
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            if (TutorialManager.Instance.pauseStateManager != null)
            {
                TutorialManager.Instance.pauseStateManager.SetToSecondaryState(); // Paused
                Debug.Log("[MenuNavigationManager] Tutorial audio paused (menu opened)");
            }
        }

        // 1. SETUP AUDIO MANAGER IMMEDIATELY (Sync)
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.SetReturningToMainMenu(false);
            Debug.Log("[MenuNavigationManager] Setting audio gate to Window_Opening (IMMEDIATE)");
            audioManager.ClearPendingSelectionAudio();
            audioManager.SetAudioState(UIAudioState.Window_Opening);
        }

        // 2. Start the sequence for selection and callbacks
        StartCoroutine(OpenMainMenuSequence());
    }

    /// <summary>
    /// Handles the main menu opening sequence with proper timing.
    /// </summary>
    private IEnumerator OpenMainMenuSequence()
    {
        // Frame 1: Wait for mainMenuPanel to fully activate
        yield return null;

        // ---------------------------------------------------------
        // FIX: FORCE DESELECT
        // We must clear the EventSystem's current object first. 
        // If we don't, and the system 'remembers' the button was last selected,
        // SetSelectedGameObject() will NOT fire OnSelect, and no audio packet will be sent.
        // ---------------------------------------------------------
        EventSystem.current.SetSelectedGameObject(null);

        // Select first button
        if (firstMainMenuButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstMainMenuButton);
            Debug.Log("[MenuNavigationManager] First button selected (Forced refresh)");
        }

        // Frame 2: Wait for button's OnSelect to fire and audio to be cached
        yield return null;
        Debug.Log("[MenuNavigationManager] Second frame passed, checking if audio was cached");

        // Post menu open event with callback
        if (menuOpenEvent != null && menuOpenEvent.IsValid())
        {
            Debug.Log("[MenuNavigationManager] Posting menu open event with callback");
            uint flags = (uint)AkCallbackType.AK_EndOfEvent;
            _menuOpenPlayingID = menuOpenEvent.Post(
                gameObject,
                flags,
                OnMenuOpenAudioFinished,
                null
            );
        }
        else
        {
            // No menu open audio - immediately play cached selection audio
            Debug.LogWarning("[MenuNavigationManager] No menuOpenEvent assigned, playing cached audio immediately");
            if (AudioManager.Instance != null)
            {
                StartCoroutine(PlayCachedSelectionAudioAfterFrame());
            }
        }
    }

    private void OnMenuOpenAudioFinished(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_EndOfEvent)
        {
            Debug.Log("[MenuNavigationManager] Menu open audio finished, playing cached selection audio");
            _menuOpenPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(PlayCachedSelectionAudioAfterFrame());
            }
        }
    }

    private IEnumerator PlayCachedSelectionAudioAfterFrame()
    {
        yield return null;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            Debug.Log("[MenuNavigationManager] Playing pending selection audio and resetting gate to Idle");
            audioManager.PlayPendingSelectionAudio();
            audioManager.SetAudioState(UIAudioState.Idle);
        }
    }

    private void StopMainMenuAudio()
    {
        if (_menuOpenPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            Debug.Log("[MenuNavigationManager] Stopping main menu open audio");
            AkSoundEngine.StopPlayingID(_menuOpenPlayingID);
            _menuOpenPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
        }
    }

    public void OpenSubwindowByGameObject(GameObject subwindowPanel)
    {
        if (subwindowPanel == null)
        {
            Debug.LogError("[MenuNavigationManager] Subwindow panel is null!");
            return;
        }

        Debug.Log($"[MenuNavigationManager] OpenSubwindowByGameObject called: {subwindowPanel.name}");

        BaseSubwindow targetSubwindow = subwindowPanel.GetComponent<BaseSubwindow>();
        if (targetSubwindow == null)
        {
            Debug.LogError($"[MenuNavigationManager] Panel '{subwindowPanel.name}' does not have a BaseSubwindow component!");
            return;
        }

        _lastSelectedMainMenuButton = EventSystem.current.currentSelectedGameObject;
        Debug.Log($"[MenuNavigationManager] Remembering last selected button: {(_lastSelectedMainMenuButton != null ? _lastSelectedMainMenuButton.name : "NULL")}");

        mainMenuPanel.SetActive(false);
        _currentActiveSubwindow = subwindowPanel;

        if (subWindowInputHandler != null)
        {
            Debug.Log($"[MenuNavigationManager] Initializing SubWindowInputHandler for {subwindowPanel.name}");
            subWindowInputHandler.Initialize(_input, subwindowPanel, targetSubwindow);
        }
        else
        {
            Debug.LogError("[MenuNavigationManager] SubWindowInputHandler is NULL!");
        }

        targetSubwindow.OpenWindow();
    }

    public void CloseActiveSubWindow()
    {
        if (_currentActiveSubwindow == null)
        {
            Debug.LogWarning("[MenuNavigationManager] No active subwindow to close");
            return;
        }

        Debug.Log("[MenuNavigationManager] Closing active subwindow");

        BaseSubwindow subwindow = _currentActiveSubwindow.GetComponent<BaseSubwindow>();
        if (subwindow != null)
        {
            subwindow.StopWindowAudio();
        }

        _currentActiveSubwindow.SetActive(false);
        _currentActiveSubwindow = null;

        // FIX: Force unlock the gate in case the subwindow left it closed
        if (AudioManager.Instance != null)
        {
            Debug.Log("[MenuNavigationManager] Forcing Audio State to Idle (Subwindow Closed)");
            AudioManager.Instance.ClearPendingSelectionAudio();
            AudioManager.Instance.SetAudioState(UIAudioState.Idle);
        }

        mainMenuPanel.SetActive(true);

        AudioManager.Instance?.SetReturningToMainMenu(true);

        if (_lastSelectedMainMenuButton != null)
        {
            Debug.Log($"[MenuNavigationManager] Returning to last selected button: {_lastSelectedMainMenuButton.name}");
            EventSystem.current.SetSelectedGameObject(_lastSelectedMainMenuButton);
        }
        else if (firstMainMenuButton != null)
        {
            Debug.Log("[MenuNavigationManager] No last selected button, using first main menu button");
            EventSystem.current.SetSelectedGameObject(firstMainMenuButton);
        }
    }

    public void CloseEntireMenu()
    {
        if (_isClosingMenu) return;
        _isClosingMenu = true;

        Debug.Log("[MenuNavigationManager] CloseEntireMenu called");

        if (_currentActiveSubwindow != null)
        {
            BaseSubwindow subwindow = _currentActiveSubwindow.GetComponent<BaseSubwindow>();
            if (subwindow != null)
            {
                subwindow.StopWindowAudio();
            }
            _currentActiveSubwindow.SetActive(false);
            _currentActiveSubwindow = null;
        }

        StopMainMenuAudio();

        // SAFETY: Force the gate open (Idle) when closing the menu.
        AudioManager.Instance?.SetAudioState(UIAudioState.Idle);
        AudioManager.Instance?.ClearPendingSelectionAudio();

        mainMenuPanel.SetActive(false);

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            if (TutorialManager.Instance.pauseStateManager != null)
            {
                TutorialManager.Instance.pauseStateManager.SetToPrimaryState(); // Playing
                Debug.Log("[MenuNavigationManager] Tutorial audio resumed (menu closed)");
            }
        }

        AudioManager.Instance?.SetReturningToMainMenu(false);

        StartCoroutine(PlayCloseAudioAndRestoreTime());
    }

    private IEnumerator PlayCloseAudioAndRestoreTime()
    {
        if (menuCloseEvent != null && menuCloseEvent.IsValid())
        {
            uint flags = (uint)AkCallbackType.AK_EndOfEvent;
            menuCloseEvent.Post(
                gameObject,
                flags,
                OnMenuCloseAudioFinished,
                null
            );

            yield return new WaitForSecondsRealtime(0.5f);
        }

        FinalizeMenuClose();
    }

    private void OnMenuCloseAudioFinished(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_EndOfEvent)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(FinalizeAfterFrame());
            }
        }
    }

    private IEnumerator FinalizeAfterFrame()
    {
        yield return null;
        FinalizeMenuClose();
    }

    private void FinalizeMenuClose()
    {
        Time.timeScale = 1f;
        _isMenuOpen = false;
        _isClosingMenu = false;

        OnMenuFullyClosed?.Invoke();

        Debug.Log("[MenuNavigationManager] Menu fully closed, Time.timeScale = 1");
    }
}