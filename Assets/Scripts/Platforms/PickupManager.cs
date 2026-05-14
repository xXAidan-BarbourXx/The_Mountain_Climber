using UnityEngine;

public enum PowerUpType
{
    HigherJump,
    Invulnerability,
    ScoreMultiplier,
    Launch,          // Slot 3 — stored in inventory, activated on key press
    OxygenRefill     // Instant — never stored, applied immediately on pickup
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
    public float oxygenRefillPercent = 0.15f;   // 15% of max HP by default

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (type == PowerUpType.OxygenRefill)
        {
            // Instant apply — never enters inventory
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                float refillAmount = health.MaxHP * oxygenRefillPercent;
                health.RefillHP(refillAmount);
            }

            Destroy(gameObject);
            return;
        }

        // All other types go into the inventory
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddToSlot(type);

        Destroy(gameObject);
    }
}