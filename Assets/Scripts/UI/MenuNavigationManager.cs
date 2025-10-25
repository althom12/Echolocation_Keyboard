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
    public GameObject[] subWindowPanels;

    private CustomInputActions _input;
    private GameObject _lastSelectedMainSettingsButton;
    private GameObject _activeSubWindow;

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
                handler.enabled = false; // --- NEW: Ensure handlers start disabled ---
            }
        }
    }

    // --- YOUR WORKING OnEnable FUNCTION ---
    private void OnEnable()
    {
        // The "UI" map must always be enabled for this system
        // to detect the initial button presses.
        _input.UI.Enable();

        // Enable Player map to listen for the "Open" command when the menu is closed
        _input.Player.Enable(); // ENABLE player map

        // Subscribe to your new "ToggleSettingsMenu" action from BOTH maps.
        _input.Player.ToggleSettingsMenu.performed += ToggleSettingsPanel;
        _input.UI.ToggleSettingsMenu.performed += ToggleSettingsPanel;
    }
    // --- END YOUR WORKING OnEnable FUNCTION ---

    private void OnDisable()
    {
        // --- Need to disable both maps ---
        _input.UI.Disable();
        _input.Player.Disable();

        // Unsubscribe to prevent errors if _input exists
        if (_input != null)
        {
            _input.Player.ToggleSettingsMenu.performed -= ToggleSettingsPanel;
            _input.UI.ToggleSettingsMenu.performed -= ToggleSettingsPanel;
        }


        // --- CLEANUP ---
        if (_activeSubWindow != null)
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// This function is called by the 'N' key from either Player or UI map.
    /// </summary>
    private void ToggleSettingsPanel(InputAction.CallbackContext context)
    {
        // Check if ANY UI is open (main panel OR a sub-window)
        if (mainSettingsPanel.activeSelf || _activeSubWindow != null)
        {
            // --- CLOSE EVERYTHING ---

            // 1. Hide main panel
            mainSettingsPanel.SetActive(false);

            // 2. Hide active sub-window (if any)
            if (_activeSubWindow != null)
            {
                SubWindowInputHandler handler = _activeSubWindow.GetComponent<SubWindowInputHandler>();
                if (handler != null) handler.enabled = false;
                _activeSubWindow.SetActive(false);
                _activeSubWindow = null;
            }

            // 3. Disable UI navigation logic
            if (customTabSubmitHandler != null) customTabSubmitHandler.enabled = false;

            // 4. Resume game & enable player input
            Time.timeScale = 1f;
            _input.Player.Enable();
            EventSystem.current.SetSelectedGameObject(null);

            // --- AUDIO FIX: Use Singleton for closing sound ---
            if (UIAudioManager.Instance != null)
            {
                // Pass the manager's own GameObject as the emitter
                UIAudioManager.Instance.PlayMenuCloseToGameSound(gameObject);
            }
            // --- END AUDIO FIX ---
        }
        else
        {
            // --- OPEN THE MAIN MENU (Called only by N key from gameplay) ---
            mainSettingsPanel.SetActive(true);

            // --- AUDIO FIX: Use Singleton for opening sound ---
            if (UIAudioManager.Instance != null)
            {
                // Pass the manager's own GameObject as the emitter
                UIAudioManager.Instance.PlayMenuOpenFromGameSound(gameObject);
            }
            // --- END AUDIO FIX ---

            // 2. Set focus to the first selectable element
            Selectable firstElement = mainSettingsPanel.GetComponentInChildren<Selectable>();
            if (firstElement != null)
            {
                EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
            }
            // 3. Enable main panel navigation
            if (customTabSubmitHandler != null) customTabSubmitHandler.enabled = true;

            // 4. Pause game & disable player movement
            Time.timeScale = 0f;
            _input.Player.Disable();
        }
    }

    /// <summary>
    /// Called by buttons on MainSettings panel. Now delays enabling sub-window nav.
    /// </summary>
    public void OpenSubWindow(GameObject subWindowToShow)
    {
        // 1. Store button
        _lastSelectedMainSettingsButton = EventSystem.current.currentSelectedGameObject;

        // 2. Swap Panels
        mainSettingsPanel.SetActive(false);
        subWindowToShow.SetActive(true);
        _activeSubWindow = subWindowToShow;

        // Audio Call (using GetComponentInChildren for modularity)
        // --- AUDIO FIX: USE GetComponentInChildren ONCE ---
        // This is the robust way to find the script, even if it's on the root or a child.
        UIAudioTrigger_SubWindow windowAudio = subWindowToShow.GetComponentInChildren<UIAudioTrigger_SubWindow>();
        if (windowAudio != null)
        {
            windowAudio.PlayOpenSound(); // Plays this window's unique sound
        }

        // 3. Pause & Disable Player Input (should already be disabled)
        Time.timeScale = 0f;
        _input.Player.Disable();

        // 4. Disable Main Panel Submit Logic
        if (customTabSubmitHandler != null)
        {
            customTabSubmitHandler.enabled = false;
        }

        // --- NEW CRITICAL STEP: Reset Flag on the First Selectable ---
        Selectable firstElement = subWindowToShow.GetComponentInChildren<Selectable>();
        if (firstElement != null)
        {
            // Find the audio trigger on the first selectable item
            UIAkSelectTrigger firstItemTrigger = firstElement.GetComponent<UIAkSelectTrigger>();
            if (firstItemTrigger != null)
            {
                firstItemTrigger.ResetSelectionState(); // Reset the local _hasBeenSelected flag
            }

            // Set focus immediately
            EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
        }
        // --- END NEW STEP ---

        // 5. DELAY enabling the SubWindowInputHandler
        SubWindowInputHandler handler = subWindowToShow.GetComponent<SubWindowInputHandler>();
        if (handler != null)
        {
            handler.enabled = false;
            StartCoroutine(EnableSubHandlerAfterFrame(handler)); // Now references the function below
        }
    }

    // --- COROUTINE FUNCTION RE-INSERTED HERE (Fixes CS0103) ---
    /// <summary>
    /// Waits until the end of the current frame before enabling the sub-window handler.
    /// This prevents the 'Tab' (Submit) press from causing a double-jump (input bleed).
    /// </summary>
    private IEnumerator EnableSubHandlerAfterFrame(SubWindowInputHandler handlerToEnable)
    {
        // Wait until all rendering and input processing for the current frame is done
        yield return new WaitForEndOfFrame();

        // Now enable the handler. The 'Tab' press from the previous context should be gone.
        if (handlerToEnable != null)
        {
            handlerToEnable.enabled = true;
        }
    }
    // --- END COROUTINE RE-INSERTION ---


    /// <summary>
    /// Called by SubWindowInputHandler to return to the main menu.
    /// </summary>
    public void CloseActiveSubWindow()
    {
        if (_activeSubWindow == null) return;

        // 1. Disable Sub Handler
        SubWindowInputHandler handler = _activeSubWindow.GetComponent<SubWindowInputHandler>();
        if (handler != null) handler.enabled = false;

        // 2. Don't resume time/player input yet

        // 3. Enable Main Handler
        if (customTabSubmitHandler != null) customTabSubmitHandler.enabled = true;

        // 4. Swap Panels
        _activeSubWindow.SetActive(false);
        mainSettingsPanel.SetActive(true);
        _activeSubWindow = null;

        // 5. Restore focus
        if (_lastSelectedMainSettingsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_lastSelectedMainSettingsButton);
        }
        // Time remains 0f, Player remains disabled.
    }
}