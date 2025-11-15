using UnityEngine;
using UnityEngine.UI;


public class StartTutorialButton : MonoBehaviour
{
    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();
        myButton.onClick.AddListener(OnButtonClicked);
    }

    public void OnButtonClicked()
    {
        Debug.Log("START TUTORIAL BUTTON CLICKED!");

        TutorialManager tutorialManager = TutorialManager.Instance;
        if (tutorialManager == null)
        {
            Debug.LogError("TutorialManager.Instance is NULL!");
            return;
        }

        // Find the MenuNavigationManager
        // Note: FindObjectOfType is slow. A direct reference assigned
        // in the inspector is much more efficient if possible.
        MenuNavigationManager navManager = FindObjectOfType<MenuNavigationManager>();
        if (navManager != null)
        {
            // --- THIS IS THE NEW LOGIC ---
            // 1. Subscribe StartTutorial to the OnMenuFullyClosed event.
            //    This ensures StartTutorial() is only called AFTER the
            //    menu close sound plays and Time.timeScale is 1.
            navManager.OnMenuFullyClosed.AddListener(tutorialManager.StartTutorial);

            // 2. Now, tell the menu to close.
            navManager.CloseEntireMenu();
        }
        else
        {
            Debug.LogError("MenuNavigationManager not found in scene!");
        }
    }
}