using UnityEngine;
using UnityEngine.SceneManagement; // REQUIRED for loading scenes

public class MainMenuManager : MonoBehaviour
{
    // Call this from your "Play Game" button
    public void PlayGame()
    {
        // Replace "Scene_MainGame" with the EXACT name of your game scene file
        SceneManager.LoadScene("Scene_MainGame");
    }

    // Call this from your "Quit" button
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }

    // The Tutorial Buttons will be handled by the "TutorialMenuLoader" 
    // script we wrote earlier, so you don't need a function here for that.
}