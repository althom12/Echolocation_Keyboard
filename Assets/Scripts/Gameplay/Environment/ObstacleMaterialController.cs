using UnityEngine;
using AK.Wwise;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Links a MaterialDefinition (asset) to UI elements (scene objects).
/// This solves the ScriptableObject limitation where assets can't reference scene objects.
/// </summary>
[System.Serializable]
public class MaterialUIBinding
{
    [Tooltip("The material definition asset")]
    public MaterialDefinition materialDefinition;

    [Tooltip("The UI Toggle in the Obstacles menu")]
    public Toggle toggle;

    [Tooltip("Optional visual indicator GameObject")]
    public GameObject visualIndicator;
}

/// <summary>
/// Obstacle Material Controller - REFACTORED for Scalability
/// 
/// Uses MaterialDefinition ScriptableObjects + MaterialUIBinding for data-driven material management.
/// MaterialDefinition contains material data (name, acoustic texture).
/// MaterialUIBinding links MaterialDefinition to UI elements (toggles, indicators).
/// 
/// USAGE:
/// 1. Create MaterialDefinition assets for each material (Carpet, Concrete, Wood, etc.)
/// 2. In Inspector, add MaterialUIBinding entries
/// 3. For each binding: assign MaterialDefinition + Toggle + Visual Indicator
/// 4. This script automatically handles toggle subscriptions and material application
/// 
/// SCALABILITY:
/// - 2 materials: Works
/// - 50 materials: Works exactly the same way
/// - No per-material methods needed
/// </summary>
public class ObstacleMaterialController : MonoBehaviour
{
    // ???????????????????????????????????????????????????????????????
    // INSPECTOR FIELDS
    // ???????????????????????????????????????????????????????????????

    [Header("Material Bindings")]
    [Tooltip("Links material definitions to UI elements. Add/remove materials by modifying this array!")]
    public MaterialUIBinding[] materialBindings;

    // ???????????????????????????????????????????????????????????????
    // PRIVATE FIELDS
    // ???????????????????????????????????????????????????????????????

    /// <summary>
    /// AkSurfaceReflector components in the currently active custom column.
    /// Updated when ObstacleManager switches to a custom column (6-8).
    /// </summary>
    private List<AkSurfaceReflector> obstacleReflectors = new List<AkSurfaceReflector>();

    /// <summary>
    /// Tracks which material is currently applied (index into materials array).
    /// -1 = no material applied.
    /// </summary>
    private int activeMaterialIndex = -1;

    /// <summary>
    /// Prevents recursive toggle updates when programmatically setting toggle states.
    /// </summary>
    private bool isUpdatingToggles = false;

    // ???????????????????????????????????????????????????????????????
    // UNITY LIFECYCLE
    // ???????????????????????????????????????????????????????????????

    private void Start()
    {
        // Validate materialBindings array
        if (materialBindings == null || materialBindings.Length == 0)
        {
            Debug.LogError("[ObstacleMaterialController] No material bindings assigned! Please assign MaterialUIBinding entries in Inspector.");
            this.enabled = false;
            return;
        }

        // Subscribe to all material toggle events
        for (int i = 0; i < materialBindings.Length; i++)
        {
            MaterialUIBinding binding = materialBindings[i];

            // Validate each binding
            if (binding == null || binding.materialDefinition == null)
            {
                Debug.LogWarning($"[ObstacleMaterialController] Binding at index {i} is null or has no material definition! Skipping.");
                continue;
            }

            if (binding.toggle == null)
            {
                Debug.LogWarning($"[ObstacleMaterialController] Material '{binding.materialDefinition.materialName}' has no toggle assigned! Skipping.");
                continue;
            }

            // Subscribe to toggle's onValueChanged event
            // We capture 'i' in a local variable to avoid closure issues
            int materialIndex = i;
            binding.toggle.onValueChanged.AddListener((isOn) => OnMaterialToggleChanged(materialIndex, isOn));

            Debug.Log($"[ObstacleMaterialController] Subscribed to '{binding.materialDefinition.materialName}' toggle");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from all toggle events to prevent memory leaks
        if (materialBindings == null) return;

        for (int i = 0; i < materialBindings.Length; i++)
        {
            MaterialUIBinding binding = materialBindings[i];
            if (binding != null && binding.toggle != null)
            {
                binding.toggle.onValueChanged.RemoveAllListeners();
            }
        }
    }

    // ???????????????????????????????????????????????????????????????
    // PUBLIC API - Called by ObstacleManager
    // ???????????????????????????????????????????????????????????????

    /// <summary>
    /// Updates the list of reflectors to match the currently active custom column.
    /// Called by ObstacleManager whenever custom column selection changes (6-8).
    /// 
    /// IMPORTANT: Clears any active material! User must re-select material after switching columns.
    /// </summary>
    /// <param name="customColumnObject">The active custom column GameObject, or null if none</param>
    public void UpdateReflectorList(GameObject customColumnObject)
    {
        obstacleReflectors.Clear();
        activeMaterialIndex = -1;

        if (customColumnObject == null)
        {
            Debug.Log("[ObstacleMaterialController] No custom column active. Clearing reflector list.");
            ClearAllIndicators();
            return;
        }

        // Find all AkSurfaceReflector components in the active custom column
        obstacleReflectors.AddRange(customColumnObject.GetComponentsInChildren<AkSurfaceReflector>(true));

        Debug.Log($"[ObstacleMaterialController] Found {obstacleReflectors.Count} reflectors in '{customColumnObject.name}'");

        // Reset material indicators (user must re-select material for this column)
        ClearAllIndicators();
    }

    /// <summary>
    /// Clears all reflectors and resets UI state.
    /// Called by ObstacleManager when switching to baked presets (0-5) or "None" (-1).
    /// </summary>
    public void ClearReflectors()
    {
        obstacleReflectors.Clear();
        activeMaterialIndex = -1;
        ClearAllIndicators();
        Debug.Log("[ObstacleMaterialController] Reflectors cleared.");
    }

    // ???????????????????????????????????????????????????????????????
    // PUBLIC API - Material Application (can be called externally)
    // ???????????????????????????????????????????????????????????????

    /// <summary>
    /// Applies a specific material by name.
    /// Useful for external scripts or UI events.
    /// </summary>
    /// <param name="materialName">Name of the material (e.g., "Carpet", "Concrete")</param>
    public void ApplyMaterialByName(string materialName)
    {
        for (int i = 0; i < materialBindings.Length; i++)
        {
            if (materialBindings[i] != null &&
                materialBindings[i].materialDefinition != null &&
                materialBindings[i].materialDefinition.materialName == materialName)
            {
                ApplyMaterial(i);
                return;
            }
        }

        Debug.LogWarning($"[ObstacleMaterialController] Material '{materialName}' not found!");
    }

    /// <summary>
    /// Applies a specific material by index.
    /// </summary>
    /// <param name="materialIndex">Index into the materialBindings array</param>
    public void ApplyMaterial(int materialIndex)
    {
        if (materialIndex < 0 || materialIndex >= materialBindings.Length)
        {
            Debug.LogError($"[ObstacleMaterialController] Invalid material index: {materialIndex}");
            return;
        }

        if (obstacleReflectors.Count == 0)
        {
            Debug.LogWarning("[ObstacleMaterialController] No reflectors to update. Is a custom column (6-8) active?");
            return;
        }

        MaterialUIBinding binding = materialBindings[materialIndex];

        if (binding == null || binding.materialDefinition == null || binding.materialDefinition.acousticTexture == null)
        {
            Debug.LogError($"[ObstacleMaterialController] Material binding at index {materialIndex} is invalid!");
            return;
        }

        // Apply acoustic texture to all reflectors
        foreach (AkSurfaceReflector reflector in obstacleReflectors)
        {
            if (reflector != null)
            {
                reflector.AcousticTexture = binding.materialDefinition.acousticTexture;
            }
        }

        // Update active material tracking
        activeMaterialIndex = materialIndex;

        // Update visual indicators (only this material should be active)
        UpdateIndicators(materialIndex);

        Debug.Log($"[ObstacleMaterialController] Applied '{binding.materialDefinition.materialName}' to {obstacleReflectors.Count} reflectors");
    }

    // ???????????????????????????????????????????????????????????????
    // PRIVATE METHODS - Toggle Event Handlers
    // ???????????????????????????????????????????????????????????????

    /// <summary>
    /// Unified event handler for ALL material toggles.
    /// Called when any material toggle's value changes.
    /// </summary>
    /// <param name="materialIndex">Index of the material whose toggle changed</param>
    /// <param name="isOn">True if toggle was turned ON, false if turned OFF</param>
    private void OnMaterialToggleChanged(int materialIndex, bool isOn)
    {
        // Ignore programmatic toggle changes (prevent recursion)
        if (isUpdatingToggles) return;

        // Only process when toggle is turned ON (prevents double-triggers in toggle groups)
        if (!isOn) return;

        Debug.Log($"[ObstacleMaterialController] Material toggle changed: {materialBindings[materialIndex].materialDefinition.materialName}");

        // Apply the selected material
        ApplyMaterial(materialIndex);
    }

    // ???????????????????????????????????????????????????????????????
    // PRIVATE METHODS - UI State Management
    // ???????????????????????????????????????????????????????????????

    /// <summary>
    /// Updates visual indicators to show only the active material.
    /// Also synchronizes toggle states.
    /// </summary>
    /// <param name="activeIndex">Index of the material that should be active</param>
    private void UpdateIndicators(int activeIndex)
    {
        isUpdatingToggles = true;

        for (int i = 0; i < materialBindings.Length; i++)
        {
            MaterialUIBinding binding = materialBindings[i];
            if (binding == null) continue;

            bool isActive = (i == activeIndex);

            // Update visual indicator
            if (binding.visualIndicator != null)
            {
                binding.visualIndicator.SetActive(isActive);
            }

            // Synchronize toggle state (only if different)
            if (binding.toggle != null && binding.toggle.isOn != isActive)
            {
                binding.toggle.isOn = isActive;
            }
        }

        isUpdatingToggles = false;
    }

    /// <summary>
    /// Clears all material indicators.
    /// Called when no custom column is active or when switching columns.
    /// </summary>
    private void ClearAllIndicators()
    {
        UpdateIndicators(-1); // -1 = no material active
    }

    // ???????????????????????????????????????????????????????????????
    // DEBUG HELPERS
    // ???????????????????????????????????????????????????????????????

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (materialBindings == null || materialBindings.Length == 0)
        {
            Debug.LogWarning($"[ObstacleMaterialController] '{gameObject.name}': No material bindings assigned!");
            return;
        }

        // Validate each material binding
        for (int i = 0; i < materialBindings.Length; i++)
        {
            MaterialUIBinding binding = materialBindings[i];

            if (binding == null || binding.materialDefinition == null)
            {
                Debug.LogWarning($"[ObstacleMaterialController] Binding at index {i} is null or has no material definition!");
                continue;
            }

            if (binding.materialDefinition.acousticTexture == null)
            {
                Debug.LogWarning($"[ObstacleMaterialController] Material '{binding.materialDefinition.materialName}' has no acousticTexture assigned!");
            }

            if (binding.toggle == null)
            {
                Debug.LogWarning($"[ObstacleMaterialController] Material '{binding.materialDefinition.materialName}' has no toggle assigned!");
            }
        }
    }

    [ContextMenu("Log Current State")]
    private void LogCurrentState()
    {
        Debug.Log("=== MATERIAL CONTROLLER STATE ===");
        Debug.Log($"Active Material Index: {activeMaterialIndex}");
        Debug.Log($"Active Material: {(activeMaterialIndex >= 0 && activeMaterialIndex < materialBindings.Length ? materialBindings[activeMaterialIndex].materialDefinition.materialName : "None")}");
        Debug.Log($"Reflector Count: {obstacleReflectors.Count}");
        Debug.Log($"Total Materials Configured: {materialBindings.Length}");
        Debug.Log("=================================");
    }
#endif
}