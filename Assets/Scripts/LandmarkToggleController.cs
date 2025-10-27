using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the UI Toggle for landmark audio (Clock or HVAC).
/// Place this script directly on the Toggle GameObject.
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
            Debug.LogError("LandmarkToggleController requires a Toggle component on the same GameObject.", this);
            this.enabled = false;
        }
    }

    /// <summary>
    /// Called when the GameObject becomes active. Initializes state and subscribes to events.
    /// </summary>
    private void OnEnable()
    {
        Debug.Log($"LandmarkToggleController.OnEnable: Starting for {landmarkType}.");

        if (LandmarksManager.Instance != null && m_Toggle != null)
        {
            // Get the current state from LandmarksManager
            bool managerValue = (landmarkType == LandmarkType.Clock)
                ? LandmarksManager.Instance.IsClockEnabled()
                : LandmarksManager.Instance.IsHVACEnabled();

            Debug.Log($"LandmarkToggleController.OnEnable: Reading LandmarksManager.Is{landmarkType}Enabled = {managerValue}. Calling SetIsOnWithoutNotify.");

            // Set toggle state without triggering the listener
            m_Toggle.SetIsOnWithoutNotify(managerValue);

            Debug.Log($"LandmarkToggleController.OnEnable: SetIsOnWithoutNotify completed. Current toggle isOn = {m_Toggle.isOn}. Adding listener.");
        }
        else if (m_Toggle != null)
        {
            Debug.LogError("LandmarksManager instance not found during OnEnable! Cannot set initial toggle state.");
        }

        // Subscribe to future user clicks
        if (m_Toggle != null)
        {
            m_Toggle.onValueChanged.AddListener(OnToggleValueChanged);
            Debug.Log("LandmarkToggleController.OnEnable: Listener added.");
        }
    }

    /// <summary>
    /// Called when the GameObject becomes inactive. Unsubscribes from events.
    /// </summary>
    private void OnDisable()
    {
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
        if (LandmarksManager.Instance != null)
        {
            // Update the landmark state
            if (landmarkType == LandmarkType.Clock)
            {
                LandmarksManager.Instance.SetClockEnabled(newValue);
                Debug.Log($"Clock toggled to: {newValue}");
            }
            else
            {
                LandmarksManager.Instance.SetHVACEnabled(newValue);
                Debug.Log($"HVAC toggled to: {newValue}");
            }

            // Note: LandmarksManager already handles starting/stopping the sounds,
            // so we don't need to save settings separately unless you want persistence
        }
        else
        {
            Debug.LogError("LandmarksManager instance not found when trying to toggle landmark!");
        }
    }
}