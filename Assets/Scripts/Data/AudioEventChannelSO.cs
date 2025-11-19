using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A ScriptableObject-based event channel for audio requests. [2]
/// This acts as a "broker" to decouple the UI (raiser) from the AudioManager (listener). [3]
/// Based on the decoupled pattern from 
/// </summary>
[CreateAssetMenu(fileName = "AudioEventChannel", menuName = "Audio/Audio Event Channel")]
public class AudioEventChannelSO : ScriptableObject
{
    /// <summary>
    /// The data packet sent with every audio request.
    /// This avoids hard-coding strings or event IDs. 
    /// </summary>
    public struct WwiseEventPacket
    {
        public AK.Wwise.Event WwiseEvent;
        public AK.Wwise.Switch WwiseSwitch; // The specific Switch to set
        public GameObject Emitter;
    }

    public UnityAction<WwiseEventPacket> OnEventRaised;

    /// <summary>
    /// Called by UI elements (e.g., WwiseObstacleToggle) to request a sound.
    /// </summary>
    public void RaiseEvent(WwiseEventPacket packet)
    {
        // Invoke the event for any listeners (e.g., the AudioManager)
        OnEventRaised?.Invoke(packet);
    }
}