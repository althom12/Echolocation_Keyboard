/// <summary>
/// Obstacle Service Interface
/// 
/// Defines the public contract for obstacle management systems.
/// This interface is currently NOT IMPLEMENTED but is provided for future decoupling.
/// 
/// PURPOSE:
/// In future refactoring phases, scripts that depend on ObstacleManager can instead
/// reference IObstacleService. This allows:
/// - Easier unit testing (mock implementations)
/// - Decoupling between systems
/// - Flexibility to swap implementations
/// 
/// FUTURE USAGE EXAMPLE:
/// Instead of:
///   public ObstacleManager obstacleManager; // Direct dependency
/// 
/// Use:
///   public IObstacleService obstacleService; // Interface dependency
/// 
/// Then ObstacleManager implements this interface:
///   public class ObstacleManager : MonoBehaviour, IObstacleService
/// 
/// IMPLEMENTATION STATUS:
/// ? ObstacleManager does NOT implement this yet
/// ? No scripts use this interface yet
/// ? Defined for Phase 3+ refactoring
/// 
/// NOTE: This file can be safely ignored for Phase 2A. It's here for documentation
/// and to establish the pattern for later phases.
/// </summary>
public interface IObstacleService
{
    /// <summary>
    /// Selects an obstacle layout by index.
    /// </summary>
    /// <param name="obstacleIndex">Index of the layout to select (0-12)</param>
    void SelectLayout(int obstacleIndex);

    /// <summary>
    /// Checks if a preset (1-6) is currently active.
    /// </summary>
    /// <returns>True if a preset is active, false otherwise</returns>
    bool IsPresetActive();

    /// <summary>
    /// Checks if a custom column (7-10) is currently active.
    /// </summary>
    /// <returns>True if a custom column is active, false otherwise</returns>
    bool IsCustomColumnActive();

    /// <summary>
    /// Gets the index of the currently active obstacle set.
    /// </summary>
    /// <returns>Index of active set, or -1 if none active</returns>
    int GetActiveIndex();
}
