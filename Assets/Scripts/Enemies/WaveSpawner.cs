using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int initialEnemies = 5;
    public float spawnDelay = 0.5f;
    public float timeBetweenWaves = 5f;

    [Header("Audio")]
    [Tooltip("Sound played when a new wave starts")]
    public AudioClip waveStartSound;

    private AudioSource audioSource;

    void Start()
    {
        // Get or add AudioSource component for wave sound
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound (non-spatial for ambient effect)
            Debug.Log("WaveSpawner: Created new AudioSource component");
        }
        
        // Ensure AudioSource is enabled and configured properly
        audioSource.enabled = true;
        audioSource.mute = false;
        audioSource.volume = 1f;

        StartCoroutine(WaveLoop());
    }

    IEnumerator WaveLoop()
    {
        int wave = 0;
        while (true)
        {
            wave++;

            // Play wave start sound
            if (audioSource != null && waveStartSound != null)
            {
                audioSource.PlayOneShot(waveStartSound);
                Debug.Log($"Wave {wave} started - playing scary sound!");
            }
            else
            {
                if (audioSource == null) Debug.LogWarning("WaveSpawner: AudioSource is null!");
                if (waveStartSound == null) Debug.LogWarning("WaveSpawner: waveStartSound AudioClip not assigned in Inspector!");
                Debug.Log($"Wave {wave} started (no sound)");
            }

            int count = initialEnemies + (wave - 1);
            for (int i = 0; i < count; i++)
            {
                var sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
                Instantiate(enemyPrefab, sp.position, sp.rotation);
                yield return new WaitForSeconds(spawnDelay);
            }

            // wait until all enemies are gone
            while (FindObjectsOfType<Enemy>().Length > 0)
                yield return null;

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }
}
