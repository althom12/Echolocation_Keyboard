using UnityEngine;
using AK.Wwise;
using System.Collections.Generic;
using UnityEngine.UI;

public class ObstacleMaterialManager : MonoBehaviour
{
    // 1. DRAG YOUR ACOUSTIC TEXTURES HERE
    public AcousticTexture carpetTexture;
    public AcousticTexture ConcreteTexture;

    // 2. DRAG YOUR UI INDICATORS AND TOGGLES HERE
    public GameObject carpetIndicator;
    public GameObject concreteIndicator;

    // References to the UI Toggles
    public Toggle carpetMaterialToggle;
    public Toggle concreteMaterialToggle;

    // 3. TARGET OBSTACLES (Populated by code)
    public List<AkSurfaceReflector> ObstacleReflectors = new List<AkSurfaceReflector>();

    // Track if we're programmatically changing toggles to prevent recursion
    private bool isUpdatingToggles = false;

    // ------------------------------------
    // INITIALIZE TOGGLE LISTENERS
    private void Start()
    {
        // Subscribe to toggle events
        if (carpetMaterialToggle != null)
        {
            carpetMaterialToggle.onValueChanged.AddListener(OnCarpetToggleChanged);
        }

        if (concreteMaterialToggle != null)
        {
            concreteMaterialToggle.onValueChanged.AddListener(OnConcreteToggleChanged);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (carpetMaterialToggle != null)
        {
            carpetMaterialToggle.onValueChanged.RemoveListener(OnCarpetToggleChanged);
        }

        if (concreteMaterialToggle != null)
        {
            concreteMaterialToggle.onValueChanged.RemoveListener(OnConcreteToggleChanged);
        }
    }

    // ------------------------------------
    // TOGGLE EVENT HANDLERS

    private void OnCarpetToggleChanged(bool isOn)
    {
        if (isUpdatingToggles) return; // Prevent recursion

        if (isOn) // Only act when toggle is turned ON
        {
            SetAllMaterialsToCarpet();
        }
    }

    private void OnConcreteToggleChanged(bool isOn)
    {
        if (isUpdatingToggles) return; // Prevent recursion

        if (isOn) // Only act when toggle is turned ON
        {
            SetAllMaterialsToConcrete();
        }
    }

    // ------------------------------------
    // 4. PUBLIC FUNCTIONS FOR MATERIAL SWITCHING

    public void SetAllMaterialsToCarpet()
    {
        Debug.Log("Setting " + ObstacleReflectors.Count + " obstacles to CARPET");
        UpdateMaterials(carpetTexture);
        SetIndicatorActive(true, false); // Carpet is ON, Concrete is OFF
    }

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

        // Set Visual Indicators
        if (carpetIndicator != null)
        {
            carpetIndicator.SetActive(carpetActive);
        }
        if (concreteIndicator != null)
        {
            concreteIndicator.SetActive(concreteActive);
        }

        // Set Toggle States
        if (carpetMaterialToggle != null && carpetMaterialToggle.isOn != carpetActive)
        {
            carpetMaterialToggle.isOn = carpetActive;
        }
        if (concreteMaterialToggle != null && concreteMaterialToggle.isOn != concreteActive)
        {
            concreteMaterialToggle.isOn = concreteActive;
        }

        isUpdatingToggles = false;
    }

    // 5. THE LOGIC
    private void UpdateMaterials(AcousticTexture newTexture)
    {
        if (newTexture == null)
        {
            Debug.LogError("The Acoustic Texture is not assigned in the Inspector!");
            return;
        }

        foreach (AkSurfaceReflector reflector in ObstacleReflectors)
        {
            if (reflector != null)
            {
                reflector.AcousticTexture = newTexture;
            }
        }
    }

    /// <summary>
    /// Clears the old list and finds all AkSurfaceReflectors
    /// in the children of the newly activated obstacle set.
    /// </summary>
    public void FindReflectorsInSet(GameObject obstacleSetParent)
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
        Debug.Log("MaterialManager found " + ObstacleReflectors.Count + " reflectors in " + obstacleSetParent.name);
    }
}