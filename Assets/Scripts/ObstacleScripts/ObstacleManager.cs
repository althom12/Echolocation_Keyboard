using UnityEngine;
using AK.Wwise; // Make sure this is here for your Wwise Events
using UnityEngine.UI;
using System.Collections.Generic; // Required for List

public class ObstacleManager : MonoBehaviour
{
    [Header("Obstacle Layouts")]
    public GameObject[] obstacleSets;
    public AK.Wwise.Event[] activationSounds;

    [Header("Material Settings")]
    public AcousticTexture carpetTexture;
    public AcousticTexture ConcreteTexture;

    [Header("UI References")]
    public GameObject carpetIndicator;
    public GameObject concreteIndicator;
    public Toggle carpetMaterialToggle;
    public Toggle concreteMaterialToggle;

    // Internal list of active obstacle reflectors
    private List<AkSurfaceReflector> ObstacleReflectors = new List<AkSurfaceReflector>();
    private bool isUpdatingToggles = false; // Prevents recursive loops

    void Start()
    {
        // When the game starts, set the "no obstacles" state.
        SelectLayout(-1, 0);

        // --- MOVED FROM ObstacleMaterialManager ---
        // Subscribe to material toggle events
        if (carpetMaterialToggle != null)
        {
            carpetMaterialToggle.onValueChanged.AddListener(OnCarpetToggleChanged);
        }

        if (concreteMaterialToggle != null)
        {
            concreteMaterialToggle.onValueChanged.AddListener(OnConcreteToggleChanged);
        }
        // --- END MOVED SECTION ---
    }

    private void OnDestroy()
    {
        // --- MOVED FROM ObstacleMaterialManager ---
        // Unsubscribe to prevent memory leaks
        if (carpetMaterialToggle != null)
        {
            carpetMaterialToggle.onValueChanged.RemoveListener(OnCarpetToggleChanged);
        }

        if (concreteMaterialToggle != null)
        {
            concreteMaterialToggle.onValueChanged.RemoveListener(OnConcreteToggleChanged);
        }
        // --- END MOVED SECTION ---
    }

    /// <summary>
    /// Activates a layout, plays a sound, AND finds its reflectors.
    /// This is the main entry point called by ObstacleToggleHelper.
    /// </summary>
    public void SelectLayout(int obstacleIndex, int soundIndex)
    {
        AkSoundEngine.StopAll(gameObject);

        // 1. Play the sound
        if (soundIndex >= 0 && soundIndex < activationSounds.Length)
        {
            activationSounds[soundIndex]?.Post(gameObject);
        }

        // 2. Activate the correct obstacle set
        GameObject newlyActiveSet = null;
        for (int i = 0; i < obstacleSets.Length; i++)
        {
            bool shouldBeActive = (i == obstacleIndex);
            if (obstacleSets[i] != null)
            {
                obstacleSets[i].SetActive(shouldBeActive);
                if (shouldBeActive)
                {
                    newlyActiveSet = obstacleSets[i];
                }
            }
        }

        // 3. Find reflectors in the new set
        // (This logic was previously in ObstacleMaterialManager)
        FindReflectorsInSet(newlyActiveSet);
    }

    // -------------------------------------------------------------------
    // --- MATERIAL LOGIC (Moved from ObstacleMaterialManager) ---
    // -------------------------------------------------------------------

    /// <summary>
    /// Event handler for when the user clicks the Carpet toggle.
    /// </summary>
    private void OnCarpetToggleChanged(bool isOn)
    {
        if (isUpdatingToggles) return; // Prevent recursion
        if (isOn)
        {
            SetAllMaterialsToCarpet();
        }
    }

    /// <summary>
    /// Event handler for when the user clicks the Concrete toggle.
    /// </summary>
    private void OnConcreteToggleChanged(bool isOn)
    {
        if (isUpdatingToggles) return; // Prevent recursion
        if (isOn)
        {
            SetAllMaterialsToConcrete();
        }
    }

    /// <summary>
    /// Public function to set material to Carpet.
    /// Called by UI events (like the toggle itself) or other scripts.
    /// </summary>
    public void SetAllMaterialsToCarpet()
    {
        Debug.Log("Setting " + ObstacleReflectors.Count + " obstacles to CARPET");
        UpdateMaterials(carpetTexture);
        SetIndicatorActive(true, false); // Carpet is ON, Concrete is OFF
    }

    /// <summary>
    /// Public function to set material to Concrete.
    /// </summary>
    public void SetAllMaterialsToConcrete()
    {
        Debug.Log("Setting " + ObstacleReflectors.Count + " obstacles to CONCRETE");
        UpdateMaterials(ConcreteTexture);
        SetIndicatorActive(false, true); // Carpet is OFF, Concrete is ON
    }

    /// <summary>
    /// Manages the visibility of the visual indicators and toggle states.
    /// </summary>
    public void SetIndicatorActive(bool carpetActive, bool concreteActive)
    {
        // Prevent recursion while updating toggles
        isUpdatingToggles = true;

        if (carpetIndicator != null)
            carpetIndicator.SetActive(carpetActive);

        if (concreteIndicator != null)
            concreteIndicator.SetActive(concreteActive);

        if (carpetMaterialToggle != null && carpetMaterialToggle.isOn != carpetActive)
            carpetMaterialToggle.isOn = carpetActive;

        if (concreteMaterialToggle != null && concreteMaterialToggle.isOn != concreteActive)
            concreteMaterialToggle.isOn = concreteActive;

        isUpdatingToggles = false;
    }

    /// <summary>
    /// Internal logic to apply the texture to all active reflectors.
    /// </summary>
    private void UpdateMaterials(AcousticTexture newTexture)
    {
        if (newTexture == null)
        {
            Debug.LogError("The Acoustic Texture is not assigned in the Inspector!");
            return;
        }

        // CORRECT - Use the AcousticTexture property directly
        foreach (AkSurfaceReflector reflector in ObstacleReflectors)
        {
            if (reflector != null)
            {
                reflector.AcousticTexture = newTexture; // 
            }
        }
    }

    /// <summary>
    /// Clears the old list and finds all AkSurfaceReflectors
    /// in the children of the newly activated obstacle set.
    /// </summary>
    private void FindReflectorsInSet(GameObject obstacleSetParent)
    {
        ObstacleReflectors.Clear();

        if (obstacleSetParent == null)
        {
            // If selecting "None", clear the indicators
            SetIndicatorActive(false, false);
            return;
        }

        // Find all reflectors in the new set
        ObstacleReflectors.AddRange(obstacleSetParent.GetComponentsInChildren<AkSurfaceReflector>(true));
        Debug.Log("ObstacleManager found " + ObstacleReflectors.Count + " reflectors in " + obstacleSetParent.name);
    }
}
