using UnityEngine;
using AK.Wwise;
using System.Collections.Generic;

public class ObstacleMaterialManager : MonoBehaviour
{
    // 1. DRAG YOUR ASSETS HERE
    [Header("Wwise Acoustic Textures")]
    public AcousticTexture carpetTexture;
    public AcousticTexture ConcreteTexture;

    // 2. DRAG YOUR 6 OBSTACLES HERE
    [Header("Target Obstacles")]
    public List<AkSurfaceReflector> ObstacleReflectors = new List<AkSurfaceReflector>();

    // 3. PUBLIC FUNCTIONS FOR YOUR UI
    public void SetAllMaterialsToCarpet()
    {
        Debug.Log("Setting " + ObstacleReflectors.Count + " obstacles to CARPET");
        UpdateMaterials(carpetTexture);
    }

    public void SetAllMaterialsToConcrete()
    {
        Debug.Log("Setting " + ObstacleReflectors.Count + " obstacles to CONCRETE");
        UpdateMaterials(ConcreteTexture);
    }

    // 4. THE LOGIC - Fixed parameter type
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

}
    