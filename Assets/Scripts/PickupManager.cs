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
}
