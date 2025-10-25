using UnityEngine;
using AK.Wwise;
using System.Collections.Generic;

public class ObstacleMaterialManager : MonoBehaviour
{
    // 1. DRAG YOUR ASSETS HERE
    [Header("Wwise Acoustic Textures")]
    public AcousticTexture carpetTexture;
    public AcousticTexture ConcreteTexture;
    public AcousticTexture woodTexture;

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

    public void SetAllMaterialsToWood()
    {
        Debug.Log("Setting " + ObstacleReflectors.Count + " obstacles to WOOD");
        UpdateMaterials(woodTexture);
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

    /// <summary>
    /// Clears the old list and finds all AkSurfaceReflectors
    /// in the children of the newly activated obstacle set.
    /// </summary>
    public void FindReflectorsInSet(GameObject obstacleSetParent)
    {
        // 1. Clear the old list of reflectors
        ObstacleReflectors.Clear();

        // 2. If the new set is null (e.g., "None" was selected), we're done.
        if (obstacleSetParent == null)
        {
            return;
        }

        // 3. Find all reflectors in the new set and add them to our list
        // We use GetComponentsInChildren(true) to find them even if they are inactive
        ObstacleReflectors.AddRange(obstacleSetParent.GetComponentsInChildren<AkSurfaceReflector>(true));

        Debug.Log("MaterialManager found " + ObstacleReflectors.Count + " reflectors in " + obstacleSetParent.name);
    }

}
    