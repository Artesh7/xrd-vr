using UnityEngine;

// Attach this to the child GameObject named "PlayerRadius" that has a Trigger Collider.
// It forwards player enter events to the parent Enemy to fire its Hit animation.
[DisallowMultipleComponent]
public class EnemyPlayerRadiusTrigger : MonoBehaviour
{
    private Enemy enemy;
    [Tooltip("Seconds between consecutive Hit triggers while player remains in radius.")]
    public float hitInterval = 1f;
    private float lastHitTime = -999f;

    void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        var col = GetComponent<Collider>();
        if (col && !col.isTrigger)
        {
            col.isTrigger = true; // ensure trigger behavior
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!enemy || enemy.IsDead) return;
        if (other.CompareTag("Player"))
        {
            var anim = enemy.Animator;
            if (anim)
            {
                anim.SetTrigger("Hit");
                lastHitTime = Time.time;
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!enemy || enemy.IsDead) return;
        if (other.CompareTag("Player"))
        {
            if (Time.time >= lastHitTime + hitInterval)
            {
                var anim = enemy.Animator;
                if (anim)
                {
                    anim.SetTrigger("Hit");
                }
                lastHitTime = Time.time;
            }
        }
    }
}
