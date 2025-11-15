using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialButtonHandler : MonoBehaviour, ISubmitHandler
{
    public enum ButtonType
    {
        StartTutorial,
        EndTutorial
    }

    public ButtonType buttonType;

    public void OnSubmit(BaseEventData eventData)
    {
        Debug.Log($"==========================================");
        Debug.Log($"{buttonType} BUTTON SUBMITTED!");
        Debug.Log($"==========================================");

        TutorialManager tutorialManager = TutorialManager.Instance;

        if (tutorialManager == null)
        {
            Debug.LogError("TutorialManager.Instance is NULL!");
            return;
        }

        if (buttonType == ButtonType.StartTutorial)
        {
            Debug.Log("Calling StartTutorial()...");
            tutorialManager.StartTutorial();
        }
        else
        {
            Debug.Log("Calling EndTutorial()...");
            tutorialManager.EndTutorial();
        }

        // Close the subwindow
        MenuNavigationManager navManager = FindObjectOfType<MenuNavigationManager>();
        if (navManager != null)
        {
            navManager.CloseActiveSubWindow();
        }
    }
}