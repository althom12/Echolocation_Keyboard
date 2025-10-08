// Required for using Unity's core functionalities like MonoBehaviour
using UnityEngine;
// THIS IS THE MOST IMPORTANT LINE: It allows the script to use the new Input System
using UnityEngine.InputSystem;

public class EcholocationController : MonoBehaviour
{
    // This creates a public slot in the Inspector. You will drag your 
    // "Play_Echolocation_Ping" event from the Wwise Picker onto this slot.
    [Tooltip("The Wwise event to post when the action is triggered.")]
    public AK.Wwise.Event echolocationEvent;

    // This creates another public slot for an Input Action. We will link our
    // "Echolocate" action (which we will create soon) to this.
    // Using 'InputActionProperty' is a flexible way to reference an action.
    [Tooltip("The Input System action that triggers the echolocation.")]
    public InputActionProperty echolocationAction;

    // This function is automatically called when the GameObject becomes active.
    // It's crucial for making the input system start listening for our action.
    void OnEnable()
    {
        echolocationAction.action.Enable();
    }

    // This is called when the GameObject is deactivated.
    // It's good practice to disable the action to prevent errors.
    void OnDisable()
    {
        echolocationAction.action.Disable();
    }

    // The Update function is called once every frame.
    void Update()
    {
        // This is the core logic. '.action.WasPressedThisFrame()' checks if the
        // linked input action (our spacebar press) was triggered on this exact frame.
        // This prevents the sound from firing continuously if the key is held down.
        if (echolocationAction.action.WasPressedThisFrame())
        {
            // First, we check if an event has actually been assigned in the Inspector
            // to prevent errors if the slot is empty.
            if (echolocationEvent != null && echolocationEvent.IsValid())
            {
                // This is the command that tells Wwise to play the sound.
                // '.Post(gameObject)' means "Play the 'echolocationEvent' at the
                // position of the GameObject this script is attached to."
                echolocationEvent.Post(gameObject);
            }
        }
    }
}