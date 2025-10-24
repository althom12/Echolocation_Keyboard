using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SubWindowInputHandler : MonoBehaviour
{
    public MenuNavigationManager menuManager;

    private CustomInputActions _input;
    private GameObject _currentActivePanel;
    private Selectable _firstSelectable; // Cache the first selectable

    public void Initialize(CustomInputActions inputActions, GameObject panel)
    {
        _input = inputActions;
        _currentActivePanel = panel;

        // Cache the first selectable when initializing
        _firstSelectable = _currentActivePanel.GetComponentInChildren<Selectable>();
    }

    private void OnEnable()
    {
        if (_input == null) return;

        _input.UI.OpenSubMenu.performed += OnTabPressed;
        _input.UI.NavigateBack.performed += OnNavigateBackPressed;
        _input.UI.Cancel.performed += OnCancelPressed;

        // Update first selectable when enabled (in case UI changed)
        _firstSelectable = _currentActivePanel.GetComponentInChildren<Selectable>();
    }

    private void OnDisable()
    {
        if (_input == null) return;

        _input.UI.OpenSubMenu.performed -= OnTabPressed;
        _input.UI.NavigateBack.performed -= OnNavigateBackPressed;
        _input.UI.Cancel.performed -= OnCancelPressed;
    }

    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        menuManager.CloseActiveSubWindow();
    }

    private void OnTabPressed(InputAction.CallbackContext context)
    {
        Debug.LogWarning("OnTabPressed (Forward Navigation) called unexpectedly during Shift+Tab?");

        Selectable current = GetCurrentSelectable();
        if (current == null) return;

        // Try to find the next selectable (down in vertical navigation)
        Selectable next = current.FindSelectableOnDown();

        if (next != null)
        {
            next.Select();
        }
        else
        {
            // Optional: wrap to first, or do nothing
            // Uncomment if you want wrapping:
            // if (_firstSelectable != null) _firstSelectable.Select();
        }
    }

    private void OnNavigateBackPressed(InputAction.CallbackContext context)
    {
        Debug.Log("OnNavigateBackPressed called."); // ADD THIS LINE

        Selectable current = GetCurrentSelectable();
        if (current == null) return;

        // Check if we're on the first selectable
        if (current == _firstSelectable || current.gameObject == _firstSelectable.gameObject)
        {
            // We're on the first item, close the window and return to main menu
            menuManager.CloseActiveSubWindow();
            return;
        }

        Debug.Log($"Current selected object: {current.gameObject.name}");

        // Try to find previous selectable
        Selectable previous = current.FindSelectableOnUp();

        if (previous != null)
        {
            previous.Select();
        }

        if (previous != null && previous.gameObject != null)
        {
            Debug.Log($"Found previous selectable: {previous.gameObject.name}. Selecting it."); // ADD THIS LINE
            previous.Select();
        }
        else
        {
            // If 'previous' is null OR the gameObject is null (destroyed?), we are likely on the first item.
            Debug.Log("Previous selectable is null or destroyed. Attempting to close window."); // ADD THIS LINE

            // As per your request, close the window.
            // Make sure menuManager is assigned in the inspector!
            if (menuManager != null)
            {
                menuManager.CloseActiveSubWindow();
            }
            else
            {
                Debug.LogError("MenuNavigationManager reference is not set on SubWindowInputHandler!"); // ADD THIS LINE
            }
        }
        // If previous is null but we're not on first item, something is wrong with navigation setup
    }

    private Selectable GetCurrentSelectable()
    {
        GameObject currentGO = EventSystem.current.currentSelectedGameObject;
        if (currentGO == null) return null;
        return currentGO.GetComponent<Selectable>();
    }
}