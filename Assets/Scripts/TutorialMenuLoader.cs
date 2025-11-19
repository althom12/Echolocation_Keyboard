using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorialMenuLoader : MonoBehaviour
{
    public Transform listContentContainer;
    public GameObject buttonPrefab;

    [Header("Configuration")]
    // Drag your "MasterTutorialList" asset here
    public TutorialSequenceSO tutorialSequence;

    void Start()
    {
        GenerateButtons();
    }

    public void GenerateButtons()
    {
        foreach (Transform child in listContentContainer) Destroy(child.gameObject);

        if (tutorialSequence == null || tutorialSequence.chapters.Count == 0)
        {
            Debug.LogWarning("No Tutorial Sequence assigned or list is empty!");
            return;
        }

        // LOOP THROUGH THE LIST (Order is determined by your drag-and-drop order)
        foreach (var chapter in tutorialSequence.chapters)
        {
            if (chapter == null) continue;

            GameObject newBtn = Instantiate(buttonPrefab, listContentContainer);

            // Update Text
            TextMeshProUGUI btnText = newBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = chapter.chapterName;

            // Update Click
            Button btnComp = newBtn.GetComponent<Button>();
            btnComp.onClick.AddListener(() =>
            {
                LaunchTutorial(chapter);
            });
        }
    }

    void LaunchTutorial(TutorialChapterSO chapter)
    {
        TutorialContext.RequestedChapter = chapter;
        SceneManager.LoadScene("Scene_Tutorial"); // Ensure this matches your scene name
    }
}