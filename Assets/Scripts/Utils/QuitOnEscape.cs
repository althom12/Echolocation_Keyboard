using UnityEngine;

public class QuitOnEscape : MonoBehaviour
{
    void Update()
    {
        // Check if the user has pressed the Escape key in the current frame.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Escape key was pressed. Quitting application.");

#if UNITY_EDITOR
            // If we are running in the Unity Editor, stop playing.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // If we are running in a standalone build, quit the application.
            Application.Quit();
#endif
        }
    }
}