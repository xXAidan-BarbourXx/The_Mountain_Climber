using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Emergency Use")]
    [SerializeField] private float emergencyHPCost = 25f;

    [Header("References assign in Inspector")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerGlow playerGlow;

    // 4 slots: 0=HigherJump, 1=Invulnerability, 2=ScoreMultiplier, 3=Launch
    private int[] counts = new int[4];
    private bool[] active = new bool[4];
    private float[] glowTimers = new float[4];          // remaining glow seconds per slot
    private Coroutine[] glowCoroutines = new Coroutine[4]; // one coroutine per slot, extended on reuse

    public int GetCount(int slot) => counts[slot];
    public bool IsActive(int slot) => active[slot];

    public event System.Action OnInventoryChanged;

    private PlayerInputActions playerInput;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        playerInput = new PlayerInputActions();
    }

    private void OnEnable()
    {
        playerInput.Player.UseSlot1.performed += _ => TryActivate(0);
        playerInput.Player.UseSlot2.performed += _ => TryActivate(1);
        playerInput.Player.UseSlot3.performed += _ => TryActivate(2);
        playerInput.Player.UseSlot4.performed += _ => TryActivate(3);
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.Player.UseSlot1.performed -= _ => TryActivate(0);
        playerInput.Player.UseSlot2.performed -= _ => TryActivate(1);
        playerInput.Player.UseSlot3.performed -= _ => TryActivate(2);
        playerInput.Player.UseSlot4.performed -= _ => TryActivate(3);
        playerInput.Disable();
    }

    public void AddToSlot(PowerUpType type)
    {
        int slot = SlotFor(type);
        if (slot < 0) return;
        counts[slot]++;
        OnInventoryChanged?.Invoke();
    }

    private void TryActivate(int slot)
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        bool hasItem = counts[slot] > 0;

        if (!hasItem)
        {
            if (playerHealth != null)
                playerHealth.DrainHP(emergencyHPCost);
        }
        else
        {
            counts[slot]--;
            OnInventoryChanged?.Invoke();
        }

        ApplyEffect(slot);
    }

    private void ApplyEffect(int slot)
    {
        float duration = 5f;

        glowTimers[slot] += duration;

        switch (slot)
        {
            case 0: playerController.ApplyHigherJump(duration, 1.5f); break;
            case 1: playerController.ApplyInvulnerability(duration); break;
            case 2: GameManager.Instance.ActivateScoreMultiplier(duration, 2f); break;
            case 3:
                playerController.ApplyLaunch(duration: duration, upwardForce: 5f,
                        speedMultiplier: 3f, collisionGracePeriod: 1f); break;
        }

        if (glowCoroutines[slot] == null)
            glowCoroutines[slot] = StartCoroutine(GlowTimer(slot));
    }

    private IEnumerator GlowTimer(int slot)
    {
        active[slot] = true;
        playerGlow?.SetGlow(GlowColorFor(slot));

        while (glowTimers[slot] > 0f)
        {
            glowTimers[slot] -= Time.deltaTime;
            yield return null;
        }

        glowTimers[slot] = 0f;
        active[slot] = false;
        glowCoroutines[slot] = null;

        if (!active[0] && !active[1] && !active[2] && !active[3])
            playerGlow?.ClearGlow();
        else
        {
            for (int i = 0; i < 4; i++)
                if (active[i]) { playerGlow?.SetGlow(GlowColorFor(i)); break; }
        }
    }

    private static int SlotFor(PowerUpType type)
    {
        return type switch
        {
            PowerUpType.HigherJump => 0,
            PowerUpType.Invulnerability => 1,
            PowerUpType.ScoreMultiplier => 2,
            PowerUpType.Launch => 3,
            PowerUpType.OxygenRefill => -1,
            _ => 0
        };
    }

    private static Color GlowColorFor(int slot)
    {
        return slot switch
        {
            0 => Color.yellow,
            1 => Color.green,
            2 => Color.cyan,
            3 => Color.magenta,
            _ => Color.white
        };
    }
}