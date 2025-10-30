using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int initialEnemies = 5;
    public float spawnDelay = 0.5f;
    public float timeBetweenWaves = 5f;

    void Start() => StartCoroutine(WaveLoop());

    IEnumerator WaveLoop()
    {
        int wave = 0;
        while (true)
        {
            wave++;
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
