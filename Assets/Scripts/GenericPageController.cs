using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows;
using static UnityEngine.Rendering.DebugUI.Table;

/// <summary>
/// Generic headless controller for navigating and interacting with settings pages.
/// This is the "Controller" in MVC - it handles input and orchestrates state changes.
/// Uses Unity's new Input System with C# event subscription.
/// 
/// NEW: Supports hierarchical navigation via parent/child controller linking.
/// Controllers can drill down to child pages and bubble up to parent pages.
/// </summary>
public class GenericPageController : MonoBehaviour
{
    [Header("Page Configuration")]
    [Tooltip("List of all controllable items on this page (populate per-instance in Inspector)")]
    [SerializeField] private List<PageControlItem> pageItems = new List<PageControlItem>();

    [Header("Hierarchical Navigation (NEW)")]
    [Tooltip("If this is a child page, reference to the parent controller. Leave null for root controllers.")]
    [SerializeField] private GenericPageController parentPage;

    [Tooltip("Is this controller currently accepting input? Root starts true, children start false.")]
    [SerializeField] private bool isInputActive = false;

    [Header("Navigation Events")]
    [Tooltip("Fired when Shift+Tab is pressed at index 0 with no parent (root-level escape request)")]
    public UnityEvent OnRequestReturnToCategories;

    [Header("State")]
    [SerializeField] private int currentIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Input System reference
    private CustomInputActions _input;

    // Timer for held-down arrow key behavior
    private float _moveTimer = 0f;

    // Frame skip mechanism for bubble-up input consumption
    private int _skipInputFrame = -1;

    private void Awake()
    {
        _input = new CustomInputActions();

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] GenericPageController initialized with {pageItems.Count} items (isInputActive: {isInputActive})");
    }

    private void OnEnable()
    {
        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] ===== OnEnable() START =====");

        // Check if input is null
        if (_input == null)
        {
            Debug.LogError($"[{gameObject.name}] _input is NULL! Input System not initialized!");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] _input exists, enabling...");

        _input.Enable();

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] _input enabled, subscribing to actions...");

        // Subscribe to all input actions (but guard logic with isInputActive checks)
        if (_input.UI.Navigate != null)
        {
            _input.UI.Navigate.performed += OnNavigatePerformed;
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] ? Navigate subscribed");
        }

        // NEW: Subscribe to Tab/Shift+Tab for navigation (in addition to arrows)
        if (_input.UI.TabNavigate != null)
        {
            _input.UI.TabNavigate.performed += OnTabNavigatePerformed;
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] ? TabNavigate subscribed");
        }

        if (_input.UI.Submit != null)
        {
            _input.UI.Submit.performed += OnSubmitPerformed;
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] ? Submit subscribed");
        }

        if (_input.UI.ModifyValue != null)
        {
            _input.UI.ModifyValue.performed += OnModifyValuePerformed;
            _input.UI.ModifyValue.canceled += OnModifyValueCanceled;
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] ? ModifyValue subscribed");
        }

        // Set initial focus if this controller is active and has items
        if (isInputActive && pageItems.Count > 0)
        {
            SetFocus(currentIndex);
        }

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] ===== OnEnable() COMPLETE =====");
    }

    private void OnDisable()
    {
        if (_input == null) return;

        // Unsubscribe from all input actions
        if (_input.UI.Navigate != null)
            _input.UI.Navigate.performed -= OnNavigatePerformed;

        if (_input.UI.TabNavigate != null)
            _input.UI.TabNavigate.performed -= OnTabNavigatePerformed;

        if (_input.UI.Submit != null)
            _input.UI.Submit.performed -= OnSubmitPerformed;

        if (_input.UI.ModifyValue != null)
        {
            _input.UI.ModifyValue.performed -= OnModifyValuePerformed;
            _input.UI.ModifyValue.canceled -= OnModifyValueCanceled;
        }

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] OnDisable: Input actions unsubscribed");
    }

    // ???????????????????????????????????????????????????????????????????
    // INPUT EVENT HANDLERS (with isInputActive guards)
    // ???????????????????????????????????????????????????????????????????

    /// <summary>
    /// Handles Up/Down arrow keys for navigation via Input System
    /// </summary>
    private void OnNavigatePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // GUARD: Only process if this controller is active
        if (!isInputActive)
        {
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] Navigate input ignored (isInputActive = false)");
            return;
        }

        // GUARD: Skip input if we just became active this frame (bubble-up)
        if (Time.frameCount == _skipInputFrame)
        {
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] Navigate input skipped (just bubbled up this frame)");
            return;
        }

        if (pageItems.Count == 0)
            return;

        // Read the navigation input (Vector2: y = 1 for down, y = -1 for up)
        Vector2 navigationInput = context.ReadValue<Vector2>();

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Navigate input received: {navigationInput}");

        if (navigationInput.y > 0.5f)
        {
            // Navigate DOWN (Down Arrow)
            NavigateDown();
        }
        else if (navigationInput.y < -0.5f)
        {
            // Navigate UP (Up Arrow)
            NavigateUp();
        }
    }

    /// <summary>
    /// Handles Tab/Shift+Tab for navigation via Input System
    /// NEW: Separate handler so Tab can work alongside arrows
    /// </summary>
    private void OnTabNavigatePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // GUARD: Only process if this controller is active
        if (!isInputActive)
        {
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] TabNavigate input ignored (isInputActive = false)");
            return;
        }

        // GUARD: Skip input if we just became active this frame (bubble-up)
        if (Time.frameCount == _skipInputFrame)
        {
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] TabNavigate input skipped (just bubbled up this frame)");
            return;
        }

        if (pageItems.Count == 0)
            return;

        // Check if Shift is held for reverse navigation
        bool shiftHeld = UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed;

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] TabNavigate input received (Shift: {shiftHeld})");

        if (shiftHeld)
        {
            // Navigate UP (Shift+Tab)
            NavigateUp();
        }
        else
        {
            // Navigate DOWN (Tab)
            NavigateDown();
        }
    }

    /// <summary>
    /// Handles Left/Right arrow keys for value modification via Input System
    /// </summary>
    private void OnModifyValuePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // GUARD: Only process if this controller is active
        if (!isInputActive)
        {
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] ModifyValue input ignored (isInputActive = false)");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] ? OnModifyValuePerformed FIRED!");

        if (pageItems.Count == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] No items in list!");
            return;
        }

        PageControlItem currentItem = pageItems[currentIndex];

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Current item: '{currentItem.itemName}' (Type: {currentItem.controlType})");

        // Only Sliders respond to arrow keys
        if (currentItem.controlType != PageControlType.Slider)
        {
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] Current item is not a Slider, ignoring");
            return;
        }

        // Read the axis value: +1 for right, -1 for left
        float direction = context.ReadValue<float>();

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Direction value read: {direction}");

        // Timer check for controlled held-down behavior
        if (Time.time > _moveTimer)
        {
            float oldValue = currentItem.currentValue;

            if (direction > 0.5f) // Right arrow
            {
                currentItem.IncrementValue();
                if (enableDebugLogs)
                    Debug.Log($"[{gameObject.name}] ?? RIGHT ARROW: '{currentItem.itemName}' changed from {oldValue:F2} to {currentItem.currentValue:F2}");
            }
            else if (direction < -0.5f) // Left arrow
            {
                currentItem.DecrementValue();
                if (enableDebugLogs)
                    Debug.Log($"[{gameObject.name}] ?? LEFT ARROW: '{currentItem.itemName}' changed from {oldValue:F2} to {currentItem.currentValue:F2}");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[{gameObject.name}] Direction {direction} is in deadzone (not > 0.5 or < -0.5)");
            }

            _moveTimer = Time.time + 0.15f;
        }
        else
        {
            if (enableDebugLogs)
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
    /// Handles Enter/Submit key via Input System event
    /// </summary>
    private void OnSubmitPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // GUARD: Only process if this controller is active
        if (!isInputActive)
        {
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] Submit input ignored (isInputActive = false)");
            return;
        }

        if (pageItems.Count == 0)
            return;

        PageControlItem currentItem = pageItems[currentIndex];

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] SUBMIT pressed on '{currentItem.itemName}' (Type: {currentItem.controlType})");

        // NEW: Check if this item has a child page (hierarchical navigation)
        if (currentItem.HasChildPage)
        {
            DrillDownToChild(currentItem.childPage);
            return; // Don't process normal submit logic
        }

        // Handle based on control type (original logic)
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

    // ???????????????????????????????????????????????????????????????????
    // NAVIGATION LOGIC
    // ???????????????????????????????????????????????????????????????????

    /// <summary>
    /// Navigate to next item (Down Arrow)
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
    /// Navigate to previous item (Up Arrow)
    /// NEW: If at index 0, bubble up to parent instead of wrapping
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
            // At index 0 - attempt to bubble up to parent
            BubbleUpToParent();
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

        // Clear previous highlight (only on this controller)
        ClearAllHighlights();

        // Update state
        currentIndex = index;

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

        // Fire value changed event to update UI
        currentItem.OnValueChanged?.Invoke(currentItem.currentValue);

        // Fire focus event for accessibility/audio
        currentItem.OnFocus?.Invoke();
    }

    /// <summary>
    /// Clears all highlight visuals on this controller
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

    // ???????????????????????????????????????????????????????????????????
    // HIERARCHICAL NAVIGATION (NEW)
    // ???????????????????????????????????????????????????????????????????

    /// <summary>
    /// Drills down to a child page, transferring input focus.
    /// Parent's highlight remains active (user knows which category they're in).
    /// </summary>
    /// <param name="child">The child controller to activate</param>
    private void DrillDownToChild(GenericPageController child)
    {
        if (child == null)
        {
            Debug.LogError($"[{gameObject.name}] DrillDownToChild called with null child!");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] ?? DRILL DOWN to child: {child.gameObject.name}");

        // Transfer input focus
        this.isInputActive = false;
        child.isInputActive = true;

        // Link parent reference (supports arbitrary depth)
        child.parentPage = this;

        // Activate child's first item
        child.SetFocus(0);

        // NOTE: We do NOT call ClearAllHighlights() on parent
        // This preserves the parent's highlight (shows active category)

        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Input deactivated");
            Debug.Log($"[{child.gameObject.name}] Input activated, parent linked to '{gameObject.name}'");
        }
    }

    /// <summary>
    /// Bubbles up to the parent page, transferring input focus back.
    /// If no parent exists (root controller), fires OnRequestReturnToCategories event.
    /// </summary>
    private void BubbleUpToParent()
    {
        if (parentPage != null)
        {
            // We have a parent - bubble up
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] ?? BUBBLE UP to parent: {parentPage.gameObject.name}");

            // Clear this controller's highlights (we're leaving)
            ClearAllHighlights();

            // Store the parent's current index before any state changes
            int parentTargetIndex = parentPage.currentIndex;

            // CRITICAL: Mark parent to skip input this frame
            // This prevents parent from processing the same Shift+Tab that caused bubble-up
            parentPage._skipInputFrame = Time.frameCount;

            // Transfer input focus back to parent
            this.isInputActive = false;
            parentPage.isInputActive = true;

            // CRITICAL: Explicitly call SetFocus on parent to restore its state
            // This ensures highlight, events, and focus are all properly restored
            parentPage.SetFocus(parentTargetIndex);

            if (enableDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] Input deactivated");
                Debug.Log($"[{parentPage.gameObject.name}] Input reactivated at index {parentTargetIndex} (skipFrame: {Time.frameCount})");
            }
        }
        else
        {
            // No parent - we're at the root
            if (enableDebugLogs)
                Debug.Log($"[{gameObject.name}] ?? At ROOT (no parent), firing OnRequestReturnToCategories");

            // Fire event for external systems (e.g., close settings menu)
            OnRequestReturnToCategories?.Invoke();
        }
    }

    // ???????????????????????????????????????????????????????????????????
    // PUBLIC API
    // ???????????????????????????????????????????????????????????????????

    /// <summary>
    /// Allow external scripts to programmatically change focus
    /// </summary>
    public void FocusItem(int index)
    {
        SetFocus(index);
    }

    /// <summary>
    /// Get current focused item (useful for accessibility announcements)
    /// </summary>
    public PageControlItem GetCurrentItem()
    {
        if (currentIndex >= 0 && currentIndex < pageItems.Count)
            return pageItems[currentIndex];
        return null;
    }

    /// <summary>
    /// Get current focused index
    /// </summary>
    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    /// <summary>
    /// Set whether this controller is actively processing input.
    /// Use this to manually control which controller is active if needed.
    /// </summary>
    public void SetInputActive(bool active)
    {
        bool wasActive = isInputActive;
        isInputActive = active;

        if (enableDebugLogs && wasActive != active)
            Debug.Log($"[{gameObject.name}] Input state changed: {wasActive} ? {active}");

        // If activating, restore focus visual
        if (active && currentIndex >= 0 && currentIndex < pageItems.Count)
        {
            var currentItem = pageItems[currentIndex];
            if (currentItem.highlightVisual != null)
            {
                currentItem.highlightVisual.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Get whether this controller is actively processing input
    /// </summary>
    public bool IsInputActive()
    {
        return isInputActive;
    }

    /// <summary>
    /// Manually set the parent controller (useful for dynamic scene construction)
    /// </summary>
    public void SetParent(GenericPageController parent)
    {
        parentPage = parent;

        if (enableDebugLogs)
            Debug.Log($"[{gameObject.name}] Parent set to: {(parent != null ? parent.gameObject.name : "null")}");
    }

    /// <summary>
    /// Get the parent controller (null if this is root)
    /// </summary>
    public GenericPageController GetParent()
    {
        return parentPage;
    }
}