using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Custom UI Submit Handler for Main Menu
/// 
/// Handles Tab key as Submit for opening subwindows from MainSettings.
/// This script automatically enables/disables based on the watched panel's state.
/// 
/// IMPORTANT:
/// - Attach this to the MainSettings panel GameObject itself (not a parent)
/// - The script will auto-enable when the panel activates
/// - The script will auto-disable when the panel deactivates
/// - This prevents conflicts with SubWindowInputHandler
/// </summary>
public class CustomUISubmitHandler : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Optional: Panel to watch. If not set, uses this GameObject.")]
    public GameObject panelToWatch;

    private CustomInputActions _input;
    private GameObject _actualPanelToWatch;

    // ???????????????????????????????????????????????????????????
    // UNITY LIFECYCLE
    // ???????????????????????????????????????????????????????????

    private void Awake()
    {
        _input = new CustomInputActions();

        // If no panel specified, watch this GameObject
        _actualPanelToWatch = panelToWatch != null ? panelToWatch : gameObject;
    }

    /// <summary>
    /// OnEnable is called when the GameObject (or panel) becomes active.
    /// This is the key to auto-enabling only when MainSettings is visible.
    /// </summary>
    private void OnEnable()
    {
        // Double-check the panel is actually active
        if (_actualPanelToWatch != null && !_actualPanelToWatch.activeInHierarchy)
        {
            return;
        }

        // Subscribe to Tab as Submit
        _input.UI.OpenSubMenu.performed += OnTabPressed;
        _input.UI.Enable();

        Debug.Log("[CustomUISubmitHandler] Enabled - listening for Tab");
    }

    /// <summary>
    /// OnDisable is called when the GameObject (or panel) becomes inactive.
    /// This automatically unsubscribes when MainSettings closes or subwindow opens.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from Tab
        _input.UI.OpenSubMenu.performed -= OnTabPressed;
        _input.UI.Disable();

        Debug.Log("[CustomUISubmitHandler] Disabled - no longer listening for Tab");
    }

    // ???????????????????????????????????????????????????????????
    // INPUT HANDLING
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Called when Tab is pressed.
    /// Triggers Submit on the currently selected MainSettings button.
    /// </summary>
    private void OnTabPressed(InputAction.CallbackContext context)
    {
        // Safety check: Ensure panel is still active
        if (_actualPanelToWatch != null && !_actualPanelToWatch.activeInHierarchy)
        {
            Debug.LogWarning("[CustomUISubmitHandler] Tab pressed but panel is inactive - shouldn't happen!");
            return;
        }

        TriggerSubmitOnSelectedObject();
    }

    /// <summary>
    /// Executes Submit on the currently selected UI element.
    /// </summary>
    private void TriggerSubmitOnSelectedObject()
    {
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning("[CustomUISubmitHandler] No object selected");
            return;
        }

        // Create event data
        BaseEventData eventData = new BaseEventData(EventSystem.current);

        // Execute Submit event
        ExecuteEvents.Execute(
            selectedObject,
            eventData,
            ExecuteEvents.submitHandler
        );

        Debug.Log($"[CustomUISubmitHandler] Triggered Submit on '{selectedObject.name}'");
    }

    // ???????????????????????????????????????????????????????????
    // PUBLIC API (Optional)
    // ???????????????????????????????????????????????????????????

    public void EnableOpenSubMenu()
    {
        if (_input != null)
        {
            _input.UI.OpenSubMenu.Enable();
        }
    }

    public void DisableOpenSubMenu()
    {
        if (_input != null)
        {
            _input.UI.OpenSubMenu.Disable();
        }
    }
}