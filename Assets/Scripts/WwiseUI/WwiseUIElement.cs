using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Generic script for any UI element (Toggle, Slider, Button, etc.) that needs audio on selection.
/// Supports optional state-based switches for toggles.
/// Use this for all NEW UI elements going forward.
/// </summary>
public class WwiseUIElement : MonoBehaviour, ISelectHandler
{
    [Header("Audio Channel")]
    public AudioEventChannelSO audioChannel; // Drag your 'UIAudioChannel' asset here

    [Header("Selection Audio")]
    public AK.Wwise.Event selectionEvent; // Drag your selection event here

    [Header("Toggle State Switches (Optional)")]
    [Tooltip("Only use these for Toggle elements that need different audio based on checked/unchecked state")]
    public AK.Wwise.Switch toggleOnSwitch;  // For toggle checked state
    public AK.Wwise.Switch toggleOffSwitch; // For toggle unchecked state

    [Header("Simple Switch (For Sliders/Buttons)")]
    [Tooltip("Use this for sliders, buttons, or any element that always uses the same switch")]
    public AK.Wwise.Switch simpleSwitch;

    [Header("Toggle Action Audio (Optional)")]
    [Tooltip("Only for toggles - plays when value changes")]
    public AK.Wwise.Event toggleActionEvent;
    public AK.Wwise.Switch actionCheckedSwitch;
    public AK.Wwise.Switch actionUncheckedSwitch;

    private Toggle myToggle;
    private bool isCurrentlySelected = false;

    private void Awake()
    {
        // Only get Toggle component if it exists on this GameObject
        myToggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        // Subscribe to toggle value changes if this is a toggle
        if (myToggle != null)
        {
            myToggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (myToggle != null)
        {
            myToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
        isCurrentlySelected = false;
    }

    private void Update()
    {
        // Check if we're no longer the selected object
        if (isCurrentlySelected && EventSystem.current.currentSelectedGameObject != this.gameObject)
        {
            isCurrentlySelected = false;
        }
    }

    /// <summary>
    /// Called by Unity's EventSystem when this element is selected.
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        isCurrentlySelected = true;

        if (audioChannel == null || selectionEvent == null) return;

        // Determine which switch to use based on element type
        AK.Wwise.Switch switchToSend = DetermineSwitch();

        if (switchToSend == null) return;

        // Create the packet
        AudioEventChannelSO.WwiseEventPacket packet = new AudioEventChannelSO.WwiseEventPacket
        {
            WwiseEvent = selectionEvent,
            WwiseSwitch = switchToSend,
            Emitter = this.gameObject
        };

        // Raise the event through the audio channel
        audioChannel.RaiseEvent(packet);
    }

    /// <summary>
    /// Called when the toggle value changes.
    /// Only plays audio if THIS toggle is currently selected (user-initiated).
    /// </summary>
    private void OnToggleValueChanged(bool isOn)
    {
        // Only play action audio if this toggle is currently selected
        if (!isCurrentlySelected) return;

        if (audioChannel == null || toggleActionEvent == null) return;

        AK.Wwise.Switch actionSwitch = isOn ? actionCheckedSwitch : actionUncheckedSwitch;
        if (actionSwitch == null) return;

        AudioEventChannelSO.WwiseEventPacket packet = new AudioEventChannelSO.WwiseEventPacket
        {
            WwiseEvent = toggleActionEvent,
            WwiseSwitch = actionSwitch,
            Emitter = this.gameObject
        };

        audioChannel.RaiseEvent(packet);
    }

    /// <summary>
    /// Determines which switch to use based on the element type and state.
    /// </summary>
    private AK.Wwise.Switch DetermineSwitch()
    {
        // Check if this is a Toggle with state-based switches assigned
        if (myToggle != null && toggleOnSwitch != null && toggleOffSwitch != null)
        {
            // Return the appropriate switch based on toggle state
            return myToggle.isOn ? toggleOnSwitch : toggleOffSwitch;
        }

        // Otherwise use the simple switch (for sliders, buttons, or toggles without state audio)
        return simpleSwitch;
    }
}