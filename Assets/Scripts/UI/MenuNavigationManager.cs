using UnityEngine;
using UnityEngine.EventSystems; // Required for EventSystem
using UnityEngine.UI; // Required for Selectable

public class MenuNavigationManager : MonoBehaviour
{
    // Assign in Inspector
    public GameObject mainSettingsPanel;
    public CustomUISubmitHandler customTabSubmitHandler;

    // Assign ALL sub-panels in the Inspector
    public GameObject [] subWindowPanels;

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
        _input.UI.Enable();
    }

    private void OnDisable()
    {
        _input.UI.Disable();
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

        // 3. SWAP THE INPUT LOGIC:
        // Disable "Tab as Submit" logic
        customTabSubmitHandler.enabled = false;

        // Enable "Tab as Navigate" logic for the sub-window
        SubWindowInputHandler handler = subWindowToShow.GetComponent<SubWindowInputHandler>();
        if (handler != null)
        {
            handler.enabled = true;
        }

        // 4. Set focus to the first item in the new window
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

        // 2. SWAP THE INPUT LOGIC:
        // Disable "Tab as Navigate" logic
        if (handler != null)
        {
            handler.enabled = false;
        }

        // Enable "Tab as Submit" logic
        customTabSubmitHandler.enabled = true;

        // 3. Hide sub-panel, show main panel
        _activeSubWindow.SetActive(false);
        mainSettingsPanel.SetActive(true);
        _activeSubWindow = null;

        // 4. Restore focus to the main menu button
        if (_lastSelectedMainSettingsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_lastSelectedMainSettingsButton);
        }
    }
}