using UnityEngine;
using AK.Wwise;
using System.Collections.Generic;
using UnityEngine.UI; // <--- THIS IS THE CRITICAL FIX

public class ObstacleMaterialManager : MonoBehaviour
{
    // 1. DRAG YOUR ACOUSTIC TEXTURES HERE

    public AcousticTexture carpetTexture;
    public AcousticTexture ConcreteTexture;

    // 2. DRAG YOUR UI INDICATORS AND TOGGLES HERE

    public GameObject carpetIndicator;
    public GameObject concreteIndicator;

    // References to the UI Toggles themselves (for clearing state)
    public Toggle carpetMaterialToggle;
    public Toggle concreteMaterialToggle;

    // 3. TARGET OBSTACLES (Populated by code)

    public List<AkSurfaceReflector> ObstacleReflectors = new List<AkSurfaceReflector>();

    // ------------------------------------

    // 4. PUBLIC FUNCTIONS FOR YOUR UI

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

    // --- NEW FUNCTION ---
    /// <summary>
    /// Manages the visibility of the visual indicators and clears the toggle states.
    /// </summary>
    public void SetIndicatorActive(bool carpetActive, bool concreteActive)
    {
        // Set Visual Indicators
        if (carpetIndicator != null)
        {
            carpetIndicator.SetActive(carpetActive);
        }
        if (concreteIndicator != null)
        {
            concreteIndicator.SetActive(concreteActive);
        }

        // Set Toggle States (Crucial for unchecking the UI elements via code)
        if (carpetMaterialToggle != null)
        {
            // Only set if the state is different to avoid recursive event calls
            if (carpetMaterialToggle.isOn != carpetActive)
            {
                carpetMaterialToggle.isOn = carpetActive;
            }
        }
        if (concreteMaterialToggle != null)
        {
            if (concreteMaterialToggle.isOn != concreteActive)
            {
                concreteMaterialToggle.isOn = concreteActive;
            }
        }
    }


    // 5. THE LOGIC
    private void UpdateMaterials(AcousticTexture newTexture)
    {
        if (newTexture == null)
        {
            Debug.LogError("The Acoustic Texture is not assigned in the Inspector!");
            return;
        }

        // Loop through every reflector in our PUBLIC list
        foreach (AkSurfaceReflector reflector in ObstacleReflectors)
        {
            if (reflector != null)
            {
                // Assign the new Wwise Acoustic Texture to the object
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
            // If selecting "None", we explicitly clear the indicators
            SetIndicatorActive(false, false);
            return;
        }

        // Find all reflectors in the new set and add them to our list
        ObstacleReflectors.AddRange(obstacleSetParent.GetComponentsInChildren<AkSurfaceReflector>(true));
        Debug.Log("MaterialManager found " + ObstacleReflectors.Count + " reflectors in " + obstacleSetParent.name);
    }
}