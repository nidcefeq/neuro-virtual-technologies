using UnityEngine;

public class DuckFly : MonoBehaviour
{
    public float speed = 3f;
    public float verticalAmplitude = 0.4f;
    public float verticalFrequency = 2f;
    public float lifeTime = 12f;

    //  то-то (Spawner) выставит это сразу после Instantiate
    [HideInInspector] public GameDifficulty.Level difficulty = GameDifficulty.Level.Mid;

    private Vector3 direction;
    private float startY;
    private float elapsed;

    void Start()
    {
        ApplyDifficulty();

        startY = transform.position.y;
        direction = transform.forward;
    }

    private void ApplyDifficulty()
    {
        switch (difficulty)
        {
            case GameDifficulty.Level.Low:
                // ћедленно, плавно, долго живЄт Ч легко увернутьс€
                speed = Random.Range(1.5f, 2.5f);
                verticalAmplitude = 0.5f;
                verticalFrequency = 1.5f;
                lifeTime = 14f;
                break;

            case GameDifficulty.Level.High:
                // Ѕыстро, дЄргано, мало живЄт Ч сложно увернутьс€
                speed = Random.Range(5f, 7f);
                verticalAmplitude = 0.2f;
                verticalFrequency = 4f;
                lifeTime = 10f;
                break;

            case GameDifficulty.Level.Mid:
            default:
                speed = Random.Range(2.5f, 4f);
                verticalAmplitude = 0.4f;
                verticalFrequency = 2f;
                lifeTime = 12f;
                break;
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        transform.position += direction * speed * Time.deltaTime;

        Vector3 pos = transform.position;
        pos.y = startY + Mathf.Sin(elapsed * verticalFrequency) * verticalAmplitude;
        transform.position = pos;

        if (elapsed >= lifeTime)
            Destroy(gameObject);
    }
}