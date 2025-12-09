using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows;
using static UnityEngine.Rendering.DebugUI.Table;

/// <summary>
/// Generic headless controller for navigating and interacting with settings pages.
/// This is the "Controller" in MVC - it handles input and orchestrates state changes.
/// Uses Unity's new Input System with C# event subscription.
/// </summary>
public class GenericPageController : MonoBehaviour
{
    [Header("Page Configuration")]
    [Tooltip("List of all controllable items on this page (populate per-instance in Inspector)")]
    [SerializeField] private List<PageControlItem> pageItems = new List<PageControlItem>();

    [Header("Navigation Events")]
    [Tooltip("Fired when Shift+Tab is pressed at index 0 (request parent to take focus)")]
    public UnityEvent OnRequestReturnToCategories;

    [Header("State")]
    [SerializeField] private int currentIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Input System reference
    private CustomInputActions _input;

    private void Awake()
    {
        _input = new CustomInputActions();

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] GenericPageController initialized with {pageItems.Count} items");

        // CRITICAL TEST: Log EVERY phase of ModifyValue
        _input.UI.ModifyValue.started += ctx =>
            Debug.Log($"?? [TEST] ModifyValue STARTED | Control: {ctx.control.path} | Value: {ctx.ReadValue<float>()}");

        _input.UI.ModifyValue.performed += ctx =>
            Debug.Log($"?? [TEST] ModifyValue PERFORMED | Control: {ctx.control.path} | Value: {ctx.ReadValue<float>()}");

        _input.UI.ModifyValue.canceled += ctx =>
            Debug.Log($"?? [TEST] ModifyValue CANCELED | Control: {ctx.control.path}");
    }

    private void OnEnable()
    {
        Debug.Log($"[{gameObject.name}] ===== OnEnable() START =====");

        // Check if input is null
        if (_input == null)
        {
            Debug.LogError($"[{gameObject.name}] _input is NULL! Input System not initialized!");
            return;
        }

        Debug.Log($"[{gameObject.name}] _input exists, enabling...");
        _input.Enable();
        Debug.Log($"[{gameObject.name}] _input enabled");

        // Check if UI action map exists
        // UI is a struct, so it always exists - just log that we're accessing it
        Debug.Log($"[{gameObject.name}] Accessing _input.UI action map...");

        // Check each action individually
        Debug.Log($"[{gameObject.name}] Checking Navigate action...");
        if (_input.UI.Navigate != null)
        {
            _input.UI.Navigate.performed += OnNavigatePerformed;
            Debug.Log($"[{gameObject.name}] ? Navigate subscribed");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Navigate action is NULL!");
        }

        Debug.Log($"[{gameObject.name}] Checking TabNavigate action...");
        if (_input.UI.TabNavigate != null)
        {
            _input.UI.TabNavigate.performed += ctx =>
            {
                Debug.Log($"[{gameObject.name}] TabNavigate fired!");
                if (UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed)
                {
                    NavigateUp();
                }
                else
                {
                    NavigateDown();
                }
            };
            Debug.Log($"[{gameObject.name}] ? TabNavigate subscribed");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] TabNavigate action is NULL!");
        }

        Debug.Log($"[{gameObject.name}] Checking Submit action...");
        if (_input.UI.Submit != null)
        {
            _input.UI.Submit.performed += OnSubmitPerformed;
            Debug.Log($"[{gameObject.name}] ? Submit subscribed");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Submit action is NULL!");
        }

        Debug.Log($"[{gameObject.name}] Checking ModifyValue action...");
        if (_input.UI.ModifyValue != null)
        {
            _input.UI.ModifyValue.performed += OnModifyValuePerformed;
            _input.UI.ModifyValue.canceled += OnModifyValueCanceled;
            Debug.Log($"[{gameObject.name}] ? ModifyValue subscribed");

            // Extra debug: Check if the action is enabled
            Debug.Log($"[{gameObject.name}] ModifyValue enabled: {_input.UI.ModifyValue.enabled}");
            Debug.Log($"[{gameObject.name}] ModifyValue phase: {_input.UI.ModifyValue.phase}");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] ? ModifyValue action is NULL! Did you regenerate the C# class?");
        }

        if (pageItems.Count > 0) SetFocus(0);

        Debug.Log($"[{gameObject.name}] ===== OnEnable() COMPLETE =====");
    }

    /// <summary>
    /// Handles Left/Right arrow keys for value modification via Input System
    /// </summary>
    private void OnModifyValuePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        Debug.Log($"[{gameObject.name}] ? OnModifyValuePerformed FIRED!");
        Debug.Log($"[{gameObject.name}] Context phase: {context.phase}");
        Debug.Log($"[{gameObject.name}] Context control: {context.control}");

        if (pageItems.Count == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] No items in list!");
            return;
        }

        PageControlItem currentItem = pageItems[currentIndex];
        Debug.Log($"[{gameObject.name}] Current item: '{currentItem.itemName}' (Type: {currentItem.controlType})");

        // Only Sliders respond to arrow keys
        if (currentItem.controlType != PageControlType.Slider)
        {
            Debug.LogWarning($"[{gameObject.name}] Current item is not a Slider, ignoring");
            return;
        }

        // Read the axis value: +1 for right, -1 for left
        float direction = context.ReadValue<float>();
        Debug.Log($"[{gameObject.name}] Direction value read: {direction}");

        // Timer check for controlled held-down behavior
        if (Time.time > _moveTimer)
        {
            float oldValue = currentItem.currentValue;

            if (direction > 0.5f) // Right arrow
            {
                currentItem.IncrementValue();
                Debug.Log($"[{gameObject.name}] ?? RIGHT ARROW: '{currentItem.itemName}' changed from {oldValue:F2} to {currentItem.currentValue:F2}");
            }
            else if (direction < -0.5f) // Left arrow
            {
                currentItem.DecrementValue();
                Debug.Log($"[{gameObject.name}] ?? LEFT ARROW: '{currentItem.itemName}' changed from {oldValue:F2} to {currentItem.currentValue:F2}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Direction {direction} is in deadzone (not > 0.5 or < -0.5)");
            }

            _moveTimer = Time.time + 0.15f;
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Move blocked by timer (too soon)");
        }
    }

    
    private void OnModifyValueCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // Reset timer for responsive single taps
        _moveTimer = 0f;

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] ModifyValue released, timer reset");
    }



    /// <summary>
    /// Handles Tab/Shift+Tab navigation via Input System event
    /// </summary>
    private void OnNavigatePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (pageItems.Count == 0)
            return;

        // Read the navigation input (Vector2: y = 1 for down/tab, y = -1 for up/shift+tab)
        Vector2 navigationInput = context.ReadValue<Vector2>();

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Navigate input received: {navigationInput}");

        if (navigationInput.y > 0.5f)
        {
            // Navigate DOWN (Tab)
            NavigateDown();
        }
        else if (navigationInput.y < -0.5f)
        {
            // Navigate UP (Shift + Tab)
            NavigateUp();
        }
    }

    /// <summary>
    /// Navigate to next item (Tab key)
    /// </summary>
    private void NavigateDown()
    {
        if (currentIndex < pageItems.Count - 1)
        {
            SetFocus(currentIndex + 1);

            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] Navigated DOWN to index {currentIndex}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] Already at bottom (index {currentIndex}), staying here");
        }
    }

    /// <summary>
    /// Navigate to previous item (Shift + Tab)
    /// </summary>
    private void NavigateUp()
    {
        if (currentIndex > 0)
        {
            SetFocus(currentIndex - 1);

            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] Navigated UP to index {currentIndex}");
        }
        else
        {
            // At index 0 - request return to categories
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] At index 0, requesting return to categories");

            OnRequestReturnToCategories?.Invoke();
        }
    }

    /// <summary>
    /// Handles Left/Right arrow keys for value modification (polled for held-down support)
    /// </summary>
    // Add this variable at the top of your class with the other variables
    // Ensure this is defined at the top of your class
    private float _moveTimer = 0f;

    

    /// <summary>
    /// Handles Enter/Submit key via Input System event
    /// </summary>
    private void OnSubmitPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (pageItems.Count == 0)
            return;

        PageControlItem currentItem = pageItems[currentIndex];

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] SUBMIT pressed on '{currentItem.itemName}' (Type: {currentItem.controlType})");

        // Handle based on control type
        switch (currentItem.controlType)
        {
            case PageControlType.Button:
                if (enableDebugLogs)
                    Debug.Log($"[{gameObject.name}] Button '{currentItem.itemName}' activated");

                currentItem.OnSubmit?.Invoke();
                break;

            case PageControlType.Toggle:
                float oldValue = currentItem.currentValue;
                currentItem.ToggleValue();

                if (enableDebugLogs)
                    Debug.Log($"[{gameObject.name}] Toggle '{currentItem.itemName}' flipped from {oldValue:F0} to {currentItem.currentValue:F0}");

                currentItem.OnSubmit?.Invoke();
                break;

            case PageControlType.Slider:
                if (enableDebugLogs)
                    Debug.Log($"[{gameObject.name}] Slider '{currentItem.itemName}' submit ignored (sliders don't respond to Enter)");
                break;

            case PageControlType.Dropdown:
                if (enableDebugLogs)
                    Debug.Log($"[{gameObject.name}] Dropdown '{currentItem.itemName}' opened (future implementation)");

                currentItem.OnSubmit?.Invoke();
                break;
        }
    }

    /// <summary>
    /// Sets focus to a specific index, updating visuals and firing events
    /// </summary>
    private void SetFocus(int index)
    {


        // Validate index
        if (index < 0 || index >= pageItems.Count)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[{gameObject.name}] Attempted to focus invalid index {index} (valid range: 0-{pageItems.Count - 1})");
            return;
        }

        // Clear previous highlight
        ClearAllHighlights();

        // Update state
        currentIndex = index;
        // Force visual update on focus
        

        // Show new highlight
        PageControlItem currentItem = pageItems[currentIndex];
        if (currentItem.highlightVisual != null)
        {
            currentItem.highlightVisual.SetActive(true);

            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] Focus moved to index {currentIndex}: '{currentItem.itemName}' (Type: {currentItem.controlType}, Value: {currentItem.currentValue:F2})");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"[{gameObject.name}] Focus moved to index {currentIndex}: '{currentItem.itemName}', but no highlight visual assigned!");
        }

        currentItem.OnValueChanged?.Invoke(currentItem.currentValue);

        // Fire focus event for accessibility/audio
        currentItem.OnFocus?.Invoke();
    }

    /// <summary>
    /// Clears all highlight visuals
    /// </summary>
    private void ClearAllHighlights()
    {
        foreach (var item in pageItems)
        {
            if (item.highlightVisual != null)
            {
                item.highlightVisual.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Public API: Allow external scripts to programmatically change focus
    /// </summary>
    public void FocusItem(int index)
    {
        SetFocus(index);
    }

    /// <summary>
    /// Public API: Get current focused item (useful for accessibility announcements)
    /// </summary>
    public PageControlItem GetCurrentItem()
    {
        if (currentIndex >= 0 && currentIndex < pageItems.Count)
            return pageItems[currentIndex];
        return null;
    }

    /// <summary>
    /// Public API: Get current focused index
    /// </summary>
    public int GetCurrentIndex()
    {
        return currentIndex;
    }
}