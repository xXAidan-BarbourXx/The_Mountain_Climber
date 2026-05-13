using UnityEngine;

public enum PowerUpType
{
    HigherJump,
    Invulnerability,
    ScoreMultiplier
}

public class PowerUp : MonoBehaviour
{
    [Header("Power-Up Settings")]
    public PowerUpType type;
    public float duration = 5f;
    public float jumpMultiplier = 1.5f;
    public float scoreMultiplier = 2f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        switch (type)
        {
            case PowerUpType.HigherJump:
                player.ApplyHigherJump(duration, jumpMultiplier);
                break;

            case PowerUpType.Invulnerability:
                player.ApplyInvulnerability(duration);
                break; // no obstacle destruction on pickup anymore

            case PowerUpType.ScoreMultiplier:
                GameManager.Instance.ActivateScoreMultiplier(duration, scoreMultiplier);
                break;
        }

        Destroy(gameObject);
    }
}