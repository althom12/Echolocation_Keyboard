using UnityEngine;
using System; // Required for the Enum class

public class InputDebugger : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        // This loops through every possible key that Unity knows about.
        // It's more reliable than checking for a single key event.
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kcode))
            {
                // If a key was pressed down this frame, print its name to the Console.
                Debug.Log("KeyCode pressed: " + kcode);
            }
        }
    }
}