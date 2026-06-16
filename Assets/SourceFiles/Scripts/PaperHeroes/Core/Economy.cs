using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 플레이어 자원(돈). 매초 자동 회복하며 상한이 있다. 코어 루프의 "돈이 차오른다" 단계.
    /// EconomyConfig(SO)에서 수치를 읽되, 미할당 시 안전한 기본값으로 폴백한다(와이어 누락 시 NRE 방지).
    /// </summary>
    public class Economy : MonoBehaviour
    {
        public EconomyConfig config;

        [Header("config 미할당 시 폴백")]
        [SerializeField] private float fallbackStartingMoney = 50f;
        [SerializeField] private float fallbackMoneyPerSecond = 10f;
        [SerializeField] private float fallbackMoneyCap = 999f;

        public float CurrentMoney { get; private set; }

        private float StartingMoney => config != null ? config.startingMoney : fallbackStartingMoney;
        private float MoneyPerSecond => config != null ? config.moneyPerSecond : fallbackMoneyPerSecond;
        private float MoneyCap => config != null ? config.moneyCap : fallbackMoneyCap;

        private void Start()
        {
            CurrentMoney = Mathf.Min(StartingMoney, MoneyCap);
        }

        private void Update()
        {
            CurrentMoney = Mathf.Min(MoneyCap, CurrentMoney + MoneyPerSecond * Time.deltaTime);
        }

        public bool CanAfford(float cost) => CurrentMoney >= cost;

        public bool TrySpend(float cost)
        {
            if (CurrentMoney < cost) return false;
            CurrentMoney -= cost;
            return true;
        }

        /// <summary>테스트/디버그용 자원 핀(QA에서 게이트 검증 시 회복을 무시하고 고정).</summary>
        public void SetMoney(float amount) => CurrentMoney = Mathf.Clamp(amount, 0f, MoneyCap);
    }
}
