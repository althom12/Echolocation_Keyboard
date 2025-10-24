using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Required for Selectable

// This script lives on a sub-panel (e.g., AudioSettingsPanel)
// It is disabled by default and enabled by MenuNavigationManager.
public class SubWindowInputHandler : MonoBehaviour
{
    // Assign your MenuNavigationManager in the Inspector
    public MenuNavigationManager menuManager;

    private CustomInputActions _input;
    private GameObject _currentActivePanel;

    // This must be called by the manager to initialize
    public void Initialize(CustomInputActions inputActions, GameObject panel)
    {
        _input = inputActions;
        _currentActivePanel = panel;
    }

    private void OnEnable()
    {
        if (_input == null) return;

        // When this script is enabled, it takes over Tab/Shift+Tab
        _input.UI.OpenSubMenu.performed += OnTabPressed; // 'Tab'
        _input.UI.NavigateBack.performed += OnNavigateBackPressed; // 'Shift+Tab'
        _input.UI.Cancel.performed += OnCancelPressed; // 'Escape'
    }

    private void OnDisable()
    {
        if (_input == null) return;

        // Unsubscribe when disabled
        _input.UI.OpenSubMenu.performed -= OnTabPressed;
        _input.UI.NavigateBack.performed -= OnNavigateBackPressed;
        _input.UI.Cancel.performed -= OnCancelPressed;
    }

    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        // 'Escape' pressed, close this window
        menuManager.CloseActiveSubWindow();
    }

    private void OnTabPressed(InputAction.CallbackContext context)
    {
        // Manually re-implement "Tab" to navigate forward
        Selectable current = GetCurrentSelectable();
        if (current == null) return;

        Selectable next = current.FindSelectableOnDown();

        if (next != null)
        {
            next.Select();
        }
        else
        {
            // If at the end, wrap to the first
            Selectable first = _currentActivePanel.GetComponentInChildren<Selectable>();
            if (first != null) first.Select();
        }
    }

    private void OnNavigateBackPressed(InputAction.CallbackContext context)
    {
        // Manually re-implement "Shift+Tab" to navigate backward
        Selectable current = GetCurrentSelectable();
        if (current == null) return;

        Selectable previous = current.FindSelectableOnUp();

        if (previous != null)
        {
            previous.Select();
        }
        else
        {
            // If 'previous' is null, we are on the first item.
            // As per your request, close the window.
            menuManager.CloseActiveSubWindow();
        }
    }

    private Selectable GetCurrentSelectable()
    {
        GameObject currentGO = EventSystem.current.currentSelectedGameObject; 
        if (currentGO == null) return null;

        return currentGO.GetComponent<Selectable>();
    }
}