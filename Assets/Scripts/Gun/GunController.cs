using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

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

    [Header("Optional desktop testing (non-VR)")]
    public InputActionReference desktopFire; // allows left-click to fire in Editor

    [HideInInspector] public bool magInserted;
    int ammoInMag;
    float nextFireTime;
    bool isFiring;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    System.Action<InputAction.CallbackContext> desktopFireHandler;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (grab != null)
        {
            grab.activated.AddListener(OnActivated);
            grab.deactivated.AddListener(OnDeactivated);
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