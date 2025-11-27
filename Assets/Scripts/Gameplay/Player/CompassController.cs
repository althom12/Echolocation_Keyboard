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
        // Mathf.DeltaAngle calculates the difference. 
        // Mathf.Abs makes sure we don't care if it's +5 or -5 degrees off.

        if (Mathf.Abs(Mathf.DeltaAngle(yaw, 0)) < angleTolerance) return CompassPoint.North;
        if (Mathf.Abs(Mathf.DeltaAngle(yaw, 90)) < angleTolerance) return CompassPoint.East;
        if (Mathf.Abs(Mathf.DeltaAngle(yaw, 180)) < angleTolerance) return CompassPoint.South;
        if (Mathf.Abs(Mathf.DeltaAngle(yaw, 270)) < angleTolerance) return CompassPoint.West;

        if (Mathf.Abs(Mathf.DeltaAngle(yaw, 45)) < angleTolerance) return CompassPoint.NorthEast;
        if (Mathf.Abs(Mathf.DeltaAngle(yaw, 135)) < angleTolerance) return CompassPoint.SouthEast;
        if (Mathf.Abs(Mathf.DeltaAngle(yaw, 225)) < angleTolerance) return CompassPoint.SouthWest;
        if (Mathf.Abs(Mathf.DeltaAngle(yaw, 315)) < angleTolerance) return CompassPoint.NorthWest;

        return CompassPoint.None;
    }
}