using UnityEngine;

[CreateAssetMenu(fileName = "NewChapter", menuName = "Tutorial/Chapter")]
public class TutorialChapterSO : ScriptableObject
{
    [Header("UI Display")]
    public string chapterName;
    [TextArea] public string description;

    [Header("Scene Configuration")]
    public string spawnPointID;

    // --- THIS IS THE MISSING LINE CAUSING YOUR ERROR ---
    public bool disableObstacles;
    // --------------------------------------------------

    [Header("Audio Logic")]
    // Ensure you have the Wwise namespace if using AK.Wwise.Event
    // If this errors, use: public string instructionEventName; 
    public AK.Wwise.Event instructionEvent;

    [Header("Completion Rules")]
    public CompletionType completionType;
    public float durationSeconds;
    public string actionID;

    [Header("Linked List")]
    public TutorialChapterSO nextChapter;
}

public enum CompletionType
{
    InputBacktick,
    WaitForAudioEnd,
    PlayerAction,
    ReachTargetZone
}