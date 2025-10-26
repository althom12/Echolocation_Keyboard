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

    private Toggle myToggle;

    private void Awake()
    {
        // Only get Toggle component if it exists on this GameObject
        myToggle = GetComponent<Toggle>();
    }

    /// <summary>
    /// Called by Unity's EventSystem when this element is selected.
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
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