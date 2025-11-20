public static class TutorialContext
{
    // The Menu sets this before loading the tutorial scene
    public static TutorialChapterSO RequestedChapter;

    // Helper to clear data when done
    public static void Clear()
    {
        RequestedChapter = null;
    }
}