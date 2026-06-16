using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 아군 유닛과 적 유닛이 공유하는 전투 수치의 베이스.
    /// 1라인 디펜스라 전투는 X축 1D로 처리되며, s거리/공격은 양 진영이 동일한 규칙을 쓴다.
    /// 데이터-로직 분리 원칙: 수치는 전부 여기(ScriptableObject)에 두고 MonoBehaviour는 읽기만 한다.
    /// </summary>
    public abstract class CombatantData : ScriptableObject
    {
        [Header("표시")]
        public string displayName;

        [Tooltip("프로토 단계 색 구분(프리미티브 틴트용). 복셀 프리팹 교체 전까지 사용.")]
        public Color prototypeColor = Color.white;

        [Header("전투 수치")]
        [Tooltip("최대 체력")]
        public float maxHp = 100f;

        [Tooltip("이동 속도(유닛/초)")]
        public float moveSpeed = 2f;

        [Tooltip("공격력(1회 타격 데미지)")]
        public float attackDamage = 10f;

        [Tooltip("사거리(X축 1D 거리). 이 안에 적이 들어오면 멈추고 교전.")]
        public float attackRange = 1f;

        [Tooltip("공격 간격(초)")]
        public float attackInterval = 1f;
    }
}
