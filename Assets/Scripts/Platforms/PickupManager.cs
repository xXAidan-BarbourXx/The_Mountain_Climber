using UnityEngine;

public enum PowerUpType
{
    HigherJump,
    Invulnerability,
    ScoreMultiplier,
    Launch,
    OxygenRefill
}

public class PowerUp : MonoBehaviour
{
    [Header("Power-Up Settings")]
    public PowerUpType type;
    public float duration = 5f;
    public float jumpMultiplier = 1.5f;
    public float scoreMultiplier = 2f;

    [Header("Launch Settings")]
    public float launchUpwardForce = 5f;
    public float launchSpeedMultiplier = 3f;
    public float launchCollisionGracePeriod = 1f;

    [Header("Oxygen Refill Settings")]
    [Range(0f, 1f)]
    public float oxygenRefillPercent = 0.15f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Boss"))
        {
            BossController boss = other.GetComponent<BossController>();
            if (boss != null)
                boss.OnPowerUpAbsorbed();

            Destroy(gameObject);
            return;
        }

        if (!other.CompareTag("Player")) return;

        if (type == PowerUpType.OxygenRefill)
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                float refillAmount = health.MaxHP * oxygenRefillPercent;
                health.RefillHP(refillAmount);
            }
            Destroy(gameObject);
            return;
        }

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddToSlot(type);

        Destroy(gameObject);
    }
}