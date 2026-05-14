using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FallObs : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 9.81f;

    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 20f;

    private void Start()
    {
        Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        transform.position += Vector3.back * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            // Drain all remaining HP to guarantee death
            health.DrainHP(health.MaxHP);
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }

        Destroy(gameObject);
    }
}