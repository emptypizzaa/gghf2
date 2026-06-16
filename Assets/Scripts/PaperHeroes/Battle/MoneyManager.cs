using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// Time-recovering wallet with a cap. Initialized from StageData by BattleManager;
    /// only accrues while the battle is in the Playing state.
    /// </summary>
    public class MoneyManager : MonoBehaviour
    {
        public float Current { get; private set; }
        public float Max { get; private set; }
        public float PerSecond { get; private set; }

        BattleManager _battle;
        bool _ready;

        public void Init(StageData stage, BattleManager battle)
        {
            Current = stage.moneyStart;
            Max = stage.moneyMax;
            PerSecond = stage.moneyPerSecond;
            _battle = battle;
            _ready = true;
        }

        void Update()
        {
            if (!_ready) return;
            if (_battle != null && !_battle.IsPlaying) return;
            Current = Mathf.Min(Max, Current + PerSecond * Time.deltaTime);
        }

        public bool CanAfford(int cost) => Current >= cost;

        public bool TrySpend(int cost)
        {
            if (Current < cost) return false;
            Current -= cost;
            return true;
        }

        /// <summary>Test-only helper (used by the editor test menu): add money, ignoring the cap.</summary>
        public void DebugGive(float amount) => Current += amount;
    }
}
