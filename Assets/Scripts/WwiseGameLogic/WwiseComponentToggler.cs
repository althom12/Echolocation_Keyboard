using UnityEngine;
using System.Collections;

/// <summary>
/// Gold Standard Wwise Component Toggler
/// 
/// Temporarily disables specified components during audio playback.
/// Designed for scenarios where components must be inactive while audio plays.
/// 
/// DESIGN PHILOSOPHY:
/// - Temporal Control: Manage component state over time
/// - Generic: Works with any MonoBehaviour component
/// - Predictable: Always re-enables components after configured delay
/// 
/// USE CASES:
/// - Disabling AkSurfaceReflector during obstacle locator pings
/// - Temporarily disabling collision audio during specific events
/// - Any scenario requiring component disable/play/enable sequence
/// 
/// REPLACES:
/// - ObstacleLocatorAudio's PlaySoundWithTemporaryDisable coroutine
/// </summary>
public class WwiseComponentToggler : MonoBehaviour
{
    // ???????????????????????????????????????????????????????????????
    // INSPECTOR FIELDS
    // ???????????????????????????????????????????????????????????????

    [Header("Wwise Configuration")]
    [Tooltip("The Wwise event to play while components are disabled")]
    public AK.Wwise.Event wwiseEvent;

    [Tooltip("GameObject to emit sound from. If null, uses this GameObject.")]
    public GameObject soundEmitter;

    [Header("Component Control")]
    [Tooltip("Components to temporarily disable during audio playback")]
    public MonoBehaviour[] componentsToToggle;

    [Tooltip("How long to wait (in seconds) before re-enabling components")]
    public float reEnableDelay = 0.5f;

    [Header("Options")]
    [Tooltip("Use unscaled time (immune to Time.timeScale changes)")]
    public bool useUnscaledTime = true;

    // ???????????????????????????????????????????????????????????????
    // PRIVATE FIELDS
    // ???????????????????????????????????????????????????????????????

    private Coroutine activeCoroutine = null;

    // ???????????????????????????????????????????????????????????????
    // UNITY LIFECYCLE
    // ???????????????????????????????????????????????????????????????

    private void Awake()
    {
        if (soundEmitter == null)
        {
            soundEmitter = this.gameObject;
        }
    }

    // ???????????????????????????????????????????????????????????????
    // PUBLIC API
    // ???????????????????????????????????????????????????????????????

    /// <summary>
    /// Triggers the disable/play/enable sequence.
    /// Can be called from UnityEvents or other scripts.
    /// </summary>
    public void PlayWithToggle()
    {
        if (wwiseEvent == null || !wwiseEvent.IsValid())
        {
            Debug.LogWarning($"[WwiseComponentToggler] '{gameObject.name}': wwiseEvent is not assigned or invalid!");
            return;
        }

        // Stop any existing coroutine
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }

        // Start new sequence
        activeCoroutine = StartCoroutine(ToggleSequence());
    }

    /// <summary>
    /// Plays audio with toggling on a specific GameObject's components.
    /// Useful when components are on a different GameObject than this script.
    /// </summary>
    public void PlayWithToggleOnGameObject(GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[WwiseComponentToggler] '{gameObject.name}': target GameObject is null!");
            return;
        }

        if (wwiseEvent == null || !wwiseEvent.IsValid())
        {
            Debug.LogWarning($"[WwiseComponentToggler] '{gameObject.name}': wwiseEvent is not assigned!");
            return;
        }

        // Stop any existing coroutine
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }

        // Get components from target
        MonoBehaviour[] targetComponents = target.GetComponents<MonoBehaviour>();

        // Start sequence with target components
        activeCoroutine = StartCoroutine(ToggleSequenceWithComponents(targetComponents, target));
    }

    /// <summary>
    /// Manually stop the toggle sequence and restore component states.
    /// </summary>
    public void StopToggle()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        // Re-enable all components
        ReEnableComponents(componentsToToggle);
    }

    // ???????????????????????????????????????????????????????????????
    // PRIVATE METHODS - Core Logic
    // ???????????????????????????????????????????????????????????????

    private IEnumerator ToggleSequence()
    {
        // 1. Disable components
        DisableComponents(componentsToToggle);

        // 2. Play the Wwise event
        wwiseEvent.Post(soundEmitter);
        Debug.Log($"[WwiseComponentToggler] '{gameObject.name}': Playing event '{wwiseEvent.Name}' with {componentsToToggle.Length} components disabled");

        // 3. Wait for specified duration
        if (useUnscaledTime)
        {
            yield return new WaitForSecondsRealtime(reEnableDelay);
        }
        else
        {
            yield return new WaitForSeconds(reEnableDelay);
        }

        // 4. Re-enable components
        ReEnableComponents(componentsToToggle);

        activeCoroutine = null;
    }

    private IEnumerator ToggleSequenceWithComponents(MonoBehaviour[] components, GameObject target)
    {
        // 1. Disable components
        DisableComponents(components);

        // 2. Play the Wwise event on target
        wwiseEvent.Post(target);
        Debug.Log($"[WwiseComponentToggler] '{gameObject.name}': Playing event on '{target.name}' with components disabled");

        // 3. Wait
        if (useUnscaledTime)
        {
            yield return new WaitForSecondsRealtime(reEnableDelay);
        }
        else
        {
            yield return new WaitForSeconds(reEnableDelay);
        }

        // 4. Re-enable components
        ReEnableComponents(components);

        activeCoroutine = null;
    }

    private void DisableComponents(MonoBehaviour[] components)
    {
        foreach (MonoBehaviour component in components)
        {
            if (component != null)
            {
                component.enabled = false;
                Debug.Log($"[WwiseComponentToggler] Disabled: {component.GetType().Name} on '{component.gameObject.name}'");
            }
        }
    }

    private void ReEnableComponents(MonoBehaviour[] components)
    {
        foreach (MonoBehaviour component in components)
        {
            if (component != null)
            {
                component.enabled = true;
                Debug.Log($"[WwiseComponentToggler] Re-enabled: {component.GetType().Name} on '{component.gameObject.name}'");
            }
        }
    }

    // ???????????????????????????????????????????????????????????????
    // DEBUG HELPERS
    // ???????????????????????????????????????????????????????????????

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (wwiseEvent == null)
        {
            Debug.LogWarning($"[WwiseComponentToggler] '{gameObject.name}': wwiseEvent is not assigned!");
        }

        if (componentsToToggle == null || componentsToToggle.Length == 0)
        {
            Debug.LogWarning($"[WwiseComponentToggler] '{gameObject.name}': componentsToToggle array is empty!");
        }

        if (reEnableDelay < 0)
        {
            Debug.LogWarning($"[WwiseComponentToggler] '{gameObject.name}': reEnableDelay is negative!");
            reEnableDelay = 0;
        }

        if (soundEmitter == null)
        {
            soundEmitter = this.gameObject;
        }
    }
#endif
}