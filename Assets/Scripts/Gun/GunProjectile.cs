using UnityEngine;

public class GunProjectile : MonoBehaviour
{
    [Header("Damage & Knockback")]
    public float damage = 50f;
    public float knockbackForce = 8f;

    [Header("Lifetime")]
    public float lifeTime = 5f;
    public bool destroyOnHit = true;

    Rigidbody rb;
    bool hasHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        if (collision == null || collision.collider == null) return;
        HandleHit(collision.collider, collision.GetContact(0).normal);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (other == null) return;
        // Trigger has no contact normal; use forward as fallback
        HandleHit(other, -transform.forward);
    }

    void HandleHit(Collider col, Vector3 surfaceNormal)
    {
        if (col == null) return;

        var enemy = col.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);

            // Compute knockback direction: prefer away from surface normal, fallback to projectile forward
            Vector3 dir = (-surfaceNormal).sqrMagnitude > 0.0001f ? -surfaceNormal : transform.forward;
            dir.y = 0f; // keep knockback mostly horizontal
            dir.Normalize();
            enemy.ApplyKnockback(dir * knockbackForce);

            hasHit = true;
            if (destroyOnHit)
                Destroy(gameObject);
        }
    }
}