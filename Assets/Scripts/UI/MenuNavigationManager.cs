using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

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

    public void OpenMainMenu()
    {
        if (_isMenuOpen) return;

        Debug.Log("[MenuNavigationManager] Opening main menu");

        Time.timeScale = 0f;
        _isMenuOpen = true;

        mainMenuPanel.SetActive(true);

        if (firstMainMenuButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstMainMenuButton);
        }

        if (menuOpenEvent != null && menuOpenEvent.IsValid())
        {
            menuOpenEvent.Post(gameObject);
        }

        // --- MODIFIED: Tutorial audio pause through new architecture ---
        // Check if tutorial is active before pausing
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            // Pause tutorial audio through the WwiseStateManager
            if (TutorialManager.Instance.pauseStateManager != null)
            {
                TutorialManager.Instance.pauseStateManager.SetToSecondaryState(); // Paused
                Debug.Log("[MenuNavigationManager] Tutorial audio paused (menu opened)");
            }
        }
        // --- END MODIFIED SECTION ---

        AudioManager.Instance?.SetReturningToMainMenu(false);
    }



    /// <summary>
    /// Opens a subwindow by direct GameObject reference.
    /// This is cleaner for Inspector-based button configuration.
    /// </summary>
    /// <summary>
    /// Opens a subwindow by direct GameObject reference.
    /// This is cleaner for Inspector-based button configuration.
    /// </summary>
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

        // CRITICAL FIX: Remember which button was selected
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

        // Stop window audio
        BaseSubwindow subwindow = _currentActiveSubwindow.GetComponent<BaseSubwindow>();
        if (subwindow != null)
        {
            subwindow.StopWindowAudio();
        }

        _currentActiveSubwindow.SetActive(false);
        _currentActiveSubwindow = null;

        mainMenuPanel.SetActive(true);

        AudioManager.Instance?.SetReturningToMainMenu(true);

        // CRITICAL FIX: Select the button we came from, or fall back to first button
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

        mainMenuPanel.SetActive(false);

        // --- MODIFIED: Tutorial audio resume through new architecture ---
        // Check if tutorial is active before resuming
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            // Resume tutorial audio through the WwiseStateManager
            if (TutorialManager.Instance.pauseStateManager != null)
            {
                TutorialManager.Instance.pauseStateManager.SetToPrimaryState(); // Playing
                Debug.Log("[MenuNavigationManager] Tutorial audio resumed (menu closed)");
            }
        }
        // --- END MODIFIED SECTION ---

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