using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
    public float health = 100f;
    public float speed = 3f;
    [Header("Knockback")]
    public float knockbackDamping = 10f; // higher = faster decay

    private Vector3 knockbackVelocity;

    private Transform target;
    private CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else if (Camera.main != null)
        {
            // Fall back to the main camera (VR headset) so enemies move toward the view
            target = Camera.main.transform;
        }
    }

    void Update()
    {
        if (!target) return;

        Vector3 toPlayer = target.position - transform.position;
        Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;

        // Use SimpleMove for intended chasing movement (includes gravity)
        cc.SimpleMove(flat * speed);

        // Apply additional knockback movement (does not include gravity)
        if (knockbackVelocity.sqrMagnitude > 0.0001f)
        {
            cc.Move(knockbackVelocity * Time.deltaTime);
            // Exponential-style decay toward zero
            float decay = knockbackDamping * Time.deltaTime;
            if (decay > 1f) decay = 1f;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, decay);
        }

        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(flat);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f) Destroy(gameObject);
    }

    // Additive knockback impulse in world space
    public void ApplyKnockback(Vector3 impulse)
    {
        // Treat impulse as an immediate velocity kick
        knockbackVelocity += impulse;
    }
}
