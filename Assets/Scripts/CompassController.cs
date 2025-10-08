using UnityEngine;

public class CompassController : MonoBehaviour
{
    // --- Public Wwise Events ---
    public AK.Wwise.Event PlayPulseEvent;
    public AK.Wwise.Event StopPulseEvent;
    public AK.Wwise.Event PlayNorthEvent;
    public AK.Wwise.Event PlayEastEvent;
    public AK.Wwise.Event PlaySouthEvent;
    public AK.Wwise.Event PlayWestEvent;
    public AK.Wwise.Event PlayPingEvent; // --- NEW: Add the ping event here

    // --- Public Settings ---
    public float angleTolerance = 5.0f;

    // --- Private Variables ---
    private string playerYawRTPC = "Player_Yaw";
    private bool isCompassActive = false;

    // --- MODIFIED: Expanded enum to include all 8 compass points ---
    private enum CompassPoint { None, North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest };
    private CompassPoint lastPoint = CompassPoint.None;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isCompassActive = true;
            PlayPulseEvent.Post(gameObject);
        }

        if (Input.GetMouseButtonUp(1))
        {
            isCompassActive = false;
            StopPulseEvent.Post(gameObject);
            lastPoint = CompassPoint.None; // --- MODIFIED: Reset the lastPoint
        }

        if (isCompassActive)
        {
            float currentYaw = transform.eulerAngles.y;
            AkSoundEngine.SetRTPCValue(playerYawRTPC, currentYaw, gameObject);

            // --- MODIFIED: This whole block is new and improved ---
            // Determine the current compass point (could be cardinal or intermediate).
            CompassPoint currentPoint = GetCompassPoint(currentYaw);

            // If we have entered a NEW point zone...
            if (currentPoint != lastPoint)
            {
                // ...play the appropriate sound.
                switch (currentPoint)
                {
                    // Cardinal Directions
                    case CompassPoint.North:
                        PlayNorthEvent.Post(gameObject);
                        break;
                    case CompassPoint.East:
                        PlayEastEvent.Post(gameObject);
                        break;
                    case CompassPoint.South:
                        PlaySouthEvent.Post(gameObject);
                        break;
                    case CompassPoint.West:
                        PlayWestEvent.Post(gameObject);
                        break;

                    // Intermediate "Ping" Directions
                    case CompassPoint.NorthEast:
                    case CompassPoint.SouthEast:
                    case CompassPoint.SouthWest:
                    case CompassPoint.NorthWest:
                        PlayPingEvent.Post(gameObject);
                        break;
                }
            }

            // Remember this frame's point for the next frame.
            lastPoint = currentPoint;
        }
    }

    // --- MODIFIED: This function now checks for all 8 directions ---
    private CompassPoint GetCompassPoint(float yaw)
    {
        // Check Cardinal directions FIRST, as they take priority.
        if (yaw <= angleTolerance || yaw >= 360 - angleTolerance) return CompassPoint.North;
        if (yaw >= 90 - angleTolerance && yaw <= 90 + angleTolerance) return CompassPoint.East;
        if (yaw >= 180 - angleTolerance && yaw <= 180 + angleTolerance) return CompassPoint.South;
        if (yaw >= 270 - angleTolerance && yaw <= 270 + angleTolerance) return CompassPoint.West;

        // THEN, check the intermediate "ping" angles.
        if (yaw >= 45 - angleTolerance && yaw <= 45 + angleTolerance) return CompassPoint.NorthEast;
        if (yaw >= 135 - angleTolerance && yaw <= 135 + angleTolerance) return CompassPoint.SouthEast;
        if (yaw >= 225 - angleTolerance && yaw <= 225 + angleTolerance) return CompassPoint.SouthWest;
        if (yaw >= 315 - angleTolerance && yaw <= 315 + angleTolerance) return CompassPoint.NorthWest;

        // If not in any specific zone, return None.
        return CompassPoint.None;
    }
}