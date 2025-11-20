using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement; // Needed for EndTutorial reloading

public class TutorialDirector : MonoBehaviour
{
    [Header("Core References")]
    public FirstPersonController playerController;
    public ObstacleManager obstacleManager;
    public Transform spawnPointsContainer; // ASSIGN THIS in Inspector!

    [Header("Defaults")]
    public TutorialChapterSO defaultStartChapter;

    private TutorialChapterSO currentChapter;
    private bool isChapterActive;
    private float timer;

    private void Start()
    {
        // 1. Check if the Menu requested a specific chapter
        TutorialChapterSO chapterToLoad = TutorialContext.RequestedChapter;

        // 2. If not (just hit Play in editor), load default
        if (chapterToLoad == null)
        {
            chapterToLoad = defaultStartChapter;
        }

        // 3. Clear context and begin
        TutorialContext.Clear();
        LoadChapter(chapterToLoad);
    }

    public void LoadChapter(TutorialChapterSO chapter)
    {
        if (chapter == null)
        {
            EndTutorial();
            return;
        }

        currentChapter = chapter;
        isChapterActive = true;
        timer = 0f;

        // --- SPAWN LOGIC (Safer Container Method) ---
        Transform spawnPoint = spawnPointsContainer.Find(chapter.spawnPointID);

        if (spawnPoint != null)
        {
            playerController.Teleport(spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError($"CRITICAL: Spawn Point '{chapter.spawnPointID}' not found inside container!");
        }

        // --- OBSTACLE LOGIC ---
        if (obstacleManager != null)
        {
            // Matches the 'disableObstacles' bool in the SO
            obstacleManager.enabled = !chapter.disableObstacles;
        }

        // --- AUDIO LOGIC ---
        AkSoundEngine.StopAll(gameObject);
        if (chapter.instructionEvent != null)
        {
            chapter.instructionEvent.Post(gameObject);
        }
    }

    private void Update()
    {
        if (!isChapterActive) return;

        switch (currentChapter.completionType)
        {
            case CompletionType.InputBacktick:
                if (Input.GetKeyDown(KeyCode.BackQuote)) CompleteChapter();
                break;

            case CompletionType.WaitForAudioEnd:
                timer += Time.deltaTime;
                if (timer >= currentChapter.durationSeconds) CompleteChapter();
                break;
        }
    }

    public void CompleteChapter()
    {
        LoadChapter(currentChapter.nextChapter);
    }

    private void EndTutorial()
    {
        SceneManager.LoadScene("MainMenu");
    }
}