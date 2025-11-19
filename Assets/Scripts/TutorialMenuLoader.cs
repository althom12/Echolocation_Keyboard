using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // Required for setting selection
using System.Collections;

public class TutorialMenuLoader : MonoBehaviour
{
    [Header("UI References")]
    public Transform listContentContainer;
    public GameObject buttonPrefab;
    public TutorialSequenceSO tutorialSequence;

    // We use OnEnable so this runs EVERY time you open the menu
    private void OnEnable()
    {
        // If the list is empty, generate it. 
        // If it's already generated (from opening it before), just select the first one.
        if (listContentContainer.childCount == 0)
        {
            GenerateButtons();
        }

        // Wait one frame to ensure the UI is fully active before selecting
        StartCoroutine(SelectFirstButton());
    }

    public void GenerateButtons()
    {
        // (Your existing Generate code goes here...)
        foreach (Transform child in listContentContainer) Destroy(child.gameObject);

        if (tutorialSequence == null) return;

        foreach (var chapter in tutorialSequence.chapters)
        {
            GameObject newBtn = Instantiate(buttonPrefab, listContentContainer);
            newBtn.GetComponentInChildren<TextMeshProUGUI>().text = chapter.chapterName;
            Button btnComp = newBtn.GetComponent<Button>();
            btnComp.onClick.AddListener(() => { LaunchTutorial(chapter); });
        }
    }

    private IEnumerator SelectFirstButton()
    {
        // Wait for end of frame so Unity UI can update its layout
        yield return new WaitForEndOfFrame();

        if (listContentContainer.childCount > 0)
        {
            // Get the first button child
            GameObject firstButton = listContentContainer.GetChild(0).gameObject;

            // Tell the Event System to select it
            EventSystem.current.SetSelectedGameObject(firstButton);

            Debug.Log("Auto-selected first button.");
        }
    }

    void LaunchTutorial(TutorialChapterSO chapter)
    {
        TutorialContext.RequestedChapter = chapter;
        SceneManager.LoadScene("Scene_Tutorial");
    }
}