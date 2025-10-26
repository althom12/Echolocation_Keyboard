using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Manages the opening of the "Obstacles" subwindow.
/// This is the "Orchestrator" that triggers the Wwise State logic
/// to ensure the window sound plays before the autoselect sound.
/// </summary>
public class ObstaclesSubwindow : MonoBehaviour
{
    [Header("UI Configuration")]


    public GameObject firstSelectedElement; // Drag your first toggle here




    public AK.Wwise.Event windowOpenEvent; // Drag your "Event_Window_Obstacles_Open"

    // Reference to the AudioManager singleton
    private AudioManager m_AudioManager;

    private void Start()
    {
        // Find the persistent AudioManager instance
        m_AudioManager = AudioManager.Instance;
    }

    /// <summary>
    /// Public function to open this subwindow.
    /// This is called by your MenuNavigationManager.
    /// </summary>
    public void OpenWindow()
    {
        if (m_AudioManager == null) return;
        if (windowOpenEvent == null || firstSelectedElement == null) return;

        // 0. Activate the window
        gameObject.SetActive(true);

        // 1. Clear any old pending audio
        m_AudioManager.ClearPendingSelectionAudio();

        // 2. SET THE LOCK: Tell the AudioManager to enter the 'Window_Opening' state.
        m_AudioManager.SetAudioState(UIAudioState.Window_Opening);

        // 3. DEFINE CALLBACK FLAGS: We want to be notified when the event is over.
        uint flags = (uint)AkCallbackType.AK_EndOfEvent; 

        // 4. POST EVENT WITH CALLBACK:
        // Post the "window open" sound and register our callback function. [7, 16]
        windowOpenEvent.Post(
            this.gameObject,
            flags,
            OnWindowAudioFinished, // The function to call when done
            null
        );

        // 5. TRIGGER AUTOSELECT:
        // This fires OnSelect on the first toggle *immediately*. [17, 6]
        // The AudioManager's "gate" is closed, so it will cache this sound.
        EventSystem.current.SetSelectedGameObject(firstSelectedElement);
    }

    /// <summary>
    // This function is EXECUTED BY WWISE when the windowOpenEvent finishes. [7]
    /// </summary>
    private void OnWindowAudioFinished(object in_cookie, AkCallbackType in_type, object in_info)
    {
        // Check if the callback type is the one we registered for [8]
        if (in_type == AkCallbackType.AK_EndOfEvent)
        {
            // The "window open" sound is done.

            // 6. PLAY PENDING SOUND:
            m_AudioManager.PlayPendingSelectionAudio();

            // 7. RELEASE THE LOCK:
            m_AudioManager.SetAudioState(UIAudioState.Idle);
        }
    }
}