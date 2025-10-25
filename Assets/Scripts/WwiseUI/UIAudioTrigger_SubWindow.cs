using UnityEngine;
using AK.Wwise;

public class UIAudioTrigger_SubWindow : MonoBehaviour
{
    // Assign the unique sound for this window in the Inspector

    public AK.Wwise.Event OnWindowOpen;

    public void PlayOpenSound()
    {
        if (UIAudioManager.Instance != null && OnWindowOpen != null)
        {
            // Use the central manager's logic to stop the previous sound 
            // and post this new, specific one.
            UIAudioManager.Instance.PostStoppableEvent(OnWindowOpen, gameObject); // <--- NEW CALL
        }
    }
}