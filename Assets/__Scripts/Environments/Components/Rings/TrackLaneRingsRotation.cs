using UnityEngine;

// Serialized environment settings retained by the deterministic rotation evaluator.
// The former live active-effect loop duplicated the snapshot simulation and could not
// reproduce pause, rewind, or overlapping propagation consistently.
public class TrackLaneRingsRotation : MonoBehaviour
{
    public TrackLaneRingsManager Manager;
    public float StartupRotationAngle;
    public float StartupRotationStep;
    public int StartupRotationPropagationSpeed;
    public float StartupRotationFlexySpeed;
    public bool CounterSpin;
}
