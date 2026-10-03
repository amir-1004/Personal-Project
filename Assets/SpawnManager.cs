using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject[] enemyPrefabs;
    public GameObject lifeUpPrefab;

    public float spawnZ = 18f;
    public float minX = -4.5f;
    public float maxX = 3.5f;
    public float spawnInterval = 1.5f;
    [Range(0f, 1f)]
    public float pickupChance = 0.25f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnObject), spawnInterval, spawnInterval);
    }

    void SpawnObject()
    {
        GameObject prefab = (lifeUpPrefab != null && Random.value < pickupChance)
            ? lifeUpPrefab
            : enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        float x = Random.Range(minX, maxX);
        Vector3 spawnPosition = new Vector3(x, prefab.transform.position.y, spawnZ);
        Instantiate(prefab, spawnPosition, prefab.transform.rotation);
    }
}
