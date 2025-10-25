using UnityEngine;

public class ObstacleToggleHelper : MonoBehaviour
{
    // 1. Drag your ObstacleManager GameObject here
    public ObstacleManager obstacleManager;

    // 2. Set these values in the Inspector for *each* toggle
    public int obstacleIndex;
    public int soundIndex;

    // 3. This is the function you will hook up in the Inspector
    public void OnToggleSelected(bool isOn)
    {
        // 4. Only fire when the toggle is turned ON
        // This prevents double-triggers [1, 2, 3]
        if (isOn)
        {
            if (obstacleManager != null)
            {
                obstacleManager.SelectLayout(obstacleIndex, soundIndex);
            }
        }
    }
}