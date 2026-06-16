using UnityEngine;

namespace PaperHeroes
{
    /// <summary>적 유닛 1종의 데이터. 전투 수치는 CombatantData에서 상속.</summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "PaperHeroes/Enemy Data")]
    public class EnemyData : CombatantData
    {
        [Header("적 전용")]
        [Tooltip("보스(부시맨 등) 여부. 연출/표기 구분용.")]
        public bool isBoss = false;
    }
}
