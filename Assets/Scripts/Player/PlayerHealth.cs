using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHits = 3;
    private int currentHits = 0;
    
    [Header("Damage Effect")]
    public Image damageOverlay; // Red overlay image for damage effect
    public float damageFlashDuration = 0.5f;
    public Color damageColor = new Color(1f, 0f, 0f, 0.3f);
    
    [Header("Death")]
    public Image fadeOverlay; // Black overlay for death fade
    public TextMeshProUGUI gameOverText; // Game Over TextMeshPro text
    public float deathFadeDuration = 2f;
    public float restartDelay = 3f;
    
    private bool isDead = false;
    private float damageFlashTimer = 0f;

    void Start()
    {
        currentHits = 0;
        isDead = false;
        
        // Ensure overlays start transparent
        if (damageOverlay != null)
        {
            Color c = damageOverlay.color;
            c.a = 0f;
            damageOverlay.color = c;
        }
        
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
        }
        
        if (gameOverText != null)
        {
            Color c = gameOverText.color;
            c.a = 0f;
            gameOverText.color = c;
        }
    }

    void Update()
    {
        // Handle damage flash fade out
        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;
            if (damageOverlay != null)
            {
                float alpha = Mathf.Lerp(0f, damageColor.a, damageFlashTimer / damageFlashDuration);
                Color c = damageColor;
                c.a = alpha;
                damageOverlay.color = c;
            }
        }
    }

    public void TakeDamage()
    {
        if (isDead) return;
        
        currentHits++;
        Debug.Log($"Player hit! {currentHits}/{maxHits}");
        
        // Flash red damage overlay
        if (damageOverlay != null)
        {
            damageOverlay.color = damageColor;
            damageFlashTimer = damageFlashDuration;
        }
        
        if (currentHits >= maxHits)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        Debug.Log("Player died!");
        
        // Start death sequence
        StartCoroutine(DeathSequence());
    }

    System.Collections.IEnumerator DeathSequence()
    {
        // Fade away all enemies
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        foreach (var enemy in allEnemies)
        {
            if (!enemy.IsDead)
            {
                enemy.FadeAway(0.5f); // Start fading after 0.5 seconds
            }
        }
        
        float elapsed = 0f;
        
        // Fade to black and fade in Game Over text simultaneously
        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / deathFadeDuration);
            
            // Fade overlay to black
            if (fadeOverlay != null)
            {
                Color c = Color.black;
                c.a = alpha;
                fadeOverlay.color = c;
            }
            
            // Fade in Game Over text
            if (gameOverText != null)
            {
                Color c = gameOverText.color;
                c.a = alpha;
                gameOverText.color = c;
            }
            
            yield return null;
        }
        
        // Ensure both are fully visible
        if (fadeOverlay != null)
        {
            Color c = Color.black;
            c.a = 1f;
            fadeOverlay.color = c;
        }
        if (gameOverText != null)
        {
            Color c = gameOverText.color;
            c.a = 1f;
            gameOverText.color = c;
        }
        
        // Wait before restart
        yield return new WaitForSeconds(restartDelay);
        
        // Restart scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
