using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PaperHeroes
{
    /// <summary>
    /// Center WIN/LOSE panel with a restart button. The component lives on an always-active
    /// holder; <see cref="panelRoot"/> is the child that gets shown/hidden.
    /// </summary>
    public class ResultPanel : MonoBehaviour
    {
        public BattleManager battle;
        public GameObject panelRoot;      // toggled on Show/Hide
        public TextMeshProUGUI resultText;
        public Button restartButton;

        void Awake()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestart);
                restartButton.onClick.AddListener(OnRestart);
            }
            Hide();
        }

        public void Show(bool win)
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (resultText != null) resultText.text = win ? "WIN" : "LOSE";
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        void OnRestart()
        {
            if (battle != null) battle.Restart();
        }
    }
}
