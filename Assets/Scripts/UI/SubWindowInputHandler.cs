using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class SubWindowInputHandler : MonoBehaviour
{
    public MenuNavigationManager menuManager;

    private CustomInputActions _input;
    private GameObject _currentActivePanel;
    private Selectable _firstSelectable;

    public void Initialize(CustomInputActions inputActions, GameObject panel, BaseSubwindow subwindow)
    {
        Debug.Log($"[SubWindowInputHandler] Initialize called for panel: {panel.name}");

        _input = inputActions;
        _currentActivePanel = panel;

        if (subwindow != null && subwindow.firstSelectedElement != null)
        {
            _firstSelectable = subwindow.firstSelectedElement.GetComponent<Selectable>();
            Debug.Log($"[SubWindowInputHandler] Got first selectable from BaseSubwindow: {(_firstSelectable != null ? _firstSelectable.gameObject.name : "NULL")}");
        }
        else
        {
            Debug.LogWarning("[SubWindowInputHandler] Could not get first selectable from BaseSubwindow!");
        }

        // CRITICAL: Unsubscribe any existing subscriptions before re-subscribing
        UnsubscribeFromActions();

        // Subscribe after one frame to prevent Tab key double-processing
        StartCoroutine(SubscribeAfterFrame());
    }

    private IEnumerator SubscribeAfterFrame()
    {
        yield return null;
        SubscribeToActions();
    }

    private void OnDisable()
    {
        UnsubscribeFromActions();
    }

    private void OnDestroy()
    {
        UnsubscribeFromActions();
    }

    private void SubscribeToActions()
    {
        if (_input == null)
        {
            Debug.LogWarning("[SubWindowInputHandler] Cannot subscribe - _input is null!");
            return;
        }

        Debug.Log("[SubWindowInputHandler] Subscribing to input actions");

        _input.UI.OpenSubMenu.performed += OnTabPressed;
        _input.UI.NavigateBack.performed += OnNavigateBackPressed;
        _input.UI.Cancel.performed += OnCancelPressed;
    }

    private void UnsubscribeFromActions()
    {
        if (_input == null) return;

        Debug.Log("[SubWindowInputHandler] Unsubscribing from input actions");

        _input.UI.OpenSubMenu.performed -= OnTabPressed;
        _input.UI.NavigateBack.performed -= OnNavigateBackPressed;
        _input.UI.Cancel.performed -= OnCancelPressed;
    }

    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        Debug.Log(">>> OnCancelPressed called <<<");
        menuManager.CloseActiveSubWindow();
    }

    private void OnTabPressed(InputAction.CallbackContext context)
    {
        Debug.Log(">>> OnTabPressed (Forward Navigation) called <<<");

        Selectable current = GetCurrentSelectable();
        if (current == null) return;

        Selectable next = current.FindSelectableOnDown();

        if (next != null)
        {
            next.Select();
        }
    }

    private void OnNavigateBackPressed(InputAction.CallbackContext context)
    {
        Debug.Log(">>> OnNavigateBackPressed called <<<");

        Selectable current = GetCurrentSelectable();
        if (current == null)
        {
            Debug.Log(">>> Current selectable is null <<<");
            return;
        }

        Debug.Log($">>> Current selected object: {current.gameObject.name} <<<");

        if (_firstSelectable != null && (current == _firstSelectable || current.gameObject == _firstSelectable.gameObject))
        {
            Debug.Log(">>> On first item, closing window <<<");
            menuManager.CloseActiveSubWindow();
            return;
        }

        Selectable previous = current.FindSelectableOnUp();

        if (previous != null && previous.gameObject != null)
        {
            Debug.Log($">>> Found previous selectable: {previous.gameObject.name}. Selecting it. <<<");
            previous.Select();
        }
        else
        {
            Debug.Log(">>> Previous selectable is null or doesn't exist, closing window <<<");

            if (menuManager != null)
            {
                menuManager.CloseActiveSubWindow();
            }
            else
            {
                Debug.LogError(">>> MenuNavigationManager reference is not set! <<<");
            }
        }
    }

    private Selectable GetCurrentSelectable()
    {
        GameObject currentGO = EventSystem.current.currentSelectedGameObject;
        if (currentGO == null) return null;
        return currentGO.GetComponent<Selectable>();
    }
}