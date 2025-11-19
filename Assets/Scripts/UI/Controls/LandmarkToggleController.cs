using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the UI Toggle for landmark audio.
/// Place this script directly on the Toggle GameObject.
/// 
/// REFACTORED: Now uses landmarkIndex instead of enum for scalability.
/// Works with data-driven LandmarksManager.
/// </summary>
public class LandmarkToggleController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Index of the landmark this toggle controls (0 = first in LandmarksManager array)")]
    public int landmarkIndex = 0;

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

        Debug.Log($"[{gameObject.name}] LandmarkToggleController initialized for landmark index {landmarkIndex}");
    }

    /// <summary>
    /// Called when the GameObject becomes active. Initializes state and subscribes to events.
    /// </summary>
    private void OnEnable()
    {
        Debug.Log($"[{gameObject.name}] OnEnable called for landmark index {landmarkIndex}");

        if (LandmarksManager.Instance != null && m_Toggle != null)
        {
            // Get the current state from LandmarksManager
            bool managerValue = LandmarksManager.Instance.IsLandmarkEnabled(landmarkIndex);

            // Get landmark name for better logging
            string landmarkName = "Unknown";
            LandmarkUIBinding binding = LandmarksManager.Instance.GetLandmark(landmarkIndex);
            if (binding != null && binding.landmarkDefinition != null)
            {
                landmarkName = binding.landmarkDefinition.landmarkName;
            }

            Debug.Log($"[{gameObject.name}] Reading '{landmarkName}' (index {landmarkIndex}) state from manager: {managerValue}");

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
            Debug.Log($"[{gameObject.name}] Listener added for landmark index {landmarkIndex}");
        }
    }

    /// <summary>
    /// Called when the GameObject becomes inactive. Unsubscribes from events.
    /// </summary>
    private void OnDisable()
    {
        Debug.Log($"[{gameObject.name}] OnDisable called for landmark index {landmarkIndex}");

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
        Debug.Log($"[{gameObject.name}] OnToggleValueChanged called: landmark index {landmarkIndex} = {newValue}");

        if (LandmarksManager.Instance != null)
        {
            // Update the landmark state
            LandmarksManager.Instance.SetLandmarkEnabled(landmarkIndex, newValue);

            // Get landmark name for logging
            string landmarkName = "Unknown";
            LandmarkUIBinding binding = LandmarksManager.Instance.GetLandmark(landmarkIndex);
            if (binding != null && binding.landmarkDefinition != null)
            {
                landmarkName = binding.landmarkDefinition.landmarkName;
            }

            Debug.Log($"[{gameObject.name}] Set '{landmarkName}' (index {landmarkIndex}) enabled to: {newValue}");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] LandmarksManager instance not found when trying to toggle landmark index {landmarkIndex}!");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (landmarkIndex < 0)
        {
            Debug.LogWarning($"[{gameObject.name}] landmarkIndex is negative! Setting to 0.");
            landmarkIndex = 0;
        }
    }
#endif
}