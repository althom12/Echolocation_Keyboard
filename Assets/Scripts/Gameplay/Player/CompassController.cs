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
    public AK.Wwise.Event PlayPingEvent;

    // --- Public Settings ---
    public float angleTolerance = 5.0f;

    // --- Private Variables ---
    private string playerYawRTPC = "Player_Yaw";
    private bool isCompassActive = false;

    private enum CompassPoint { None, North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest };
    private CompassPoint lastPoint = CompassPoint.None;

    void Update()
    {
        // --- MODIFIED: This block now handles toggling ---
        if (Input.GetMouseButtonDown(1))
        {
            // Invert the isCompassActive state
            isCompassActive = !isCompassActive;

            if (isCompassActive)
            {
                // If the compass is now ON, play the start event
                PlayPulseEvent.Post(gameObject);
            }
            else
            {
                // If the compass is now OFF, play the stop event and reset the last point
                StopPulseEvent.Post(gameObject);
                lastPoint = CompassPoint.None;
            }
        }

        // --- This logic remains the same ---
        if (isCompassActive)
        {
            float currentYaw = transform.eulerAngles.y;
            AkSoundEngine.SetRTPCValue(playerYawRTPC, currentYaw, gameObject);

            CompassPoint currentPoint = GetCompassPoint(currentYaw);

            if (currentPoint != lastPoint)
            {
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
            lastPoint = currentPoint;
        }
    }

    private CompassPoint GetCompassPoint(float yaw)
    {
        if (yaw <= angleTolerance || yaw >= 360 - angleTolerance) return CompassPoint.North;
        if (yaw >= 90 - angleTolerance && yaw <= 90 + angleTolerance) return CompassPoint.East;
        if (yaw >= 180 - angleTolerance && yaw <= 180 + angleTolerance) return CompassPoint.South;
        if (yaw >= 270 - angleTolerance && yaw <= 270 + angleTolerance) return CompassPoint.West;
        if (yaw >= 45 - angleTolerance && yaw <= 45 + angleTolerance) return CompassPoint.NorthEast;
        if (yaw >= 135 - angleTolerance && yaw <= 135 + angleTolerance) return CompassPoint.SouthEast;
        if (yaw >= 225 - angleTolerance && yaw <= 225 + angleTolerance) return CompassPoint.SouthWest;
        if (yaw >= 315 - angleTolerance && yaw <= 315 + angleTolerance) return CompassPoint.NorthWest;

        return CompassPoint.None;
    }
}