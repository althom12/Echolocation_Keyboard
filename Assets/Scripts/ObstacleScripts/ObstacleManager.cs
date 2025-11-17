using UnityEngine;
using AK.Wwise;

/// <summary>
/// Obstacle Manager - Refactored for Single Responsibility
/// 
/// Manages obstacle layout selection and routing logic.
/// Material management has been extracted to ObstacleMaterialController.
/// 
/// OBSTACLE SYSTEM ARCHITECTURE:
/// 
/// Indices 0-6: Baked Presets (mutually exclusive)
///   0  = None/Default (clears all obstacles)
///   1-6 = Fixed presets with baked materials
/// 
/// Indices 7-10: Custom Columns (mutually exclusive among themselves)
///   7-10 = Column layouts that can accept material assignments
/// 
/// Indices 11-12: Material Selectors (apply to active custom column only)
///   11 = Carpet material
///   12 = Concrete material
/// 
/// SELECTION RULES:
/// - Selecting any preset (1-6) deactivates all other presets + all custom columns
/// - Selecting any custom column (7-10) deactivates all presets + other custom columns
/// - Materials (11-12) only apply to the currently active custom column
/// - Selecting "None" (0) deactivates everything
/// - Materials do NOT persist when switching columns (reset to null)
/// 
/// RESPONSIBILITIES:
/// - Activate/deactivate obstacle GameObjects
/// - Route material requests to ObstacleMaterialController
/// - Provide public API for UI toggles (via ObstacleToggleHelper)
/// 
/// DOES NOT:
/// - Manage materials directly (delegated to ObstacleMaterialController)
/// - Handle UI toggle listeners (handled by MaterialController)
/// - Track reflector components (handled by MaterialController)
/// </summary>
public class ObstacleManager : MonoBehaviour
{
    // ???????????????????????????????????????????????????????????
    // INSPECTOR FIELDS
    // ???????????????????????????????????????????????????????????

    [Header("Obstacle Layouts")]
    [Tooltip("Array of obstacle set GameObjects. Indices 0-6 = Presets, 7-10 = Custom Columns")]
    public GameObject[] obstacleSets;

    [Header("Material Controller")]
    [Tooltip("Reference to the ObstacleMaterialController component (should be on this same GameObject)")]
    public ObstacleMaterialController materialController;

    // ???????????????????????????????????????????????????????????
    // CONSTANTS - Index Ranges
    // ???????????????????????????????????????????????????????????

    private const int INDEX_NONE = 0;
    private const int PRESET_START = 1;
    private const int PRESET_END = 6;
    private const int CUSTOM_COLUMN_START = 7;
    private const int CUSTOM_COLUMN_END = 10;
    private const int MATERIAL_CARPET = 11;
    private const int MATERIAL_CONCRETE = 12;

    // ???????????????????????????????????????????????????????????
    // PRIVATE FIELDS
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Tracks the currently active preset index (1-6), or -1 if none.
    /// </summary>
    private int activePresetIndex = -1;

    /// <summary>
    /// Tracks the currently active custom column index (7-10), or -1 if none.
    /// </summary>
    private int activeCustomColumnIndex = -1;

    // ???????????????????????????????????????????????????????????
    // UNITY LIFECYCLE
    // ???????????????????????????????????????????????????????????

    void Start()
    {
        // Validate critical references
        if (materialController == null)
        {
            Debug.LogError("[ObstacleManager] materialController is not assigned! Material switching will not work.");
        }

        if (obstacleSets == null || obstacleSets.Length < 11)
        {
            Debug.LogError("[ObstacleManager] obstacleSets array is not properly configured! Expected at least 11 elements.");
            this.enabled = false;
            return;
        }

        // Initialize to "None" state (index 0)
        SelectLayout(INDEX_NONE);
    }

    // ???????????????????????????????????????????????????????????
    // PUBLIC API - Main Entry Point (called by ObstacleToggleHelper)
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Main routing function called by ObstacleToggleHelper.
    /// Determines the category of the selection and routes to appropriate handler.
    /// 
    /// NOTE: soundIndex parameter is DEPRECATED (leftover from removed activationSounds).
    /// It's kept for backward compatibility but is not used.
    /// </summary>
    /// <param name="obstacleIndex">The index from the UI toggle (0-12)</param>
    /// <param name="soundIndex">DEPRECATED - Not used, will be removed in future</param>
    public void SelectLayout(int obstacleIndex, int soundIndex = 0)
    {
        // Validate index range
        if (obstacleIndex < 0 || obstacleIndex >= obstacleSets.Length)
        {
            Debug.LogError($"[ObstacleManager] Invalid obstacleIndex: {obstacleIndex}. Must be 0-{obstacleSets.Length - 1}");
            return;
        }

        Debug.Log($"[ObstacleManager] SelectLayout called with index: {obstacleIndex}");

        // Route based on index range
        if (obstacleIndex == INDEX_NONE)
        {
            SelectNone();
        }
        else if (obstacleIndex >= PRESET_START && obstacleIndex <= PRESET_END)
        {
            SelectPreset(obstacleIndex);
        }
        else if (obstacleIndex >= CUSTOM_COLUMN_START && obstacleIndex <= CUSTOM_COLUMN_END)
        {
            SelectCustomColumn(obstacleIndex);
        }
        else if (obstacleIndex == MATERIAL_CARPET || obstacleIndex == MATERIAL_CONCRETE)
        {
            ApplyMaterial(obstacleIndex);
        }
        else
        {
            Debug.LogWarning($"[ObstacleManager] Index {obstacleIndex} is out of expected ranges. No action taken.");
        }
    }

    /// <summary>
    /// Overload for backward compatibility (no soundIndex parameter).
    /// </summary>
    public void SelectLayout(int obstacleIndex)
    {
        SelectLayout(obstacleIndex, 0);
    }

    // ???????????????????????????????????????????????????????????
    // PRIVATE METHODS - Category Handlers
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Handles selection of "None" (index 0).
    /// Deactivates all obstacles and clears material controller state.
    /// </summary>
    private void SelectNone()
    {
        Debug.Log("[ObstacleManager] Selecting 'None' - clearing all obstacles");

        // Deactivate all obstacle sets
        DeactivateAllObstacles();

        // Clear material controller
        if (materialController != null)
        {
            materialController.ClearReflectors();
        }

        // Reset tracking
        activePresetIndex = -1;
        activeCustomColumnIndex = -1;
    }

    /// <summary>
    /// Handles selection of baked presets (indices 1-6).
    /// Deactivates all other presets and all custom columns.
    /// Materials do not apply to presets (they have fixed materials).
    /// </summary>
    private void SelectPreset(int presetIndex)
    {
        Debug.Log($"[ObstacleManager] Selecting Preset at index {presetIndex}");

        // Deactivate all obstacles
        DeactivateAllObstacles();

        // Activate the selected preset
        if (obstacleSets[presetIndex] != null)
        {
            obstacleSets[presetIndex].SetActive(true);
            Debug.Log($"[ObstacleManager] Activated preset: {obstacleSets[presetIndex].name}");
        }
        else
        {
            Debug.LogWarning($"[ObstacleManager] Preset at index {presetIndex} is null!");
        }

        // Update tracking
        activePresetIndex = presetIndex;
        activeCustomColumnIndex = -1;

        // Clear material controller (presets don't use it)
        if (materialController != null)
        {
            materialController.ClearReflectors();
        }
    }

    /// <summary>
    /// Handles selection of custom columns (indices 7-10).
    /// Deactivates all presets and other custom columns.
    /// Updates material controller with the new column's reflectors.
    /// 
    /// IMPORTANT: Materials are NOT remembered when switching columns.
    /// User must re-select Carpet or Concrete after switching.
    /// </summary>
    private void SelectCustomColumn(int columnIndex)
    {
        Debug.Log($"[ObstacleManager] Selecting Custom Column at index {columnIndex}");

        // Deactivate all obstacles
        DeactivateAllObstacles();

        // Activate the selected custom column
        GameObject selectedColumn = null;
        if (obstacleSets[columnIndex] != null)
        {
            obstacleSets[columnIndex].SetActive(true);
            selectedColumn = obstacleSets[columnIndex];
            Debug.Log($"[ObstacleManager] Activated custom column: {selectedColumn.name}");
        }
        else
        {
            Debug.LogWarning($"[ObstacleManager] Custom column at index {columnIndex} is null!");
        }

        // Update tracking
        activePresetIndex = -1;
        activeCustomColumnIndex = columnIndex;

        // Update material controller with new reflector list
        if (materialController != null)
        {
            materialController.UpdateReflectorList(selectedColumn);
        }
        else
        {
            Debug.LogWarning("[ObstacleManager] materialController is null! Cannot update reflector list.");
        }
    }

    /// <summary>
    /// Handles material selection (indices 11-12).
    /// Only applies if a custom column (7-10) is currently active.
    /// If a preset or "None" is active, materials do nothing (silent ignore per requirements).
    /// </summary>
    private void ApplyMaterial(int materialIndex)
    {
        // Check if a custom column is active
        if (activeCustomColumnIndex == -1)
        {
            Debug.Log("[ObstacleManager] Material selected but no custom column is active. Ignoring (per design).");
            return;
        }

        if (materialController == null)
        {
            Debug.LogError("[ObstacleManager] Cannot apply material - materialController is null!");
            return;
        }

        // Route to appropriate material method
        if (materialIndex == MATERIAL_CARPET)
        {
            Debug.Log("[ObstacleManager] Applying Carpet material to active custom column");
            materialController.ApplyCarpet();
        }
        else if (materialIndex == MATERIAL_CONCRETE)
        {
            Debug.Log("[ObstacleManager] Applying Concrete material to active custom column");
            materialController.ApplyConcrete();
        }
    }

    /// <summary>
    /// Utility method to deactivate all obstacle GameObjects.
    /// Called before activating a new selection to ensure clean state.
    /// </summary>
    private void DeactivateAllObstacles()
    {
        for (int i = 0; i < obstacleSets.Length; i++)
        {
            if (obstacleSets[i] != null)
            {
                obstacleSets[i].SetActive(false);
            }
        }
    }

    // ???????????????????????????????????????????????????????????
    // PUBLIC API - Query Methods (for external scripts)
    // ???????????????????????????????????????????????????????????

    /// <summary>
    /// Checks if a preset (1-6) is currently active.
    /// </summary>
    public bool IsPresetActive()
    {
        return activePresetIndex >= PRESET_START && activePresetIndex <= PRESET_END;
    }

    /// <summary>
    /// Checks if a custom column (7-10) is currently active.
    /// </summary>
    public bool IsCustomColumnActive()
    {
        return activeCustomColumnIndex >= CUSTOM_COLUMN_START && activeCustomColumnIndex <= CUSTOM_COLUMN_END;
    }

    /// <summary>
    /// Gets the index of the currently active obstacle set.
    /// Returns -1 if "None" or nothing is active.
    /// </summary>
    public int GetActiveIndex()
    {
        if (activePresetIndex != -1)
            return activePresetIndex;
        if (activeCustomColumnIndex != -1)
            return activeCustomColumnIndex;
        return -1;
    }

    // ???????????????????????????????????????????????????????????
    // DEBUG HELPERS
    // ???????????????????????????????????????????????????????????

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Validation warnings in Inspector
        if (obstacleSets == null || obstacleSets.Length == 0)
        {
            Debug.LogWarning($"[ObstacleManager] '{gameObject.name}': obstacleSets array is empty!");
        }

        if (materialController == null)
        {
            Debug.LogWarning($"[ObstacleManager] '{gameObject.name}': materialController is not assigned!");
        }
    }

    /// <summary>
    /// Debug context menu to log current state.
    /// Right-click on component in Inspector ? "Log Current State"
    /// </summary>
    [ContextMenu("Log Current State")]
    private void LogCurrentState()
    {
        Debug.Log("=== OBSTACLE MANAGER STATE ===");
        Debug.Log($"Active Preset Index: {activePresetIndex}");
        Debug.Log($"Active Custom Column Index: {activeCustomColumnIndex}");
        Debug.Log($"Is Preset Active: {IsPresetActive()}");
        Debug.Log($"Is Custom Column Active: {IsCustomColumnActive()}");
        Debug.Log($"Active Index: {GetActiveIndex()}");
        Debug.Log("==============================");
    }
#endif
}