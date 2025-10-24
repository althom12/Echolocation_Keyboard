using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

// Assumes the generated C# class is named "CustomUIActions"
public class CustomUISubmitHandler : MonoBehaviour
{
    private CustomInputActions _input;

    private void Awake()
    {
        // Initialize the input actions wrapper class 
        _input = new CustomInputActions();
    }

    private void OnEnable()
    {
        // Subscribe to the 'performed' event for our custom 'Tab' action [8]
        _input.UI.OpenSubMenu.performed += OnTabPressed;

        // Enable the "UI" Action Map 
        _input.UI.Enable();
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        _input.UI.OpenSubMenu.performed -= OnTabPressed;

        // Disable the "UI" Action Map 
        _input.UI.Disable();
    }

    // This callback function will execute when the 'Tab' key is pressed
    private void OnTabPressed(InputAction.CallbackContext context)
    {
        // This is the core logic, detailed in Step 3.3
        TriggerSubmitOnSelectedObject();
    }

    private void TriggerSubmitOnSelectedObject()
    {
        // 1. Get the currently selected GameObject from the EventSystem
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject; 

        // 2. A crucial null-check
        if (selectedObject == null)
        {
            // No object is selected, so do nothing.
            return;
        }

        // 3. Create a BaseEventData for the event
        BaseEventData eventData = new BaseEventData(EventSystem.current);

        // 4. Manually execute a 'Submit' event on the selected object 
        ExecuteEvents.Execute(
            selectedObject,
            eventData,
            ExecuteEvents.submitHandler
        );
    }

    public void EnableOpenSubMenu()
    {
        _input.UI.OpenSubMenu.Enable();
    }

    public void DisableOpenSubMenu()
    {
        _input.UI.OpenSubMenu.Disable();
    }
}