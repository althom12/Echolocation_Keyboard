using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the UI Toggle for landmark audio (Clock or HVAC).
/// Place this script directly on the Toggle GameObject.
/// IMPORTANT: Make sure to set the correct LandmarkType in the Inspector!
/// </summary>
public class LandmarkToggleController : MonoBehaviour
{
    public enum LandmarkType
    {
        Clock,
        HVAC
    }

    [Header("Settings")]
    [Tooltip("Which landmark does this toggle control?")]
    public LandmarkType landmarkType = LandmarkType.Clock;

    private Toggle m_Toggle;

    private void Awake()
    {
        m_Toggle = GetComponent<Toggle>();
        if (m_Toggle == null)
        {
            Debug.LogError($"[{gameObject.name}] LandmarkToggleController requires a Toggle component!", this);
            this.enabled = false;
            return;
        }

        Debug.Log($"[{gameObject.name}] LandmarkToggleController initialized for {landmarkType}");
    }

    /// <summary>
    /// Called when the GameObject becomes active. Initializes state and subscribes to events.
    /// </summary>
    private void OnEnable()
    {
        Debug.Log($"[{gameObject.name}] OnEnable called for {landmarkType} toggle");

        if (LandmarksManager.Instance != null && m_Toggle != null)
        {
            // Get the current state from LandmarksManager
            bool managerValue;

            if (landmarkType == LandmarkType.Clock)
            {
                managerValue = LandmarksManager.Instance.IsClockEnabled();
                Debug.Log($"[{gameObject.name}] Reading Clock state from manager: {managerValue}");
            }
            else
            {
                managerValue = LandmarksManager.Instance.IsHVACEnabled();
                Debug.Log($"[{gameObject.name}] Reading HVAC state from manager: {managerValue}");
            }

            // Set toggle state without triggering the listener
            m_Toggle.SetIsOnWithoutNotify(managerValue);

            Debug.Log($"[{gameObject.name}] Toggle set to: {m_Toggle.isOn}");
        }
        else if (m_Toggle != null)
        {
            Debug.LogError($"[{gameObject.name}] LandmarksManager instance not found during OnEnable!");
        }

        // Subscribe to future user clicks
        if (m_Toggle != null)
        {
            m_Toggle.onValueChanged.AddListener(OnToggleValueChanged);
            Debug.Log($"[{gameObject.name}] Listener added for {landmarkType}");
        }
    }

    /// <summary>
    /// Called when the GameObject becomes inactive. Unsubscribes from events.
    /// </summary>
    private void OnDisable()
    {
        Debug.Log($"[{gameObject.name}] OnDisable called for {landmarkType} toggle");

        if (m_Toggle != null)
        {
            m_Toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }

    /// <summary>
    /// Called ONLY when the USER clicks the toggle (due to the AddListener setup).
    /// </summary>
    private void OnToggleValueChanged(bool newValue)
    {
        Debug.Log($"[{gameObject.name}] OnToggleValueChanged called: {landmarkType} = {newValue}");

        if (LandmarksManager.Instance != null)
        {
            // Update the landmark state
            if (landmarkType == LandmarkType.Clock)
            {
                LandmarksManager.Instance.SetClockEnabled(newValue);
                Debug.Log($"[{gameObject.name}] Set Clock enabled to: {newValue}");
            }
            else
            {
                LandmarksManager.Instance.SetHVACEnabled(newValue);
                Debug.Log($"[{gameObject.name}] Set HVAC enabled to: {newValue}");
            }
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] LandmarksManager instance not found when trying to toggle {landmarkType}!");
        }
    }
}