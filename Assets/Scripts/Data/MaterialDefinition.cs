using UnityEngine;
using AK.Wwise;

/// <summary>
/// Defines a single material that can be applied to custom obstacle columns.
/// Contains ONLY material data - no scene references (those go in MaterialUIBinding).
/// 
/// USAGE:
/// 1. Create new MaterialDefinition asset (Right-click ? Create ? Obstacles ? Material Definition)
/// 2. Set material name and acoustic texture
/// 3. Use in MaterialUIBinding to connect to UI elements
/// </summary>
[CreateAssetMenu(fileName = "Material_NewMaterial", menuName = "Obstacles/Material Definition")]
public class MaterialDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name (e.g., 'Carpet', 'Concrete', 'Wood')")]
    public string materialName;

    [Header("Wwise Configuration")]
    [Tooltip("The acoustic texture to apply to AkSurfaceReflector components")]
    public AcousticTexture acousticTexture;
}