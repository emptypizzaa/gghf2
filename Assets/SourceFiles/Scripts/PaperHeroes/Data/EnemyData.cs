using UnityEngine;

namespace PaperHeroes
{
    /// <summary>적 유닛 1종의 데이터. 전투 수치는 CombatantData에서 상속.</summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "PaperHeroes/Enemy Data")]
    public class EnemyData : CombatantData
    {
        [Header("적 전용")]
        [Tooltip("보스(부기맨 등) 여부. 연출/표기 구분용.")]
        public bool isBoss = false;

        [Tooltip("처치 시 지급하는 꿈에너지(킬 보상). 경제 획득 2종 중 '몬스터 처치 보상'. (M3에서 경제에 연결)")]
        public float killReward = 0f;

        [Tooltip("처치 시 경험치(성장=프로토 OUT, 스키마만).")]
        public float expReward = 0f;
    }
}
