using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifeTime = 4f;
    public float damage = 25f;

    void Start() => Destroy(gameObject, lifeTime);

    void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
