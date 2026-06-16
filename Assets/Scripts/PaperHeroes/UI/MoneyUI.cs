using UnityEngine;
using TMPro;

namespace PaperHeroes
{
    /// <summary>Top HUD: shows current / max money.</summary>
    public class MoneyUI : MonoBehaviour
    {
        public MoneyManager money;
        public TextMeshProUGUI label;

        void Update()
        {
            if (money == null || label == null) return;
            label.text = Mathf.FloorToInt(money.Current) + " / " + Mathf.FloorToInt(money.Max);
        }
    }
}
