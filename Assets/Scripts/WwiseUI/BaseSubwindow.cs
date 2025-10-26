using UnityEngine;
using UnityEngine.EventSystems;
using AK.Wwise;

/// <summary>
/// Base class for all subwindow panels that need audio orchestration.
/// Can be used directly or inherited for custom behavior.
/// </summary>
public class BaseSubwindow : MonoBehaviour
{
    [Header("UI Configuration")]
    public GameObject firstSelectedElement;

    [Header("Audio Configuration")]
    public AK.Wwise.Event windowOpenEvent;

    protected AudioManager m_AudioManager;

    protected virtual void Start()
    {
        m_AudioManager = AudioManager.Instance;
    }

    public virtual void OpenWindow()
    {
        Debug.Log($"[BaseSubwindow] OpenWindow called for {gameObject.name}");

        // Check if AudioManager is null
        if (m_AudioManager == null)
        {
            Debug.LogWarning($"[BaseSubwindow] AudioManager is NULL, attempting to find it...");
            m_AudioManager = AudioManager.Instance;

            if (m_AudioManager == null)
            {
                Debug.LogError($"[BaseSubwindow] AudioManager.Instance is NULL! Cannot proceed with audio.");
            }
        }

        if (m_AudioManager == null || windowOpenEvent == null || firstSelectedElement == null)
        {
            Debug.LogWarning($"[BaseSubwindow] Missing references - falling back to simple activation:");
            Debug.LogWarning($"  - AudioManager: {(m_AudioManager != null ? "OK" : "NULL")}");
            Debug.LogWarning($"  - windowOpenEvent: {(windowOpenEvent != null ? "OK" : "NULL")}");
            Debug.LogWarning($"  - firstSelectedElement: {(firstSelectedElement != null ? "OK" : "NULL")}");

            // Fallback
            gameObject.SetActive(true);
            if (firstSelectedElement != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelectedElement);
            }
            return;
        }

        Debug.Log($"[BaseSubwindow] All references valid, proceeding with audio orchestration");

        gameObject.SetActive(true);
        m_AudioManager.ClearPendingSelectionAudio();
        m_AudioManager.SetAudioState(UIAudioState.Window_Opening);

        Debug.Log($"[BaseSubwindow] Posting Wwise event...");
        uint flags = (uint)AkCallbackType.AK_EndOfEvent;
        uint playingID = windowOpenEvent.Post(
            this.gameObject,
            flags,
            OnWindowAudioFinished,
            null
        );

        Debug.Log($"[BaseSubwindow] Wwise event posted. PlayingID: {playingID}");

        EventSystem.current.SetSelectedGameObject(firstSelectedElement);
        Debug.Log($"[BaseSubwindow] Selected first element: {firstSelectedElement.name}");
    }

    private void OnWindowAudioFinished(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_EndOfEvent)
        {
            m_AudioManager.PlayPendingSelectionAudio();
            m_AudioManager.SetAudioState(UIAudioState.Idle);
        }
    }
}