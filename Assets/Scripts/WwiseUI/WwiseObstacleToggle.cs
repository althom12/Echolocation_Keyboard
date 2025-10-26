using UnityEngine;
using UnityEngine.UI; // Required for Toggle
using UnityEngine.EventSystems; // Required for ISelectHandler [12, 13]

/// <summary>
/// This script is attached to EACH of the 13 obstacle toggles.
/// It implements ISelectHandler to know when it's been selected. [12]
/// It holds references to its two specific Wwise Switches.
/// </summary>
public class WwiseObstacleToggle : MonoBehaviour, ISelectHandler
{
    [Header("Audio Channel")]


    public AudioEventChannelSO audioChannel; // Drag your 'UIAudioChannel' asset here




    public AK.Wwise.Event selectionEvent; // Drag 'Event_UI_Select' here




    public AK.Wwise.Switch checkedSwitch; // Drag 'Obstacle_1_Checked', etc. here




    public AK.Wwise.Switch notCheckedSwitch; // Drag 'Obstacle_1_NotChecked', etc. here

    // Internal reference to the Toggle component
    private Toggle myToggle;

    private void Awake()
    {
        myToggle = GetComponent<Toggle>();
    }

    /// <summary>
    /// Called by Unity's EventSystem when this object is selected. [14]
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        // 1. DETERMINE STATE: Check if the toggle is currently on or off
        bool isToggleOn = myToggle.isOn;

        // 2. CHOOSE THE SWITCH: Select the correct Wwise Switch to send
        AK.Wwise.Switch switchToSend = isToggleOn ? checkedSwitch : notCheckedSwitch;

        // 3. CREATE THE PACKET:
        AudioEventChannelSO.WwiseEventPacket packet = new AudioEventChannelSO.WwiseEventPacket
        {
            WwiseEvent = this.selectionEvent,
            WwiseSwitch = switchToSend, // We send the *specific* switch
            Emitter = this.gameObject
        };

        // 4. RAISE THE EVENT:
        // The AudioManager will receive this packet and handle all logic. [3]
        audioChannel.RaiseEvent(packet);
    }
}