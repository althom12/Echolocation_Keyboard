using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeyboardNavigator : MonoBehaviour
{
    void Update()
    {
        // Listen for TAB key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Navigate();
        }
    }

    private void Navigate()
    {
        // Check if an object is currently selected
        GameObject currentObj = EventSystem.current.currentSelectedGameObject;
        if (currentObj == null) return;

        // Get the Selectable component (Button, Toggle, etc.)
        Selectable currentSelectable = currentObj.GetComponent<Selectable>();
        if (currentSelectable == null) return;

        Selectable nextSelectable = null;

        // Check for SHIFT key (Go Backward/Up)
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            // Try to find the element "Up" or "Left"
            nextSelectable = currentSelectable.FindSelectableOnUp();
            if (nextSelectable == null) nextSelectable = currentSelectable.FindSelectableOnLeft();
        }
        else // Normal Tab (Go Forward/Down)
        {
            // Try to find the element "Down" or "Right"
            nextSelectable = currentSelectable.FindSelectableOnDown();
            if (nextSelectable == null) nextSelectable = currentSelectable.FindSelectableOnRight();
        }

        // If we found a target, select it
        if (nextSelectable != null)
        {
            nextSelectable.Select();
        }
    }
}