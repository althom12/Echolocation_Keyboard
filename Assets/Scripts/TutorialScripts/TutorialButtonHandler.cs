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
        Debug.Log($"{buttonType} BUTTON SUBMITTED!");

        TutorialManager tutorialManager = TutorialManager.Instance;
        MenuNavigationManager navManager = FindObjectOfType<MenuNavigationManager>();

        if (tutorialManager == null || navManager == null)
        {
            Debug.LogError("TutorialManager or MenuNavigationManager is NULL!");
            return;
        }

        // Use the event pattern for proper timing
        if (buttonType == ButtonType.StartTutorial)
        {
            navManager.OnMenuFullyClosed.AddListener(tutorialManager.StartTutorial);
        }
        else
        {
            navManager.OnMenuFullyClosed.AddListener(tutorialManager.EndTutorial);
        }

        // Close the entire menu (not just subwindow)
        navManager.CloseEntireMenu();
    }
}