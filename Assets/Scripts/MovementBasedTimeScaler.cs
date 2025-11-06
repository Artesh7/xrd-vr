using UnityEngine;

/*
MovementBasedTimeScaler
-----------------------
Superhot-style time control for VR: time advances based on your movement.
Attach this component to any always-enabled GameObject (e.g., a Gameplay Systems object).

Inspector setup:
- Assign your Head (HMD camera transform), Left/Right hand controller transforms.
- Optionally assign Body/Origin (e.g., XROrigin transform) to include locomotion.
- Tweak weights, thresholds, and mapping curve.

Notes:
- Uses unscaled delta time for measurement to remain stable while timeScale changes.
- Automatically scales Time.fixedDeltaTime with timeScale for consistent physics.
- Ignores teleport-like jumps beyond thresholds to avoid spikes.
- If other scripts modify Time.timeScale concurrently, results may conflict.
*/

[DefaultExecutionOrder(-1000)]
public class MovementBasedTimeScaler : MonoBehaviour
{
    [Header("Tracked Transforms")]
    [Tooltip("Head/HMD camera transform (e.g., XROrigin.Camera.transform)")]
    public Transform head;

    [Tooltip("Left hand/controller transform")]
    public Transform leftHand;

    [Tooltip("Right hand/controller transform")]
    public Transform rightHand;

    [Tooltip("Optional: Body/XR Origin transform to include rig locomotion")]
    public Transform body;

    [Header("Weights (contribution to movement speed)")]
    [Tooltip("Weight for head linear motion (m/s)")]
    public float headLinearWeight = 1f;

    [Tooltip("Weight for hand linear motion (m/s) per hand")]
    public float handLinearWeight = 1f;

    [Tooltip("Weight for body linear motion (m/s)")]
    public float bodyLinearWeight = 0.75f;

    [Space]
    [Tooltip("Convert rotational speed (deg/s) to an equivalent linear contribution (m/s per deg/s)")]
    public float rotationToLinearScale = 0.01f; // m/s per deg/s

    [Tooltip("Additional multiplier for head rotation contribution")]
    public float headRotationWeight = 1.0f;

    [Tooltip("Additional multiplier for hand rotation contribution")]
    public float handRotationWeight = 0.5f;

    [Tooltip("Additional multiplier for body rotation contribution")]
    public float bodyRotationWeight = 0.5f;

    [Header("Velocity Smoothing")]
    [Tooltip("Exponential smoothing time (seconds). 0 = no smoothing")]
    [Min(0f)]
    public float smoothingTime = 0.1f;

    [Header("Teleport/Spike Rejection")]
    [Tooltip("If linear displacement/frame exceeds this (meters), ignore that transform's linear.")]
    [Min(0f)]
    public float teleportLinearThreshold = 0.5f;

    [Tooltip("If rotation delta/frame exceeds this (degrees), ignore that transform's rotation.")]
    [Min(0f)]
    public float teleportAngularThreshold = 120f;

    [Header("Mapping to Time Scale")]
    [Tooltip("Minimum time scale when perfectly still")]
    [Range(0f, 1f)]
    public float minTimeScale = 0.02f;

    [Tooltip("Combined movement (m/s equivalent) required to reach full speed (timeScale=1)")]
    [Min(0.01f)]
    public float speedForFullTime = 1.5f;

    [Tooltip("Response curve applied to normalized speed (0..1)")]
    public AnimationCurve responseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Clamp the combined movement to this value before normalization")]
    [Min(0.01f)]
    public float maxCombinedSpeed = 5f;

    [Header("Runtime")]
    [Tooltip("Optional: Freeze time when any menu is open by setting this externally.")]
    public bool externallyPaused = false;

    [Tooltip("Show debug values in the Game view and draw gizmos in Scene view")]
    public bool debug = false;

    // Internal tracking
    private Vector3 _headPrevPos, _leftPrevPos, _rightPrevPos, _bodyPrevPos;
    private Quaternion _headPrevRot, _leftPrevRot, _rightPrevRot, _bodyPrevRot;
    private bool _inited;

    private float _smoothedCombined; // smoothed combined movement (m/s eq.)
    private float _baseFixedDeltaTime;

    public float CurrentCombinedMovement => _smoothedCombined;

    void Awake()
    {
        _baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    void OnEnable()
    {
        InitializeSamples();
    }

    void InitializeSamples()
    {
        if (head != null)
        {
            _headPrevPos = head.position;
            _headPrevRot = head.rotation;
        }
        if (leftHand != null)
        {
            _leftPrevPos = leftHand.position;
            _leftPrevRot = leftHand.rotation;
        }
        if (rightHand != null)
        {
            _rightPrevPos = rightHand.position;
            _rightPrevRot = rightHand.rotation;
        }
        if (body != null)
        {
            _bodyPrevPos = body.position;
            _bodyPrevRot = body.rotation;
        }
        _inited = true;
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        if (!_inited) InitializeSamples();

        var dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        float combined = 0f;

        // HEAD
        if (head != null)
        {
            float linear = ComputeLinearSpeed(head.position, ref _headPrevPos, dt, teleportLinearThreshold);
            float angular = ComputeAngularSpeed(head.rotation, ref _headPrevRot, dt, teleportAngularThreshold);
            combined += headLinearWeight * linear;
            combined += headRotationWeight * (angular * rotationToLinearScale);
        }

        // HANDS
        if (leftHand != null)
        {
            float linear = ComputeLinearSpeed(leftHand.position, ref _leftPrevPos, dt, teleportLinearThreshold);
            float angular = ComputeAngularSpeed(leftHand.rotation, ref _leftPrevRot, dt, teleportAngularThreshold);
            combined += handLinearWeight * linear;
            combined += handRotationWeight * (angular * rotationToLinearScale);
        }
        if (rightHand != null)
        {
            float linear = ComputeLinearSpeed(rightHand.position, ref _rightPrevPos, dt, teleportLinearThreshold);
            float angular = ComputeAngularSpeed(rightHand.rotation, ref _rightPrevRot, dt, teleportAngularThreshold);
            combined += handLinearWeight * linear;
            combined += handRotationWeight * (angular * rotationToLinearScale);
        }

        // BODY
        if (body != null)
        {
            float linear = ComputeLinearSpeed(body.position, ref _bodyPrevPos, dt, teleportLinearThreshold);
            float angular = ComputeAngularSpeed(body.rotation, ref _bodyPrevRot, dt, teleportAngularThreshold);
            combined += bodyLinearWeight * linear;
            combined += bodyRotationWeight * (angular * rotationToLinearScale);
        }

        // Clamp
        combined = Mathf.Min(combined, maxCombinedSpeed);

        // Smoothing (exponential toward current)
        if (smoothingTime > 0f)
        {
            float k = 1f - Mathf.Exp(-dt / Mathf.Max(1e-4f, smoothingTime));
            _smoothedCombined = Mathf.Lerp(_smoothedCombined, combined, k);
        }
        else
        {
            _smoothedCombined = combined;
        }

        // Map to timeScale
        float norm = Mathf.InverseLerp(0f, Mathf.Max(0.001f, speedForFullTime), _smoothedCombined);
        float curve = Mathf.Clamp01(responseCurve != null ? responseCurve.Evaluate(norm) : norm);
        float targetScale = externallyPaused ? 0f : Mathf.Lerp(minTimeScale, 1f, curve);

        // Apply
        Time.timeScale = targetScale;
        Time.fixedDeltaTime = _baseFixedDeltaTime * Mathf.Max(0.0001f, targetScale);
    }

    float ComputeLinearSpeed(Vector3 current, ref Vector3 prev, float dt, float teleThreshold)
    {
        float dist = Vector3.Distance(current, prev);
        prev = current;
        if (dist > teleThreshold) return 0f; // treat as teleport; ignore linear this frame
        return dist / dt;
    }

    float ComputeAngularSpeed(Quaternion current, ref Quaternion prev, float dt, float teleDegThreshold)
    {
        float angle;
        Quaternion delta = current * Quaternion.Inverse(prev);
        delta.ToAngleAxis(out angle, out _);
        // AngleAxis returns 0..360; convert to the smallest
        if (angle > 180f) angle = 360f - angle;
        prev = current;
        if (angle > teleDegThreshold) return 0f; // spike rejection
        return angle / dt; // deg/s
    }

    void OnDisable()
    {
        // Restore physics step to base when disabled
        Time.fixedDeltaTime = _baseFixedDeltaTime;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!debug) return;
        Gizmos.color = Color.cyan;
        if (head != null) Gizmos.DrawWireSphere(head.position, 0.03f);
        Gizmos.color = Color.yellow;
        if (leftHand != null) Gizmos.DrawWireSphere(leftHand.position, 0.03f);
        if (rightHand != null) Gizmos.DrawWireSphere(rightHand.position, 0.03f);
        Gizmos.color = Color.magenta;
        if (body != null) Gizmos.DrawWireSphere(body.position, 0.04f);

        // Display values in Scene view
        var pos = transform.position + Vector3.up * 0.2f;
        UnityEditor.Handles.Label(pos, $"Combined: {_smoothedCombined:F2} m/s eq.\nTimeScale: {Time.timeScale:F2}");
    }
#endif
}
