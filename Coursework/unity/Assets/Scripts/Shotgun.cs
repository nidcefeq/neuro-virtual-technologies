using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Shotgun : MonoBehaviour
{
    [Header("Настройки стрельбы")]
    public Transform muzzle;
    public float range = 50f;
    public GameObject muzzleFlash;

    [Header("Прицел")]
    public Transform reticle;
    [Tooltip("Базовый размер прицела. Подберите значение (например, 0.02 - 0.05)")]
    public float reticleBaseScale = 0.03f;

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
            grabInteractable.activated.AddListener(OnShoot);
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
            grabInteractable.activated.RemoveListener(OnShoot);
    }

    private void Update()
    {
        UpdateReticle();
    }

    private void UpdateReticle()
    {
        if (muzzle == null || reticle == null)
            return;

        Ray ray = new Ray(muzzle.position, muzzle.forward);
        float distance = range; // Расстояние по умолчанию

        // Стреляем лучом, чтобы найти точку попадания
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            reticle.position = hit.point;
            distance = hit.distance; // Запоминаем реальное расстояние до объекта
        }
        else
        {
            // Если никуда не попали, ставим на максимальную дистанцию
            reticle.position = muzzle.position + muzzle.forward * range;
        }

        // Динамическое масштабирование: увеличиваем прицел пропорционально расстоянию
        float currentScale = distance * reticleBaseScale;

        // Защита от слишком маленького размера (чтобы не исчез вблизи)
        currentScale = Mathf.Max(currentScale, 0.01f);

        // Применяем масштаб
        reticle.localScale = new Vector3(currentScale, currentScale, currentScale);

        // Прицел всегда виден
        reticle.gameObject.SetActive(true);
    }

    private void OnShoot(ActivateEventArgs args)
    {
        Shoot();
    }

    private void Shoot()
    {
        if (muzzle == null)
        {
            Debug.LogWarning("У Shotgun не назначено дуло!");
            return;
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(true);
            CancelInvoke(nameof(HideFlash));
            Invoke(nameof(HideFlash), 0.08f);
        }

        if (Physics.Raycast(
            muzzle.position,
            muzzle.forward,
            out RaycastHit hit,
            range))
        {
            DuckFly duck =
                hit.collider.GetComponentInParent<DuckFly>();

            if (duck != null)
            {
                Debug.Log("Попадание!");
                Destroy(duck.gameObject);
            }
        }

        Debug.Log("Выстрел!");
    }

    private void HideFlash()
    {
        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);
    }
}