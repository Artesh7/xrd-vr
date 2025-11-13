using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
    public float health = 100f;
    public float speed = 3f;
    [Header("Knockback")]
    public float knockbackDamping = 10f; // higher = faster decay

    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    [Header("Death")]
    public float deathAnimDuration = 2f; // Time before fade starts
    public float fadeOutDuration = 1f; // Fade duration

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
        // If dead, stop all updates (no movement, no rotation)
        if (IsDead) return;
        
        if (!target) return;

        Vector3 toPlayer = target.position - transform.position;
        Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;

        // Use SimpleMove for intended chasing movement (includes gravity)
        // Only move when NOT engaged with player (not attacking)
        if (!engagedWithPlayer)
        {
            cc.SimpleMove(flat * speed);
        }
        else
        {
            // When engaged (attacking), apply gravity only, no forward movement
            cc.SimpleMove(Vector3.zero);
        }

        // Apply additional knockback movement (does not include gravity)
        // Even when attacking, knockback should still work
        if (knockbackVelocity.sqrMagnitude > 0.0001f)
        {
            cc.Move(knockbackVelocity * Time.deltaTime);
            // Exponential-style decay toward zero
            float decay = knockbackDamping * Time.deltaTime;
            if (decay > 1f) decay = 1f;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, decay);
        }

        // Always face the player (rotation) regardless of movement state
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
            
            // Stop all velocity immediately when dead
            knockbackVelocity = Vector3.zero;
            
            // Trigger Die animation in animator
            if (animator)
            {
                animator.SetBool("isDead", true);
                animator.SetTrigger("Die");
            }
            
            // Start death and fade sequence
            StartCoroutine(DeathSequence());
        }
    }
    
    IEnumerator DeathSequence()
    {
        // Wait for death animation to play
        yield return new WaitForSeconds(deathAnimDuration);
        
        // Get all renderers to fade
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        // Store original materials and create fade materials
        Material[][] originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
            
            // Create new material instances for fading
            Material[] newMats = new Material[renderers[i].materials.Length];
            for (int j = 0; j < renderers[i].materials.Length; j++)
            {
                newMats[j] = new Material(renderers[i].materials[j]);
                
                // Enable transparency if not already
                if (newMats[j].HasProperty("_Mode"))
                {
                    newMats[j].SetFloat("_Mode", 3); // Transparent mode
                    newMats[j].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    newMats[j].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    newMats[j].SetInt("_ZWrite", 0);
                    newMats[j].DisableKeyword("_ALPHATEST_ON");
                    newMats[j].EnableKeyword("_ALPHABLEND_ON");
                    newMats[j].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    newMats[j].renderQueue = 3000;
                }
            }
            renderers[i].materials = newMats;
        }
        
        // Fade out
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                }
            }
            
            yield return null;
        }
        
        // Destroy after fade completes
        Destroy(gameObject);
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
    
    // Called externally (e.g., when player dies) to fade away immediately
    public void FadeAway(float delay = 0f)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(FadeAwaySequence(delay));
    }
    
    IEnumerator FadeAwaySequence(float delay)
    {
        // Optional delay before starting fade
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        
        // Get all renderers to fade
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        // Create fade materials
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] newMats = new Material[renderers[i].materials.Length];
            for (int j = 0; j < renderers[i].materials.Length; j++)
            {
                newMats[j] = new Material(renderers[i].materials[j]);
                
                // Enable transparency
                if (newMats[j].HasProperty("_Mode"))
                {
                    newMats[j].SetFloat("_Mode", 3);
                    newMats[j].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    newMats[j].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    newMats[j].SetInt("_ZWrite", 0);
                    newMats[j].DisableKeyword("_ALPHATEST_ON");
                    newMats[j].EnableKeyword("_ALPHABLEND_ON");
                    newMats[j].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    newMats[j].renderQueue = 3000;
                }
            }
            renderers[i].materials = newMats;
        }
        
        // Fade out
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.materials != null)
                {
                    foreach (var mat in renderer.materials)
                    {
                        if (mat != null && mat.HasProperty("_Color"))
                        {
                            Color c = mat.color;
                            c.a = alpha;
                            mat.color = c;
                        }
                    }
                }
            }
            
            yield return null;
        }
        
        // Destroy after fade completes
        Destroy(gameObject);
    }
}