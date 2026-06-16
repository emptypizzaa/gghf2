using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PaperHeroes
{
    /// <summary>
    /// One ally spawn button. Shows name + cost, a cooldown sweep (filled image), and
    /// disables itself when unaffordable or on cooldown. Click spawns via AllySpawner.
    /// </summary>
    public class UnitButton : MonoBehaviour
    {
        public CharacterData data;
        public AllySpawner spawner;
        public MoneyManager money;

        public Button button;
        public Image cooldownSweep;       // fillAmount 0..1 = cooldown remaining
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI costLabel;

        // Public fields are wired by BattleSceneBuilder at edit time (they serialize).
        // Labels + onClick are (re)bound at runtime here, since UnityEvent listeners and
        // runtime label text do not persist from edit-time assignment.
        void Start()
        {
            if (data != null)
            {
                if (nameLabel != null) nameLabel.text = data.displayName;
                if (costLabel != null) costLabel.text = data.cost.ToString();
            }
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
                button.onClick.AddListener(OnClick);
            }
            if (cooldownSweep != null) cooldownSweep.fillAmount = 0f;
        }

        void OnClick()
        {
            if (spawner != null && data != null) spawner.TrySpawn(data);
        }

        void Update()
        {
            if (data == null) return;
            if (button != null)
                button.interactable = spawner != null && spawner.CanSpawn(data);
            if (cooldownSweep != null)
                cooldownSweep.fillAmount = spawner != null ? spawner.CooldownRemaining01(data) : 0f;
        }
    }
}
