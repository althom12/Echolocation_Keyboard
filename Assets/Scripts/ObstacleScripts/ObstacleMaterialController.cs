using UnityEngine;
using AK.Wwise;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Obstacle Material Controller
/// 
/// Manages acoustic material properties for CUSTOM COLUMNS ONLY (obstacle presets 7-10).
/// Extracted from ObstacleManager to follow Single Responsibility Principle.
/// 
/// RESPONSIBILITIES:
/// - Track AkSurfaceReflector components in active custom columns
/// - Apply material textures (Carpet/Concrete) to reflectors
/// - Manage UI toggle listeners and visual indicators
/// - Handle toggle state synchronization
/// 
/// DOES NOT:
/// - Handle obstacle layout selection (that's ObstacleManager's job)
/// - Apply materials to baked presets (1-6) - they have fixed materials
/// 
/// USAGE:
/// 1. Attach to the same GameObject as ObstacleManager
/// 2. Assign material textures in Inspector
/// 3. Assign UI toggle and indicator references
/// 4. ObstacleManager calls UpdateReflectorList() when custom columns change
/// 5. UI toggles call ApplyCarpet()/ApplyConcrete() via UnityEvents or toggle listeners
/// </summary>
public class ObstacleMaterialController : MonoBehaviour
{
    // ???????????????????????????????????????????????????????????
    // INSPECTOR FIELDS
    // ???????????????????????????????????????????????????????????

    [Header("Material Assets")]
    [Tooltip("The Carpet acoustic texture from Wwise")]
    public AcousticTexture carpetTexture;

    [Tooltip("The Concrete acoustic texture from Wwise")]
    public AcousticTexture concreteTexture;

    [Header("UI References")]
    [Tooltip("The Carpet toggle in the Obstacles UI")]
    public Toggle carpetMaterialToggle;

    [Tooltip("The Concrete toggle in the Obstacles UI")]
    public Toggle concreteMaterialToggle;

    [Header("Visual Indicators")]
    [Tooltip("Visual indicator GameObject for Carpet (optional)")]
    public GameObject carpetIndicator;

    [Tooltip("Visual indicator GameObject for Concrete (optional)")]
    public GameObject concreteIndicator;

    // ???????????????????????????????????????????????????????????
    // PRIVATE FIELDS
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// List of AkSurfaceReflector components in the currently active custom column.
    /// Updated by ObstacleManager when custom column selection changes.
    /// </summary>
    private List<AkSurfaceReflector> obstacleReflectors = new List<AkSurfaceReflector>();

    /// <summary>
    /// Prevents recursive toggle updates when programmatically setting toggle states.
    /// Same pattern as original ObstacleManager implementation.
    /// </summary>
    private bool isUpdatingToggles = false;

    // ???????????????????????????????????????????????????????????
    // UNITY LIFECYCLE
    // ???????????????????????????????????????????????????????????

    private void Start()
    {
        // Subscribe to material toggle events
        if (carpetMaterialToggle != null)
        {
            carpetMaterialToggle.onValueChanged.AddListener(OnCarpetToggleChanged);
        }
        else
        {
            Debug.LogWarning("[ObstacleMaterialController] carpetMaterialToggle is not assigned!");
        }

        if (concreteMaterialToggle != null)
        {
            concreteMaterialToggle.onValueChanged.AddListener(OnConcreteToggleChanged);
        }
        else
        {
            Debug.LogWarning("[ObstacleMaterialController] concreteMaterialToggle is not assigned!");
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

    // ???????????????????????????????????????????????????????????
    // PUBLIC API - Called by ObstacleManager
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Updates the list of reflectors to match the currently active custom column.
    /// Called by ObstacleManager whenever custom column selection changes.
    /// 
    /// IMPORTANT: This clears any existing material assignments!
    /// When switching columns, materials are NOT remembered (per client requirements).
    /// </summary>
    /// <param name="customColumnObject">The currently active custom column GameObject, or null if none</param>
    public void UpdateReflectorList(GameObject customColumnObject)
    {
        obstacleReflectors.Clear();

        if (customColumnObject == null)
        {
            // No custom column active - clear indicators and reset toggles
            Debug.Log("[ObstacleMaterialController] No custom column active. Clearing reflector list.");
            SetIndicatorActive(false, false);
            return;
        }

        // Find all AkSurfaceReflector components in the active custom column
        // includeInactive = true because some child objects might be disabled
        obstacleReflectors.AddRange(customColumnObject.GetComponentsInChildren<AkSurfaceReflector>(true));

        Debug.Log($"[ObstacleMaterialController] Found {obstacleReflectors.Count} reflectors in '{customColumnObject.name}'");

        // Reset material indicators (user must re-select material)
        SetIndicatorActive(false, false);
    }

    /// <summary>
    /// Clears all reflectors and resets UI state.
    /// Called by ObstacleManager when switching to baked presets or "None".
    /// </summary>
    public void ClearReflectors()
    {
        obstacleReflectors.Clear();
        SetIndicatorActive(false, false);
        Debug.Log("[ObstacleMaterialController] Reflectors cleared.");
    }

    // ???????????????????????????????????????????????????????????
    // PUBLIC API - Material Application (can be called externally)
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Applies the Carpet material to all reflectors in the active custom column.
    /// Can be called directly by other scripts or via UI events.
    /// </summary>
    public void ApplyCarpet()
    {
        Debug.Log($"[ObstacleMaterialController] Applying CARPET to {obstacleReflectors.Count} reflectors");
        UpdateMaterials(carpetTexture);
        SetIndicatorActive(true, false); // Carpet ON, Concrete OFF
    }

    /// <summary>
    /// Applies the Concrete material to all reflectors in the active custom column.
    /// Can be called directly by other scripts or via UI events.
    /// </summary>
    public void ApplyConcrete()
    {
        Debug.Log($"[ObstacleMaterialController] Applying CONCRETE to {obstacleReflectors.Count} reflectors");
        UpdateMaterials(concreteTexture);
        SetIndicatorActive(false, true); // Carpet OFF, Concrete ON
    }

    // ???????????????????????????????????????????????????????????
    // PRIVATE METHODS - Toggle Event Handlers
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Event handler for when the user clicks the Carpet toggle.
    /// Only triggers when toggle is turned ON (not when programmatically changed).
    /// </summary>
    private void OnCarpetToggleChanged(bool isOn)
    {
        if (isUpdatingToggles) return; // Prevent recursion

        if (isOn)
        {
            ApplyCarpet();
        }
    }

    /// <summary>
    /// Event handler for when the user clicks the Concrete toggle.
    /// Only triggers when toggle is turned ON (not when programmatically changed).
    /// </summary>
    private void OnConcreteToggleChanged(bool isOn)
    {
        if (isUpdatingToggles) return; // Prevent recursion

        if (isOn)
        {
            ApplyConcrete();
        }
    }

    // ???????????????????????????????????????????????????????????
    // PRIVATE METHODS - Internal Logic
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Internal logic to apply the acoustic texture to all active reflectors.
    /// Uses Wwise 2024.5 compatible API: reflector.AcousticTexture property.
    /// </summary>
    private void UpdateMaterials(AcousticTexture newTexture)
    {
        if (newTexture == null)
        {
            Debug.LogError("[ObstacleMaterialController] Acoustic Texture is null! Cannot apply material.");
            return;
        }

        if (obstacleReflectors.Count == 0)
        {
            Debug.LogWarning("[ObstacleMaterialController] No reflectors to update. Is a custom column active?");
            return;
        }

        // Apply texture to all reflectors in the active custom column
        foreach (AkSurfaceReflector reflector in obstacleReflectors)
        {
            if (reflector != null)
            {
                // Wwise 2024.5 API: Use the AcousticTexture property directly
                reflector.AcousticTexture = newTexture;
            }
        }

        Debug.Log($"[ObstacleMaterialController] Successfully applied '{newTexture.Name}' to {obstacleReflectors.Count} reflectors");
    }

    /// <summary>
    /// Manages the visibility of visual indicators and synchronizes toggle states.
    /// Uses the isUpdatingToggles flag to prevent recursive onValueChanged calls.
    /// </summary>
    /// <param name="carpetActive">Should Carpet indicator be visible?</param>
    /// <param name="concreteActive">Should Concrete indicator be visible?</param>
    public void SetIndicatorActive(bool carpetActive, bool concreteActive)
    {
        // Prevent recursion while updating toggles programmatically
        isUpdatingToggles = true;

        // Update visual indicators (if assigned)
        if (carpetIndicator != null)
        {
            carpetIndicator.SetActive(carpetActive);
        }

        if (concreteIndicator != null)
        {
            concreteIndicator.SetActive(concreteActive);
        }

        // Synchronize toggle states (only if they differ from target state)
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

    // ???????????????????????????????????????????????????????????
    // DEBUG HELPERS
    // ???????????????????????????????????????????????????????????

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Validation warnings in Inspector
        if (carpetTexture == null)
        {
            Debug.LogWarning($"[ObstacleMaterialController] '{gameObject.name}': carpetTexture is not assigned!");
        }

        if (concreteTexture == null)
        {
            Debug.LogWarning($"[ObstacleMaterialController] '{gameObject.name}': concreteTexture is not assigned!");
        }

        if (carpetMaterialToggle == null)
        {
            Debug.LogWarning($"[ObstacleMaterialController] '{gameObject.name}': carpetMaterialToggle is not assigned!");
        }

        if (concreteMaterialToggle == null)
        {
            Debug.LogWarning($"[ObstacleMaterialController] '{gameObject.name}': concreteMaterialToggle is not assigned!");
        }
    }
#endif
}