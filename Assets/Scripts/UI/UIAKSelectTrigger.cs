using UnityEngine;
using UnityEngine.EventSystems;
using AK.Wwise;
using UnityEngine.UI; // Required for Selectable checks

public class UIAkSelectTrigger : MonoBehaviour, ISelectHandler, IDeselectHandler
{

    public int ElementIndex = 0; // Set 0 for the first element, 1 for the slider, etc.

    // --- New Local Flag (Managed by MenuNavigationManager) ---
    private bool _hasBeenSelected = false;


    public AK.Wwise.Event OnStandardFocus; // For the Pitch Slider (ElementIndex > 0)

    public AK.Wwise.Event OnReturnFocus;    // For the First Element (ElementIndex == 0)


    // --- Public function called by MenuNavigationManager to reset state ---
    public void ResetSelectionState()
    {
        _hasBeenSelected = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (UIAudioManager.Instance == null) return;

        UIAudioManager.StopCurrentGlobalSound();

        // ===============================================
        // A. BLOCK INITIAL SELECTION SOUND
        // ===============================================
        if (!_hasBeenSelected)
        {
            // First time selecting this item since the window opened.
            _hasBeenSelected = true;

            // Critical Requirement: If this is the element auto-selected on open, 
            // we must be silent. We have confirmed the silent OpenSubWindow logic
            // works by checking _hasBeenSelected.
            return;
        }

        // ===============================================
        // B. HANDLE RETURN/STANDARD NAVIGATION SOUNDS
        // ===============================================

        // 1. Return Focus Sound (Element 0 only)
        if (ElementIndex == 0 && OnReturnFocus != null)
        {
            UIAudioManager.Instance.PostStoppableEvent(OnReturnFocus, gameObject);
        }
        // 2. Standard Focus Sound (Pitch Slider, etc.)
        else if (OnStandardFocus != null)
        {
            UIAudioManager.Instance.PostStoppableEvent(OnStandardFocus, gameObject);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.StopCurrentGlobalSound();
        }
    }
}