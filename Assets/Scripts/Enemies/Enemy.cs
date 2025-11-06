using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
    public float health = 100f;
    public float speed = 3f;
    [Header("Knockback")]
    public float knockbackDamping = 10f; // higher = faster decay

    [Header("Death")]
    public float destroyDelayAfterDeath = 0f; // 0 = immediate destroy (adjust if you want death anim to play)

    [Header("Animation")]
    public Animator animator; // assign or auto-find

    [Header("Behavior")]
    [Tooltip("Enemy stops moving when closer than this distance to the player (stand-still while attacking).")]
    public float standStillDistance = 1.5f;
    [Tooltip("How quickly the enemy turns to face the player.")]
    public float rotationSpeed = 8f;

    private Vector3 knockbackVelocity;

    private Transform target;
    private CharacterController cc;
    private bool isDead;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!animator)
        {
            animator = GetComponentInChildren<Animator>();
        }
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
        if (isDead) return; // stop movement when dead
        if (!target) return;

        Vector3 toPlayer = target.position - transform.position;
        // Use horizontal (XZ) distance for movement decisions to avoid VR head height jitter
        Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
        float horizontalDistance = flat.magnitude;
        Vector3 moveDir = horizontalDistance > 0.0001f ? (flat / horizontalDistance) : Vector3.zero;

        // Chase only if outside standStillDistance
        if (horizontalDistance > standStillDistance)
        {
            // Use SimpleMove for intended chasing movement (includes gravity)
            cc.SimpleMove(moveDir * speed);
        }

        // Apply additional knockback movement (does not include gravity)
        if (knockbackVelocity.sqrMagnitude > 0.0001f)
        {
            cc.Move(knockbackVelocity * Time.deltaTime);
            // Exponential-style decay toward zero
            float decay = knockbackDamping * Time.deltaTime;
            if (decay > 1f) decay = 1f;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, decay);
        }

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            // Smooth rotation to reduce close-range jitter
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f && !isDead)
        {
            Die();
        }
    }

    // Additive knockback impulse in world space
    public void ApplyKnockback(Vector3 impulse)
    {
        // Treat impulse as an immediate velocity kick
        knockbackVelocity += impulse;
    }

    private void Die()
    {
        isDead = true;
        if (animator)
        {
            animator.SetBool("isDead", true);
        }
        // Hard stop all motion (including lingering knockback) when dead
        knockbackVelocity = Vector3.zero;
        // Optionally ensure CharacterController doesn't retain momentum
        // (SimpleMove applies internally each frame, so just skipping Update logic is enough.)
        if (destroyDelayAfterDeath > 0f)
        {
            Destroy(gameObject, destroyDelayAfterDeath);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Public accessors for helper trigger component
    public bool IsDead => isDead;
    public Animator Animator => animator;
}
