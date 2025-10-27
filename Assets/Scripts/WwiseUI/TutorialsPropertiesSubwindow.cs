using UnityEngine;

/// <summary>
/// Simple subwindow with Start Tutorial and End Tutorial buttons.
/// </summary>
public class TutorialPropertiesSubwindow : BaseSubwindow
{
    [Header("Tutorial Manager Reference")]
    public TutorialManager tutorialManager;

    protected override void Start()
    {
        base.Start();

        if (tutorialManager == null)
        {
            tutorialManager = TutorialManager.Instance;
        }
    }

    /// <summary>
    /// Called when "Start Tutorial" button is pressed.
    /// </summary>
    public void OnStartTutorialPressed()
    {
        if (tutorialManager != null)
        {
            tutorialManager.StartTutorial();
        }

        // Close the subwindow
        MenuNavigationManager navManager = FindObjectOfType<MenuNavigationManager>();
        if (navManager != null)
        {
            navManager.CloseActiveSubWindow();
        }
    }

    /// <summary>
    /// Called when "End Tutorial" button is pressed.
    /// </summary>
    public void OnEndTutorialPressed()
    {
        if (tutorialManager != null)
        {
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