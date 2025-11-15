using UnityEngine;
using UnityEngine.UI;


public class EndTutorialButton : MonoBehaviour
{
    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();
        myButton.onClick.AddListener(OnButtonClicked);
    }

    public void OnButtonClicked()
    {
        Debug.Log("END TUTORIAL BUTTON CLICKED!");

        TutorialManager tutorialManager = TutorialManager.Instance;
        if (tutorialManager == null)
        {
            Debug.LogError("TutorialManager.Instance is NULL!");
            return;
        }

        MenuNavigationManager navManager = FindObjectOfType<MenuNavigationManager>();
        if (navManager != null)
        {
            // --- APPLYING THE SAME DECOUPLED PATTERN ---
            // 1. Subscribe EndTutorial to the OnMenuFullyClosed event.
            navManager.OnMenuFullyClosed.AddListener(tutorialManager.EndTutorial);

            // 2. Now, tell the menu to close.
            navManager.CloseEntireMenu();
        }
        else
        {
            Debug.LogError("MenuNavigationManager not found in scene!");
        }
    }
}