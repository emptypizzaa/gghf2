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

        [Header("행동")]
        [Tooltip("true면 적을 때리는 대신 사거리 내 가장 다친 아군을 회복한다(attackDamage=회복량, attackRange=회복 사거리).")]
        public bool isHealer = false;

        [Header("원거리/발사체")]
        [Tooltip("true면 공격 시 즉시타격 대신 발사체를 발사한다(원거리).")]
        public bool usesProjectile = false;

        [Tooltip("발사체 비행 속도(유닛/초). 사거리·이속보다 충분히 빨라야 명중감이 산다.")]
        public float projectileSpeed = 14f;

        [Tooltip("발사체 명중 판정 거리(타겟에 이만큼 가까워지면 명중).")]
        public float projectileHitRadius = 0.35f;

        [Tooltip("발사체 안전 수명(초). 누수 방지 상한.")]
        public float projectileMaxLifetime = 3f;

        [Tooltip("발사체 비행 아크 높이(0이면 직선). 전선 뒤 슈터가 아군 머리 위로 넘기는 시각용.")]
        public float projectileArcHeight = 1.2f;

        [Tooltip("발사체 색(검정/미설정이면 prototypeColor로 폴백).")]
        public Color projectileColor = new Color(1f, 0.95f, 0.4f, 1f);

        [Header("비주얼(선택)")]
        [Tooltip("지정 시 프리미티브 대신 이 3D 모델을 유닛 외형으로 사용(지면에 맞춰 자동 스케일).")]
        public GameObject visualPrefab;

        [Tooltip("모델 사용 시 목표 높이(유닛/월드). 모델을 이 높이로 자동 스케일.")]
        public float visualHeight = 2.2f;

        [Tooltip("모델 애니메이션 클립(선택). 지정 시 이동/정지/공격 상태에 맞춰 재생.")]
        public AnimationClip walkClip;
        public AnimationClip idleClip;
        public AnimationClip attackClip;
    }
}
