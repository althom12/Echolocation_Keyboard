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
            // --- OPEN THE MAIN MENU ---
            mainSettingsPanel.SetActive(true);
            Selectable firstElement = mainSettingsPanel.GetComponentInChildren<Selectable>();
            if (firstElement != null)
            {
                EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
            }
            if (customTabSubmitHandler != null) customTabSubmitHandler.enabled = true;
            Time.timeScale = 0f;
            _input.Player.Disable();
        }
    }

    /// <summary>
    /// Called by buttons on MainSettings panel.
    /// *** REVERTED VERSION - DOES NOT CHECK FOR ObstaclesSubwindow ***
    /// </summary>
    public void OpenSubWindow(GameObject subWindowToShow)
    {
        Debug.Log($"OpenSubWindow STARTED (REVERTED VERSION) for {subWindowToShow.name} at Time: {Time.unscaledTime}");

        // 1. Store button
        _lastSelectedMainSettingsButton = EventSystem.current.currentSelectedGameObject;

        // 2. Swap Panels
        mainSettingsPanel.SetActive(false);
        subWindowToShow.SetActive(true); // Directly activate here
        _activeSubWindow = subWindowToShow;

        // 3. Pause & Disable Player Input (should already be disabled)
        Time.timeScale = 0f;
        _input.Player.Disable();

        // 4. Disable Main Panel Submit Logic
        if (customTabSubmitHandler != null)
        {
            Debug.Log($"---> Disabling customTabSubmitHandler component at Time: {Time.unscaledTime}");
            customTabSubmitHandler.enabled = false; // Disable component
        }

        // --- OLD LOGIC: Directly select first element ---
        Selectable firstElement = subWindowToShow.GetComponentInChildren<Selectable>();
        if (firstElement != null)
        {
            EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
            Debug.Log($"---> Directly selected: {firstElement.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"---> NO selectable found in {subWindowToShow.name}");
        }
        // --- END OLD LOGIC ---

        // 5. DELAY enabling the SubWindowInputHandler
        SubWindowInputHandler handler = subWindowToShow.GetComponent<SubWindowInputHandler>();
        if (handler != null)
        {
            Debug.Log($"---> Starting Coroutine EnableSubHandlerAfterFrame at Time: {Time.unscaledTime}");
            handler.enabled = false;
            StartCoroutine(EnableSubHandlerAfterFrame(handler));
        }
        Debug.Log($"OpenSubWindow (REVERTED VERSION) FINISHED at Time: {Time.unscaledTime}");
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

        // 2. Don't resume time/player input yet

        // 3. Enable Main Handler
        if (customTabSubmitHandler != null)
        {
            Debug.Log($"---> Re-enabling customTabSubmitHandler component at Time: {Time.unscaledTime}");
            customTabSubmitHandler.enabled = true; // Re-enable component
        }


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