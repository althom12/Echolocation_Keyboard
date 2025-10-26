using UnityEngine;

/// <summary>
/// Defines the states for the UI audio state machine.
/// This matches the Wwise State Group 'UI_Audio_State'.
/// </summary>
public enum UIAudioState
{
    Idle,
    Window_Opening
}

/// <summary>
/// Persistent singleton that acts as the "gatekeeper" for all UI audio. [1]
/// It listens to the AudioEventChannelSO and enforces priority/sequencing rules.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // Simple singleton instance
    public static AudioManager Instance { get; private set; }

    [Header("Event Channel Listener")]


    public AudioEventChannelSO audioChannel; // Drag your 'UIAudioChannel' asset here




    public AK.Wwise.State stateIdle; // Drag your 'Idle' State here



    public AK.Wwise.State stateWindowOpening; // Drag your 'Window_Opening' State here

    // The current state of our machine
    public UIAudioState m_CurrentAudioState = UIAudioState.Idle;

    // The cached audio request that is deferred during the Window_Opening state
    public AudioEventChannelSO.WwiseEventPacket? m_PendingSelectionAudio = null;

    private bool m_IsReturningToMainMenu = false;

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Subscribe to the audio event channel
        if (audioChannel != null)
        {
            audioChannel.OnEventRaised += OnAudioEventReceived;
        }
    }

    private void OnDestroy()
    {
        // Clean up subscription
        if (audioChannel != null)
        {
            audioChannel.OnEventRaised -= OnAudioEventReceived;
        }
    }

    /// <summary>
    /// The "gatekeeper" method.
    /// Receives all audio requests from UI elements.
    /// </summary>
    private void OnAudioEventReceived(AudioEventChannelSO.WwiseEventPacket packet)
    {
        Debug.Log($"[AudioManager] OnAudioEventReceived - Current State: {m_CurrentAudioState}");

        if (m_CurrentAudioState == UIAudioState.Window_Opening)
        {
            // STATE: Window_Opening (Gate is CLOSED)
            // Do NOT play the sound. Cache it.
            Debug.Log($"[AudioManager] Gate CLOSED - Caching audio packet");
            m_PendingSelectionAudio = packet;
        }
        else // m_CurrentAudioState == UIAudioState.Idle
        {
            // STATE: Idle (Gate is OPEN)
            // Play the sound immediately.
            Debug.Log($"[AudioManager] Gate OPEN - Playing audio immediately");
            PlaySelectionAudio(packet);
        }
    }

    /// <summary>
    /// Internal function that sets the switch and posts the event to Wwise.
    /// </summary>
    private void PlaySelectionAudio(AudioEventChannelSO.WwiseEventPacket packet)
    {
        if (packet.Emitter == null || packet.WwiseEvent == null || packet.WwiseSwitch == null)
        {
            return;
        }

        // 1. Set the Switch on THIS AudioManager, not the original emitter
        packet.WwiseSwitch.SetValue(this.gameObject);

        // 2. Post the event from THIS AudioManager
        // All selection sounds now come from one source, automatically replacing each other
        packet.WwiseEvent.Post(this.gameObject);
    }

    // --- PUBLIC METHODS (Called by SubwindowController) --- //

    /// <summary>
    /// Called by the Wwise 'AK_EndOfEvent' callback.
    /// Plays the cached selection sound.
    /// </summary>
    public void PlayPendingSelectionAudio()
    {
        Debug.Log($"[AudioManager] PlayPendingSelectionAudio called. Has pending audio: {m_PendingSelectionAudio.HasValue}");

        if (m_PendingSelectionAudio.HasValue)
        {
            Debug.Log($"[AudioManager] Playing cached selection audio");
            PlaySelectionAudio(m_PendingSelectionAudio.Value);
            m_PendingSelectionAudio = null; // Clear the cache
        }
        else
        {
            Debug.LogWarning($"[AudioManager] No pending audio to play!");
        }
    }

    /// <summary>
    /// Clears any pending audio.
    /// </summary>
    public void ClearPendingSelectionAudio()
    {
        m_PendingSelectionAudio = null;
    }

    /// <summary>
    /// Sets the global UI audio state in both C# and Wwise.
    /// This is the "lock" and "unlock" mechanism.
    /// </summary>
    public void SetAudioState(UIAudioState newState)
    {
        Debug.Log($"[AudioManager] SetAudioState: {m_CurrentAudioState} ? {newState}");
        m_CurrentAudioState = newState;

        // Set the state in Wwise
        switch (newState)
        {
            case UIAudioState.Idle:
                stateIdle?.SetValue();
                break;
            case UIAudioState.Window_Opening:
                stateWindowOpening?.SetValue();
                break;
        }
    }

    public void SetReturningToMainMenu(bool isReturning)
    {
        m_IsReturningToMainMenu = isReturning;
    }

    public bool IsReturningToMainMenu()
    {
        return m_IsReturningToMainMenu;
    }


}