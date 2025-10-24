using UnityEngine;
using UnityEngine.UI; // Required for Toggle

/// <summary>
/// Manages the UI Toggle for the footstep setting.
/// Place this script directly on the Toggle GameObject.
/// </summary>
 // Ensures a Toggle component exists on the GameObject
public class FootstepsToggleController : MonoBehaviour
{
    private Toggle m_Toggle;

    private void Awake()
    {
        m_Toggle = GetComponent<Toggle>();
        if (m_Toggle == null)
        {
            Debug.LogError("FootstepsToggleController requires a Toggle component on the same GameObject.", this);
            this.enabled = false; // Disable script if Toggle is missing
        }
    }

    /// <summary>
    /// Called when the GameObject becomes active. Initializes state and subscribes to events. 
    /// </summary>
    /// <summary>
    /// Called when the GameObject becomes active. Initializes state and subscribes to events.
    /// </summary>
    private void OnEnable()
    {
        // --- ADDED DEBUG LOG ---
        Debug.Log("FootstepsToggleController.OnEnable: Starting.");

        if (SettingsManager.Instance != null && m_Toggle != null)
        {
            // --- ADDED VARIABLE DEFINITION AND DEBUG LOGS ---
            bool managerValue = SettingsManager.Instance.IsFootstepsEnabled; // Define the variable here
            Debug.Log($"FootstepsToggleController.OnEnable: Reading SettingsManager.IsFootstepsEnabled = {managerValue}. Calling SetIsOnWithoutNotify.");

            m_Toggle.SetIsOnWithoutNotify(managerValue); // Use the variable to set the toggle state [1]

            Debug.Log($"FootstepsToggleController.OnEnable: SetIsOnWithoutNotify completed. Current toggle isOn = {m_Toggle.isOn}. Adding listener.");
            // --- END ADDED SECTION ---
        }
        else if (m_Toggle != null) // Only log error if toggle exists but manager doesn't
        {
            Debug.LogError("SettingsManager instance not found during OnEnable! Cannot set initial toggle state.");
            // Optionally default the toggle state here if manager is missing on enable
            // m_Toggle.SetIsOnWithoutNotify(true); // Default to on?
        }


        // Subscribe to future user clicks
        if (m_Toggle != null)
        {
            m_Toggle.onValueChanged.AddListener(OnToggleValueChanged); 
            // --- ADDED DEBUG LOG ---
            Debug.Log("FootstepsToggleController.OnEnable: Listener added.");
        }
    }

    /// <summary>
    /// Called when the GameObject becomes inactive. Unsubscribes from events. [19, 20, 17, 18]
    /// </summary>
    private void OnDisable()
    {
        // Clean up listener [19, 20]
        if (m_Toggle != null)
        {
            m_Toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }

    /// <summary>
    /// Called ONLY when the USER clicks the toggle (due to the AddListener setup). [9, 16, 17]
    /// </summary>
    private void OnToggleValueChanged(bool newValue)
    {
        if (SettingsManager.Instance != null)
        {
            // Update the central setting
            SettingsManager.Instance.IsFootstepsEnabled = newValue;

            // Save the change
            SettingsManager.Instance.SaveSettings();

            // Optional: Play UI feedback sound here (using global scope recommended)
            // Example: YourUIToggleSoundEvent?.Post(null); 
        }
        else
        {
            Debug.LogError("SettingsManager instance not found when trying to save toggle change!");
        }
    }
}