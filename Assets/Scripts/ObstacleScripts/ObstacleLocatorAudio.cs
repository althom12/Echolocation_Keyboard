using UnityEngine;
using System.Collections;

public class ObstacleLocatorAudio : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("A reference to the TutorialManager script in the scene.")]
    public TutorialManager tutorialManager; // --- MODIFIED: Added reference

    [Header("Locator Settings")]
    [Tooltip("The tag assigned to all sound-emitting obstacles.")]
    public string obstacleTag = "Obstacle";

    [Header("Input Settings")]
    [Tooltip("The time in seconds to wait before the hold action is triggered.")]
    public float holdDuration = 0.1f;

    private float holdTimer;
    private bool holdActionWasTriggered;

    void Start()
    {
        // --- MODIFIED: Added a check to ensure the TutorialManager is assigned ---
        if (tutorialManager == null)
        {
            Debug.LogError("ObstacleLocatorAudio ERROR: TutorialManager reference is not assigned in the Inspector!", this.gameObject);
            this.enabled = false;
            return;
        }
        Debug.Log("Hold Duration is set to: " + holdDuration);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            holdTimer += Time.unscaledDeltaTime;

            if (holdTimer >= holdDuration && !holdActionWasTriggered)
            {
                PerformHoldAction();
                holdActionWasTriggered = true;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (!holdActionWasTriggered)
            {
                PerformShortPressAction();
            }
            holdTimer = 0;
            holdActionWasTriggered = false;
        }
    }

    private void PerformShortPressAction()
    {
        // --- MODIFIED: This now calls the pause function in the TutorialManager ---
        Debug.Log("Short Press Action -> Toggling Tutorial Pause");
        tutorialManager?.ToggleAudioPause();
    }

    private void PerformHoldAction()
    {
        Debug.Log("--- Hold Action: Locating nearest obstacle... ---");
        FindAndTriggerClosestObstacle();
    }

    void FindAndTriggerClosestObstacle()
    {
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag(obstacleTag);
        if (obstacles.Length == 0)
        {
            Debug.LogWarning("No obstacles with tag '" + obstacleTag + "' found in the scene.");
            return;
        }
        GameObject closestObstacle = null;
        float minDistance = Mathf.Infinity;
        foreach (GameObject obstacle in obstacles)
        {
            float distance = Vector3.Distance(transform.position, obstacle.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestObstacle = obstacle;
            }
        }
        if (closestObstacle != null)
        {
            StartCoroutine(PlaySoundWithTemporaryDisable(closestObstacle));
        }
    }

    IEnumerator PlaySoundWithTemporaryDisable(GameObject obstacle)
    {
        AkSurfaceReflector reflector = obstacle.GetComponent<AkSurfaceReflector>();
        AkEvent akEvent = obstacle.GetComponent<AkEvent>();
        if (akEvent == null)
        {
            Debug.LogWarning("Closest obstacle '" + obstacle.name + "' does not have an AkEvent component.", obstacle);
            yield break;
        }
        if (reflector != null)
        {
            reflector.enabled = false;
        }
        akEvent.HandleEvent(obstacle);
        Debug.Log("Play command sent to obstacle: " + obstacle.name + " (SurfaceReflector temporarily disabled)");

        yield return new WaitForSeconds(0.5f);

        if (reflector != null)
        {
            reflector.enabled = true;
        }
    }
}