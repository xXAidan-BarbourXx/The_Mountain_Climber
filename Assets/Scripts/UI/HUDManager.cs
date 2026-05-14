using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [System.Serializable]
    public class InventorySlot
    {
        public Image icon;
        public TextMeshProUGUI countText;
        public Sprite sprite;
    }

    [Header("Inventory Slots (0=Jump, 1=Invul, 2=ScoreMult, 3=Launch)")]
    [SerializeField] private InventorySlot[] slots = new InventorySlot[4];

    [Header("Health Bar")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;

    private static readonly Color healthColorFull = Color.green;
    private static readonly Color healthColorLow = Color.red;

    private void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHPChanged += UpdateHealthBar;
            UpdateHealthBar(playerHealth.CurrentHP, playerHealth.MaxHP);
        }

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RefreshSlots;

        RefreshSlots();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHPChanged -= UpdateHealthBar;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshSlots;
    }

    private void RefreshSlots()
    {
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            int count = InventoryManager.Instance.GetCount(i);

            if (slots[i].icon != null)
            {
                slots[i].icon.sprite = slots[i].sprite;
                Color c = slots[i].icon.color;
                c.a = count > 0 ? 1f : 0.3f;
                slots[i].icon.color = c;
            }

            if (slots[i].countText != null)
            {
                slots[i].countText.text = count >= 0 ? count.ToString() : "";
                slots[i].countText.enabled = count >= 0;
            }
        }
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthSlider != null)
            healthSlider.value = current / max;

        if (healthFill != null)
            healthFill.color = Color.Lerp(healthColorLow, healthColorFull, current / max);
    }
}