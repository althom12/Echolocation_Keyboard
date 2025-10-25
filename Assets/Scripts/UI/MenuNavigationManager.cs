using UnityEngine;
using UnityEngine.EventSystems; // Required for EventSystem
using UnityEngine.InputSystem;
using UnityEngine.UI; // Required for Selectable
using System.Collections;

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
            }
        }
    }

    private void OnEnable()
    {
        // The "UI" map must always be enabled for this system
        // to detect the initial button presses.
        _input.UI.Enable();

        // --- NEW ---
        // Enable Player map to listen for the "Open" command
       _input.Player.Disable(); // Disable player map

        // Subscribe to your new "ToggleSettingsMenu" action from BOTH maps.
        // Make sure the name matches what you created in the Input Action Asset.
        _input.Player.ToggleSettingsMenu.performed += ToggleSettingsPanel;
        _input.UI.ToggleSettingsMenu.performed += ToggleSettingsPanel;
        // --- END NEW ---
    }

    private void OnDisable()
    {
        _input.UI.Disable();

        // --- MODIFIED ---
        _input.Player.Disable(); // Disable player map

        // Unsubscribe to prevent errors
        if (_input != null)
        {
            _input.Player.ToggleSettingsMenu.performed -= ToggleSettingsPanel;
            _input.UI.ToggleSettingsMenu.performed -= ToggleSettingsPanel;
        }
        // --- END MODIFIED ---

        // --- CLEANUP ---
        // Just in case the menu is disabled while a sub-window is open,
        // ensure player input is re-enabled and time is resumed.
        if (_activeSubWindow != null)
        {
            Time.timeScale = 1f;
            // _input.Player.Enable(); // This is handled by the logic above
        }
    }

    // --- NEW FUNCTION ---
    /// <summary>
    /// This function is called by the 'N' key from either Player or UI map.
    /// It opens the main menu if all UI is closed, or closes all UI if any is open.
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
                // Disable its specific handler
                SubWindowInputHandler handler = _activeSubWindow.GetComponent<SubWindowInputHandler>();
                if (handler != null)
                {
                    handler.enabled = false;
                }
                _activeSubWindow.SetActive(false);
                _activeSubWindow = null;
            }

            // 3. Disable all UI navigation logic
            customTabSubmitHandler.enabled = false;

            // 4. Resume game & enable player input
            Time.timeScale = 1f;
            _input.Player.Enable();

            // 5. Clear selection from EventSystem
            EventSystem.current.SetSelectedGameObject(null);
        }
        else
        {
            // --- OPEN THE MAIN MENU ---

            // 1. Show main panel
            mainSettingsPanel.SetActive(true);

            // 2. Set focus
            Selectable firstElement = mainSettingsPanel.GetComponentInChildren<Selectable>();
            if (firstElement != null)
            {
                EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
            }

            // 3. Enable main panel navigation
            customTabSubmitHandler.enabled = true;

            // 4. Pause game & disable player movement
            Time.timeScale = 0f;
            _input.Player.Disable();
        }
    }

    /// <summary>
    /// This is called by the buttons on your MainSettings panel.
    /// </summary>
    public void OpenSubWindow(GameObject subWindowToShow)
    {
        // 1. Store the button we just pressed
        _lastSelectedMainSettingsButton = EventSystem.current.currentSelectedGameObject;

        // 2. Hide main panel, show sub-panel
        mainSettingsPanel.SetActive(false);
        subWindowToShow.SetActive(true);
        _activeSubWindow = subWindowToShow;

        // --- NEW ---
        // 3. PAUSE GAME & SWAP INPUT MAPS
        Time.timeScale = 0f; // Pause all game physics and Update logic

        // *** IMPORTANT ***
        // Replace "Player" if your movement Action Map is named differently
        _input.Player.Disable(); // Disable the "Player" map
                                 // The "UI" map is already enabled from this script's OnEnable()

        // --- END NEW ---

        // 4. SWAP THE UI-INTERNAL INPUT LOGIC:
        // Disable "Tab as Submit" logic
        customTabSubmitHandler.enabled = false;

        // Enable "Tab as Navigate" logic for the sub-window
        SubWindowInputHandler handler = subWindowToShow.GetComponent<SubWindowInputHandler>();
        if (handler != null)
        {
            handler.enabled = true;
        }

        // 5. Set focus to the first item in the new window
        Selectable firstElement = subWindowToShow.GetComponentInChildren<Selectable>();
        if (firstElement != null)
        {
            EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
        }
    }

    /// <summary>
    /// This is now called by the SubWindowInputHandler (on 'Shift+Tab' or 'Esc').
    /// </summary>
    public void CloseActiveSubWindow()
    {
        if (_activeSubWindow == null) return;

        // 1. Get the handler on the sub-window
        SubWindowInputHandler handler = _activeSubWindow.GetComponent<SubWindowInputHandler>();

        // --- NEW ---
        // 2. RESUME GAME & SWAP INPUT MAPS
        Time.timeScale = 1f; // Resume the game

        // *** IMPORTANT ***
        // Replace "Player" if your movement Action Map is named differently
        _input.Player.Enable(); // Re-enable the "Player" map

        // --- END NEW ---

        // 3. SWAP THE UI-INTERNAL INPUT LOGIC:
        // Disable "Tab as Navigate" logic
        if (handler != null)
        {
            handler.enabled = false;
        }

        // Enable "Tab as Submit" logic
        customTabSubmitHandler.enabled = true;

        // 4. Hide sub-panel, show main panel
        _activeSubWindow.SetActive(false);
        mainSettingsPanel.SetActive(true);
        _activeSubWindow = null;

        // 5. Restore focus to the main menu button
        if (_lastSelectedMainSettingsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_lastSelectedMainSettingsButton);
        }
    }
}