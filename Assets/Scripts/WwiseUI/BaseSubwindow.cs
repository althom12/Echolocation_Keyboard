using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class BaseSubwindow : MonoBehaviour
{
    [Header("UI Configuration")]
    public GameObject firstSelectedElement;

    [Header("Audio Configuration")]
    public AK.Wwise.Event windowOpenEvent;

    protected AudioManager m_AudioManager;
    private uint windowOpenEventPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID; // NEW

    protected virtual void Start()
    {
        m_AudioManager = AudioManager.Instance;
        Debug.Log($"[BaseSubwindow] AudioManager found: {m_AudioManager != null}");
    }

    public virtual void OpenWindow()
    {
        // Ensure AudioManager is found
        if (m_AudioManager == null)
        {
            m_AudioManager = AudioManager.Instance;
        }

        if (m_AudioManager == null || windowOpenEvent == null || firstSelectedElement == null)
        {
            gameObject.SetActive(true);
            if (firstSelectedElement != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelectedElement);
            }
            return;
        }

        gameObject.SetActive(true);

        // AGGRESSIVELY stop all UI audio FIRST
        AkSoundEngine.StopAll(m_AudioManager.gameObject);
        Debug.Log($"[BaseSubwindow] Stopped all audio on AudioManager");

        // CRITICAL: Set the gate BEFORE selecting
        m_AudioManager.ClearPendingSelectionAudio();
        m_AudioManager.SetAudioState(UIAudioState.Window_Opening);
        Debug.Log($"[BaseSubwindow] Gate closed, state = Window_Opening");

        // Select the first element (audio will be cached)
        EventSystem.current.SetSelectedGameObject(firstSelectedElement);
        Debug.Log($"[BaseSubwindow] Selected first element: {firstSelectedElement.name}");

        // Start coroutine to post window event after stop takes effect
        StartCoroutine(PostWindowEventAfterStop());
    }

    /// <summary>
    /// NEW: Public method to stop window audio and clean up state.
    /// Called by MenuNavigationManager when closing the subwindow.
    /// </summary>
    public void StopWindowAudio()
    {
        if (windowOpenEventPlayingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.StopPlayingID(windowOpenEventPlayingID);
            windowOpenEventPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
            Debug.Log($"[BaseSubwindow] Stopped window open event");
        }
    }

    private IEnumerator PostWindowEventAfterStop()
    {
        Debug.Log($"[BaseSubwindow] Waiting one frame for audio stop to take effect");
        yield return null;

        Debug.Log($"[BaseSubwindow] Now posting window open event");
        uint flags = (uint)AkCallbackType.AK_EndOfEvent;
        windowOpenEventPlayingID = windowOpenEvent.Post( // STORE THE ID
            this.gameObject,
            flags,
            OnWindowAudioFinished,
            null
        );
        Debug.Log($"[BaseSubwindow] Posted window open event, PlayingID: {windowOpenEventPlayingID}");
    }

    private void OnWindowAudioFinished(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_EndOfEvent)
        {
            windowOpenEventPlayingID = AkSoundEngine.AK_INVALID_PLAYING_ID; // CLEAR THE ID

            // Only start coroutine if GameObject is still active
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(PlayPendingAudioAfterFrame());
            }
            else
            {
                Debug.LogWarning($"[BaseSubwindow] GameObject inactive, cannot play pending audio");
            }
        }
    }

    private IEnumerator PlayPendingAudioAfterFrame()
    {
        yield return null;

        if (m_AudioManager != null)
        {
            m_AudioManager.PlayPendingSelectionAudio();
            m_AudioManager.SetAudioState(UIAudioState.Idle);
        }
    }
}