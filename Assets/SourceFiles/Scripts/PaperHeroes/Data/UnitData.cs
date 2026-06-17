using UnityEngine;

namespace PaperHeroes
{
    /// <summary>유닛 포지션. 수치가 행동을 결정하므로 로직용이라기보단 분류/UI/조합 설계용 라벨이다.</summary>
    public enum UnitRole
    {
        Tank,    // 종이상자 — 단단하게 전선을 버틴다
        Melee,   // 종이칼 — 값싸고 빠른 주력 화력
        Ranged,  // 종이활 — 뒤에서 안전하게 화력
        Healer   // 종이지팡이 — STRETCH(M5), MVP 아님
    }

    /// <summary>아군 용병 1종의 데이터. 전투 수치는 CombatantData에서 상속.</summary>
    [CreateAssetMenu(fileName = "UnitData", menuName = "PaperHeroes/Unit Data")]
    public class UnitData : CombatantData
    {
        [Header("소환")]
        [Tooltip("소환 비용(자원)")]
        public float cost = 50f;

        [Tooltip("소환 후 재소환까지 쿨다운(초)")]
        public float summonCooldown = 2f;

        [Tooltip("포지션(분류/UI용). 실제 행동은 전투 수치로 결정된다.")]
        public UnitRole role = UnitRole.Melee;

        [Tooltip("판매 환급률(0~1). 판매=프로토 OUT, 스키마만(워드 용병 테이블 '판매환급률').")]
        [Range(0f, 1f)] public float sellRefundRate = 0.5f;
    }
}
