using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class GunController : MonoBehaviour
{
    [Header("Refs")]
    public Transform muzzle;
    public GameObject projectilePrefab;

    [Header("Gameplay")]
    public float projectileSpeed = 30f;
    public float fireCooldown = 0.15f;
    public bool requireMag = true;
    public int magCapacity = 7;

    [Header("Magazine Insertion")]
    public Vector3 magazineLocalPosition = new Vector3(0.104200006f, -0.00340008782f, 0f);
    public Quaternion magazineLocalRotation = Quaternion.identity;

    [Header("Grab Orientation")]
    [Tooltip("Extra yaw (degrees) applied to the grab attach so the gun points forward. Set to 180 to flip if it points toward you.")]
    public float attachYawOffset = 180f;

    [Header("Optional desktop testing (non-VR)")]
    public InputActionReference desktopFire; // allows left-click to fire in Editor

    [HideInInspector] public bool magInserted;
    int ammoInMag;
    float nextFireTime;
    bool isFiring;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    System.Action<InputAction.CallbackContext> desktopFireHandler;

    // Cache of interactor visual/physics state we modify while holding the gun
    class InteractorState
    {
        public List<Renderer> renderers = new List<Renderer>();
        public List<bool> rendererWasEnabled = new List<bool>();
        public Rigidbody rb;
        public bool rbHadDetectCollisions;
        public bool rbWasKinematic;
    }

    // Track one entry per interactor transform
    readonly Dictionary<Transform, InteractorState> _hiddenInteractorStates = new Dictionary<Transform, InteractorState>();

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (grab != null)
        {
            grab.activated.AddListener(OnActivated);
            grab.deactivated.AddListener(OnDeactivated);

            // Hide controller visuals and disable its rigidbody while holding this gun
            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);

            // Ensure we have an attach transform so orientation can be corrected (points forward)
            EnsureAttachTransformAndOrientation();
        }

        if (desktopFire != null)
        {
            desktopFireHandler = OnDesktopFire;
            desktopFire.action.performed += desktopFireHandler;
        }
    }

    void OnDestroy()
    {
        if (grab != null)
        {
            grab.activated.RemoveListener(OnActivated);
            grab.deactivated.RemoveListener(OnDeactivated);
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
        }

        if (desktopFire != null && desktopFireHandler != null)
            desktopFire.action.performed -= desktopFireHandler;
    }

    void Update()
    {
        if (isFiring)
            TryFire();
    }

    void OnActivated(ActivateEventArgs _) => isFiring = true;
    void OnDeactivated(DeactivateEventArgs _) => isFiring = false;

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        var interactor = args?.interactorObject;
        if (interactor == null) return;

        var t = interactor.transform;
        if (t == null || _hiddenInteractorStates.ContainsKey(t))
            return;

        var state = new InteractorState();

        // Hide all visual renderers on the interactor (controller model, hand mesh, etc.)
        // Collect renderers manually (overload with List may not exist on all Unity versions)
        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
        {
            state.renderers.Add(r);
        }
        foreach (var r in state.renderers)
        {
            state.rendererWasEnabled.Add(r.enabled);
            r.enabled = false;
        }

        // Disable physics influence from the controller while holding the gun
        state.rb = t.GetComponentInChildren<Rigidbody>();
        if (state.rb != null)
        {
            state.rbHadDetectCollisions = state.rb.detectCollisions;
            state.rbWasKinematic = state.rb.isKinematic;
            state.rb.detectCollisions = false;
            state.rb.isKinematic = true;
            state.rb.linearVelocity = Vector3.zero;
            state.rb.angularVelocity = Vector3.zero;
        }

        _hiddenInteractorStates[t] = state;
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        var interactor = args?.interactorObject;
        if (interactor == null) return;

        var t = interactor.transform;
        if (t == null) return;

        if (_hiddenInteractorStates.TryGetValue(t, out var state))
        {
            for (int i = 0; i < state.renderers.Count; i++)
            {
                var r = state.renderers[i];
                if (r != null)
                {
                    var wasEnabled = i < state.rendererWasEnabled.Count ? state.rendererWasEnabled[i] : true;
                    r.enabled = wasEnabled;
                }
            }

            if (state.rb != null)
            {
                state.rb.detectCollisions = state.rbHadDetectCollisions;
                state.rb.isKinematic = state.rbWasKinematic;
            }

            _hiddenInteractorStates.Remove(t);
        }
    }

    // Ensure the gun aligns forward when held by correcting the attach transform orientation
    void EnsureAttachTransformAndOrientation()
    {
        if (grab == null) return;

        // Create an attach transform if none assigned
        if (grab.attachTransform == null)
        {
            var attach = new GameObject("GripAttach").transform;
            attach.SetParent(transform, false);
            attach.localPosition = Vector3.zero;
            attach.localRotation = Quaternion.identity;
            grab.attachTransform = attach;
        }

        // Compute local forward of the muzzle in gun local space to determine yaw offset
        float yawDeg = 0f;
        if (muzzle != null)
        {
            Vector3 localMuzzleFwd = transform.InverseTransformDirection(muzzle.forward);

            // Determine the dominant horizontal axis (X or Z)
            float absX = Mathf.Abs(localMuzzleFwd.x);
            float absZ = Mathf.Abs(localMuzzleFwd.z);

            if (absX > absZ)
            {
                // Muzzle is closer to local ±X, rotate so it becomes +Z
                yawDeg = localMuzzleFwd.x > 0f ? -90f : 90f;
            }
            else if (absZ > 0.0001f)
            {
                // If it's already near ±Z, flip if pointing backwards
                yawDeg = localMuzzleFwd.z < 0f ? 180f : 0f;
            }
        }

        // Apply extra yaw as requested (default 180 flips if it points toward you)
        yawDeg += attachYawOffset;

        grab.attachTransform.localRotation = Quaternion.Euler(0f, yawDeg, 0f) * grab.attachTransform.localRotation;
        grab.matchAttachRotation = true;
        grab.matchAttachPosition = true;
    }

    void TryFire()
    {
        if (Time.time < nextFireTime) return;
        if (requireMag && (!magInserted || ammoInMag <= 0)) return;

        Fire();
        nextFireTime = Time.time + fireCooldown;
        if (requireMag) ammoInMag--;
    }

    void Fire()
    {
        if (!muzzle || !projectilePrefab) return;

        var go = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
        if (go.TryGetComponent<Rigidbody>(out var rb))
            rb.linearVelocity = muzzle.forward * projectileSpeed;

        Debug.Log("Gun fired");
    }

    public void SetMagInserted(bool inserted)
    {
        magInserted = inserted;
        if (inserted) ammoInMag = magCapacity;
    }
    // One-time flag to avoid repeated teleport while colliders keep contacting.
    bool magazineTeleported = false;

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null) return;
        TryInsertFromCollider(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        TryInsertFromCollider(other);
    }

    // Helper: climb ancestors to find a transform with the given name
    Transform FindAncestorNamed(Transform t, string targetName)
    {
        while (t != null)
        {
            if (t.name == targetName) return t;
            t = t.parent;
        }
        return null;
    }

    // Helper: search descendants of a specific root for a transform with the given name
    Transform FindDescendantNamed(Transform root, string targetName)
    {
        if (root == null) return null;
        var children = root.GetComponentsInChildren<Transform>(true);
        foreach (var c in children)
            if (c.name == targetName)
                return c;
        return null;
    }

    // Disable magazine colliders and make its rigidbody kinematic while inserted
    void ConfigureInsertedMagazine(Transform mag)
    {
        if (mag == null) return;

        // Disable any colliders on the magazine (and its children)
        var colliders = mag.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
            c.enabled = false;

        // Make the magazine's rigidbody (if any) kinematic and zero velocities
        var rb = mag.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    // Called when this gun body collides with something; if it's a magazine, insert it
    void TryInsertFromCollider(Collider other)
    {
        if (other == null) return;

        // Identify a transform named exactly "Magazine" related to the incoming collider
        Transform mag = FindAncestorNamed(other.transform, "Magazine");
        if (mag == null)
            mag = FindDescendantNamed(other.transform, "Magazine");
        if (mag == null) return;

        // Avoid redundant work if already parented here
        if (mag.parent == this.transform && magInserted)
        {
            // Ensure correct local pose
            mag.localPosition = magazineLocalPosition;
            mag.localRotation = magazineLocalRotation;
            return;
        }

        // If the magazine is held by an interactor, don't force-insert it
        var magGrab = mag.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (magGrab != null && magGrab.isSelected) return;

        InsertMagazine(mag);
    }

    void InsertMagazine(Transform mag)
    {
        if (mag == null) return;

        // Parent the magazine under this gun body (Gun_M1911)
        mag.SetParent(this.transform, false);
        mag.localPosition = magazineLocalPosition;
        mag.localRotation = magazineLocalRotation;

        // Stabilize physics and avoid immediate re-collision
        ConfigureInsertedMagazine(mag);

        // Update gameplay state
        SetMagInserted(true);
        Debug.Log("Magazine inserted and parented to gun body.");
    }

    void OnDesktopFire(InputAction.CallbackContext ctx)
    {
        TryFire();
    }
}