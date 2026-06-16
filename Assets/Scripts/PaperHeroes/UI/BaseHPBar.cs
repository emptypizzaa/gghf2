using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PaperHeroes
{
    /// <summary>HP bar for a base (filled image + numeric label). Polls the Base each frame.</summary>
    public class BaseHPBar : MonoBehaviour
    {
        public Base target;
        public Image fill;
        public TextMeshProUGUI label;

        public void Bind(Base b) => target = b;

        void Update()
        {
            if (target == null) return;
            if (fill != null) fill.fillAmount = target.HPRatio;
            if (label != null) label.text = Mathf.CeilToInt(target.CurrentHP) + " / " + Mathf.CeilToInt(target.MaxHP);
        }
    }
}
