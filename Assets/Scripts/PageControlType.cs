using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

/// <summary>
/// Enum defining the behavior type of each page control item
/// </summary>
public enum PageControlType
{
    Button,
    Toggle,
    Slider,
    Dropdown  // For future implementation
}

/// <summary>
/// Serializable class representing a single controllable item on a settings page.
/// This is the "Model" in MVC - it owns the data state.
/// </summary>
[System.Serializable]
public class PageControlItem
{
    [Header("Identity")]
    [Tooltip("Display name for accessibility (NVDA will read this)")]
    public string itemName;

    [Tooltip("Determines input behavior and valid interactions")]
    public PageControlType controlType;

    [Header("Visual Feedback")]
    [Tooltip("GameObject to activate when this item has focus")]
    public GameObject highlightVisual;

    [Header("State Data (Model owns the truth)")]
    [Tooltip("Current value for Sliders (0-1 normalized) or Toggles (0=off, 1=on)")]
    public float currentValue;

    [Tooltip("Step size for Slider increments when using arrow keys")]
    public float stepSize = 0.1f;

    [Tooltip("Min/Max clamping for Slider values")]
    public float minValue = 0f;
    public float maxValue = 1f;

    [Header("Events (Listeners wire up in Inspector)")]
    [Tooltip("Fired when this item receives focus (for audio cues, NVDA announcements)")]
    public UnityEvent OnFocus;

    [Tooltip("Fired when Enter/Submit is pressed (for Buttons and Toggles)")]
    public UnityEvent OnSubmit;

    [Tooltip("Fired when value changes (passes new absolute value as float)")]
    public UnityEvent<float> OnValueChanged;

    /// <summary>
    /// Increments the value based on control type and step size
    /// </summary>
    public void IncrementValue()
    {
        if (controlType == PageControlType.Slider)
        {
            currentValue = Mathf.Clamp(currentValue + stepSize, minValue, maxValue);
            OnValueChanged?.Invoke(currentValue);
        }
        else if (controlType == PageControlType.Toggle)
        {
            currentValue = 1f; // Turn on
            OnValueChanged?.Invoke(currentValue);
        }
    }

    /// <summary>
    /// Decrements the value based on control type and step size
    /// </summary>
    public void DecrementValue()
    {
        if (controlType == PageControlType.Slider)
        {
            currentValue = Mathf.Clamp(currentValue - stepSize, minValue, maxValue);
            OnValueChanged?.Invoke(currentValue);
        }
        else if (controlType == PageControlType.Toggle)
        {
            currentValue = 0f; // Turn off
            OnValueChanged?.Invoke(currentValue);
        }
    }

    /// <summary>
    /// Toggles the current value (for Toggles and optional Button behavior)
    /// </summary>
    public void ToggleValue()
    {
        if (controlType == PageControlType.Toggle)
        {
            currentValue = currentValue > 0.5f ? 0f : 1f;
            OnValueChanged?.Invoke(currentValue);
        }
    }
}