using UnityEngine;
using UnityEngine.UI; // For standard Button
using TMPro;          // For TMP_Dropdown
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TutorialMenuLoader : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown chapterDropdown;
    public Button launchButton;

    [Header("Data")]
    public TutorialSequenceSO tutorialSequence;

    private void Start()
    {
        InitializeDropdown();

        // Hook up the launch button
        launchButton.onClick.AddListener(LaunchSelectedChapter);
    }

    private void InitializeDropdown()
    {
        // 1. Clear any dummy options (like "Option A")
        chapterDropdown.ClearOptions();

        if (tutorialSequence == null) return;

        // 2. Create a list of options from your ScriptableObjects
        List<string> optionNames = new List<string>();

        foreach (var chapter in tutorialSequence.chapters)
        {
            // Add the name to the list
            optionNames.Add(chapter.chapterName);
        }

        // 3. Feed the list into the dropdown
        chapterDropdown.AddOptions(optionNames);
    }

    public void LaunchSelectedChapter()
    {
        // 1. Get the index of the selected item (0, 1, 2...)
        int index = chapterDropdown.value;

        // 2. Safety check
        if (index < 0 || index >= tutorialSequence.chapters.Count)
        {
            Debug.LogError("Invalid Chapter Selection");
            return;
        }

        // 3. Find the matching data object
        TutorialChapterSO selectedChapter = tutorialSequence.chapters[index];

        // 4. Launch
        Debug.Log($"Loading: {selectedChapter.chapterName}");
        TutorialContext.RequestedChapter = selectedChapter;
        SceneManager.LoadScene("Scene_Tutorial");
    }
}