using UnityEngine;

public class DuckSpawner : MonoBehaviour
{
    public GameObject duckPrefab;
    public float spawnInterval = 2f;
    public float spawnWidth = 5f;
    public float spawnHeight = 2f;

    public float modelYOffset = -90f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= GetCurrentInterval())
        {
            SpawnDuck();
            timer = 0f;
        }
    }

    private float GetCurrentInterval()
    {
        switch (GameDifficulty.Current)
        {
            case GameDifficulty.Level.Low: return spawnInterval * 1.5f; // реже
            case GameDifficulty.Level.High: return spawnInterval * 0.5f; // чаще
            case GameDifficulty.Level.Mid:
            default: return spawnInterval;
        }
    }

    void SpawnDuck()
    {
        Vector3 pos = transform.position;
        pos += transform.right * Random.Range(-spawnWidth, spawnWidth);
        pos += transform.up * Random.Range(-0.5f, spawnHeight);

        Quaternion spawnRotation = transform.rotation * Quaternion.Euler(0f, modelYOffset, 0f);

        GameObject duck = Instantiate(duckPrefab, pos, spawnRotation);

        DuckFly fly = duck.GetComponent<DuckFly>();
        if (fly != null)
        {
            // Ключевая строка: пробрасываем текущий уровень сложности.
            // DuckFly сам выставит speed/amplitude/frequency/lifeTime в Start().
            fly.difficulty = GameDifficulty.Current;
        }
    }
}