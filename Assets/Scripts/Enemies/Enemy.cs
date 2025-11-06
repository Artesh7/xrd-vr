using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
    public float health = 100f;
    public float speed = 3f;
    [Header("Knockback")]
    public float knockbackDamping = 10f; // higher = faster decay

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Vector3 knockbackVelocity;

    private Transform target;
    private CharacterController cc;

    // State
    public bool IsDead { get; private set; }
    private bool engagedWithPlayer; // within PlayerRadius, stop running/attack

    // Expose Animator for helper scripts like EnemyPlayerRadiusTrigger
    public Animator Animator => animator;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!animator)
        {
            // Try find any Animator on self or children if not wired in inspector
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
        if (IsDead || !target) return;

        Vector3 toPlayer = target.position - transform.position;
        Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;

        // Use SimpleMove for intended chasing movement (includes gravity)
        if (!engagedWithPlayer)
        {
            cc.SimpleMove(flat * speed);
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

        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(flat);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        health -= amount;
        if (health <= 0f)
        {
            IsDead = true;
            // Optionally signal animator here (parameter name may vary per controller)
            // if (animator) animator.SetBool("Dead", true);
            Destroy(gameObject);
        }
    }

    // Additive knockback impulse in world space
    public void ApplyKnockback(Vector3 impulse)
    {
        // Treat impulse as an immediate velocity kick
        knockbackVelocity += impulse;
    }

    // Called by PlayerRadius trigger when the player enters/stays/exits
    public void OnPlayerRadiusEnter()
    {
        engagedWithPlayer = true;
    }

    public void OnPlayerRadiusExit()
    {
        engagedWithPlayer = false;
    }
}