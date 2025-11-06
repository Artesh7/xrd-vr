using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
    public float health = 100f;
    public float speed = 3f;

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

        // SimpleMove applies gravity automatically
        cc.SimpleMove(flat * speed);

        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(flat);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f) Destroy(gameObject);
    }
}
