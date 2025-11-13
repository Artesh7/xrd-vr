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
            
            // Activate HitBlood and hide projectile
            Transform hitBlood = transform.Find("HitBlood");
            if (hitBlood != null)
            {
                hitBlood.gameObject.SetActive(true);
                
                // Get particle system duration
                ParticleSystem ps = hitBlood.GetComponent<ParticleSystem>();
                float bloodDuration = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 1f;
                
                // Hide projectile visuals
                var renderer = GetComponent<Renderer>();
                if (renderer != null)
                    renderer.enabled = false;
                
                // Disable collider so it doesn't hit anything else
                var collider = GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = false;
                
                // Stop rigidbody movement
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
                
                // Destroy after particle finishes
                Destroy(gameObject, bloodDuration);
            }
            else if (destroyOnHit)
            {
                // No HitBlood found, destroy immediately
                Destroy(gameObject);
            }

            hasHit = true;
        }
    }
}