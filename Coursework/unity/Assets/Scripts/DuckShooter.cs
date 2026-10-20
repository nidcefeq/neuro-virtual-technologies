using UnityEngine;
using UnityEngine.InputSystem;

public class DuckShooter : MonoBehaviour
{
    [Header("Стрельба")]
    public Shotgun shotgun;

    [Header("Настройки победы")]
    public int maxScore = 10;
    public GameObject winPanel;

    [Header("Ссылки на игру")]
    public DuckSpawner duckSpawner;

    private int score = 0;
    private bool isGameOver = false;

    void Update()
    {
        if (isGameOver) return;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (shotgun == null || shotgun.muzzle == null)
        {
            Debug.LogWarning("На DuckShooter не назначено ружьё или дуло.");
            return;
        }

        Transform muzzle = shotgun.muzzle;
        Ray ray = new Ray(muzzle.position, muzzle.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, shotgun.range))
        {
            DuckFly duck = hit.collider.GetComponentInParent<DuckFly>();

            if (duck != null)
            {
                score++;
                Debug.Log("Попадание! Счёт: " + score);
                Destroy(duck.gameObject);

                if (score >= maxScore)
                    WinGame();
            }
            else
            {
                Debug.Log("Попал в объект: " + hit.collider.name);
            }
        }
        else
        {
            Debug.Log("Промах!");
        }
    }

    void WinGame()
    {
        isGameOver = true;

        if (winPanel != null)
            winPanel.SetActive(true);

        if (duckSpawner != null)
            duckSpawner.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("ИГРА ОКОНЧЕНА! ПОБЕДА!");
    }
}