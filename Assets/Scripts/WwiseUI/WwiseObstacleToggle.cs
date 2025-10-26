using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WwiseObstacleToggle : MonoBehaviour, ISelectHandler
{
    [Header("Audio Channel")]
    public AudioEventChannelSO audioChannel;

    [Header("Selection Audio (Navigation)")]
    public AK.Wwise.Event selectionEvent;
    public AK.Wwise.Switch checkedSwitch;
    public AK.Wwise.Switch notCheckedSwitch;

    [Header("Action Audio (Toggle Changed)")]
    public AK.Wwise.Event toggleActionEvent;
    public AK.Wwise.Switch actionCheckedSwitch;
    public AK.Wwise.Switch actionUncheckedSwitch;

    private Toggle myToggle;
    private bool isCurrentlySelected = false; // NEW: Track if this toggle is selected

    private void Awake()
    {
        myToggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        if (myToggle != null)
        {
            myToggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
    }

    private void OnDisable()
    {
        if (myToggle != null)
        {
            myToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
        isCurrentlySelected = false; // NEW: Clear selection state
    }

    public void OnSelect(BaseEventData eventData)
    {
        isCurrentlySelected = true; // NEW: Mark as selected

        if (audioChannel == null || selectionEvent == null) return;

        bool isToggleOn = myToggle.isOn;
        AK.Wwise.Switch switchToSend = isToggleOn ? checkedSwitch : notCheckedSwitch;

        AudioEventChannelSO.WwiseEventPacket packet = new AudioEventChannelSO.WwiseEventPacket
        {
            WwiseEvent = selectionEvent,
            WwiseSwitch = switchToSend,
            Emitter = this.gameObject
        };

        audioChannel.RaiseEvent(packet);
    }

    /// <summary>
    /// Called when another object is selected (Unity doesn't have OnDeselect callback).
    /// We detect this in Update.
    /// </summary>
    private void Update()
    {
        // Check if we're no longer the selected object
        if (isCurrentlySelected && EventSystem.current.currentSelectedGameObject != this.gameObject)
        {
            isCurrentlySelected = false;
        }
    }

    /// <summary>
    /// Called when the toggle value changes.
    /// Only plays audio if THIS toggle is currently selected (user-initiated).
    /// </summary>
    private void OnToggleValueChanged(bool isOn)
    {
        // NEW: Only play action audio if this toggle is currently selected
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
}