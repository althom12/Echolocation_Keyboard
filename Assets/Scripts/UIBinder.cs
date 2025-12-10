using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
// Explicit imports to avoid ambiguity with UIElements
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

/// <summary>
/// Bridge script that connects PageControlItem data (Model) to Unity UI components (View).
/// Attach this to individual UI GameObjects (Sliders, Toggles, Text labels).
/// Wire up the public methods to PageControlItem.OnValueChanged events in the Inspector.
/// This is the "View" in MVC - it only displays data, never owns it.
/// </summary>
public class UIBinder : MonoBehaviour
{
    [Header("Component References (Auto-detected if not assigned)")]
    [Tooltip("The Slider component to update (leave empty to auto-detect)")]
    [SerializeField] private Slider sliderComponent;

    [Tooltip("The Toggle component to update (leave empty to auto-detect)")]
    [SerializeField] private Toggle toggleComponent;

    [Tooltip("The TextMeshProUGUI component to update (leave empty to auto-detect)")]
    [SerializeField] private TextMeshProUGUI textComponent;

    [Header("Text Formatting")]
    [Tooltip("Format string for displaying values. Use {0} as placeholder. Examples: '{0}%', 'Volume: {0:F1}', '{0:F0} dB'")]
    [SerializeField] private string formatString = "{0:F0}%";

    [Tooltip("Multiplier applied before formatting (e.g., 100 to convert 0-1 range to 0-100%)")]
    [SerializeField] private float displayMultiplier = 100f;

    [Tooltip("Should the text show rounded integers (true) or decimals (false)?")]
    [SerializeField] private bool roundToInteger = true;

    [Header("Toggle Visual Refresh")]
    [Tooltip("Force rebuild the toggle graphic to ensure visual update")]
    [SerializeField] private bool forceToggleRefresh = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private void Awake()
    {
        // Auto-detect components if not manually assigned
        if (sliderComponent == null)
            sliderComponent = GetComponent<Slider>();

        if (toggleComponent == null)
            toggleComponent = GetComponent<Toggle>();

        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        if (enableDebugLogs)
        {
            string components = "";
            if (sliderComponent != null) components += "Slider ";
            if (toggleComponent != null) components += "Toggle ";
            if (textComponent != null) components += "Text ";
            Debug.Log($"[UIBinder - {gameObject.name}] Initialized with: {components}");
        }
    }

    /// <summary>
    /// Updates a Slider component with the given value.
    /// Wire this to PageControlItem.OnValueChanged in the Inspector.
    /// </summary>
    /// <param name="value">Normalized value (typically 0-1)</param>
    public void UpdateSlider(float value)
    {
        if (sliderComponent == null)
        {
            Debug.LogError($"[UIBinder - {gameObject.name}] UpdateSlider called but no Slider component found!");
            return;
        }

        // DEBUG: Check slider range
        Debug.Log($"[UIBinder - {gameObject.name}] Slider range: {sliderComponent.minValue} to {sliderComponent.maxValue} | Setting value: {value}");

        sliderComponent.value = value;

        if (enableDebugLogs)
            Debug.Log($"[UIBinder - {gameObject.name}] Slider updated to {value:F2}");
    }

    /// <summary>
    /// Updates a Toggle component by converting float to bool.
    /// Wire this to PageControlItem.OnValueChanged in the Inspector.
    /// </summary>
    /// <param name="value">Float value (>0.5 = true, ?0.5 = false)</param>
    public void UpdateToggle(float value)
    {
        if (toggleComponent == null)
        {
            Debug.LogError($"[UIBinder - {gameObject.name}] UpdateToggle called but no Toggle component found!");
            return;
        }

        bool isOn = value > 0.5f;

        // Update the toggle state without triggering its own onValueChanged event
        toggleComponent.SetIsOnWithoutNotify(isOn);

        // Force visual refresh if enabled (ensures checkmark graphic updates)
        if (forceToggleRefresh && toggleComponent.graphic != null)
        {
            toggleComponent.graphic.enabled = isOn;
        }

        if (enableDebugLogs)
            Debug.Log($"[UIBinder - {gameObject.name}] Toggle updated to {isOn} (from value {value:F2})");
    }

    /// <summary>
    /// Updates a TextMeshProUGUI component with formatted value.
    /// Wire this to PageControlItem.OnValueChanged in the Inspector.
    /// </summary>
    /// <param name="value">Raw value to display (will be multiplied and formatted)</param>
    public void UpdateText(float value)
    {
        if (textComponent == null)
        {
            Debug.LogError($"[UIBinder - {gameObject.name}] UpdateText called but no TextMeshProUGUI component found!");
            return;
        }

        // Apply multiplier (e.g., convert 0.5 to 50 for percentage display)
        float displayValue = value * displayMultiplier;

        // Round if requested
        if (roundToInteger)
        {
            displayValue = Mathf.Round(displayValue);
        }

        // Format the text using the format string
        string formattedText;
        try
        {
            formattedText = string.Format(formatString, displayValue);
        }
        catch (System.FormatException)
        {
            Debug.LogWarning($"[UIBinder - {gameObject.name}] Invalid format string: '{formatString}'. Using default.");
            formattedText = displayValue.ToString("F0");
        }

        textComponent.text = formattedText;

        if (enableDebugLogs)
            Debug.Log($"[UIBinder - {gameObject.name}] Text updated to '{formattedText}' (raw value: {value:F2})");
    }

    /// <summary>
    /// Convenience method: Updates both Slider and Text in one call.
    /// Useful for slider + label combinations (e.g., "Volume: 75%").
    /// </summary>
    /// <param name="value">Value to display</param>
    public void UpdateSliderAndText(float value)
    {
        UpdateSlider(value);
        UpdateText(value);
    }

    /// <summary>
    /// Convenience method: Updates both Toggle and Text in one call.
    /// Useful for toggle + status label combinations (e.g., "ON" / "OFF").
    /// </summary>
    /// <param name="value">Value to display</param>
    public void UpdateToggleAndText(float value)
    {
        UpdateToggle(value);
        UpdateText(value);
    }

    /// <summary>
    /// Advanced: Update text with custom ON/OFF strings for toggles.
    /// Wire this to OnValueChanged if you want text like "Enabled" / "Disabled".
    /// </summary>
    /// <param name="value">Float value (>0.5 = ON text, ?0.5 = OFF text)</param>
    public void UpdateToggleText(float value)
    {
        if (textComponent == null)
        {
            Debug.LogError($"[UIBinder - {gameObject.name}] UpdateToggleText called but no TextMeshProUGUI component found!");
            return;
        }

        bool isOn = value > 0.5f;

        // Parse format string for ON/OFF text (format: "ON|OFF")
        string displayText;
        if (formatString.Contains("|"))
        {
            string[] parts = formatString.Split('|');
            displayText = isOn ? parts[0] : parts[1];
        }
        else
        {
            displayText = isOn ? "ON" : "OFF";
        }

        textComponent.text = displayText;

        if (enableDebugLogs)
            Debug.Log($"[UIBinder - {gameObject.name}] Toggle text updated to '{displayText}' (value: {value:F2})");
    }
}
