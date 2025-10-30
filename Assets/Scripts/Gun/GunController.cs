using UnityEngine;
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

    [HideInInspector] public bool magInserted;
    int ammoInMag;
    float nextFireTime;
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.activated.AddListener(OnActivated);     // trigger pulled
    }

    void OnDestroy()
    {
        if (grab != null) grab.activated.RemoveListener(OnActivated);
    }

    public void SetMagInserted(bool inserted)
    {
        magInserted = inserted;
        if (inserted) ammoInMag = magCapacity;
    }

    void OnActivated(ActivateEventArgs _)
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
    }
}
