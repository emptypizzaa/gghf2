using UnityEngine;

namespace PaperHeroes
{
    /// <summary>자원(돈) 경제 설정. 매초 자동 회복 + 상한. 코어 루프의 "돈이 차오른다" 단계 수치.</summary>
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "PaperHeroes/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        [Tooltip("매치 시작 시 보유 자원")]
        public float startingMoney = 50f;

        [Tooltip("초당 자동 회복량")]
        public float moneyPerSecond = 10f;

        [Tooltip("자원 보유 상한")]
        public float moneyCap = 999f;
    }
}
