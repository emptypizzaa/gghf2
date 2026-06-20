using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 아군 유닛과 적 유닛이 공유하는 전투 수치의 베이스.
    /// 1라인 디펜스라 전투는 X축 1D로 처리되며, s거리/공격은 양 진영이 동일한 규칙을 쓴다.
    /// 데이터-로직 분리 원칙: 수치는 전부 여기(ScriptableObject)에 두고 MonoBehaviour는 읽기만 한다.
    /// </summary>
    /// <summary>용병 등급(워드 용병 테이블 '등급'). 프로토 미사용 — UI/조합 라벨 스텁.</summary>
    public enum UnitGrade { Common, Rare, Epic, Legendary }

    public abstract class CombatantData : ScriptableObject
    {
        [Header("식별/테이블")]
        [Tooltip("테이블 키(고유 ID). 미설정 시 에셋 파일명으로 식별.")]
        public string id;

        [Tooltip("등급(프로토 스텁, 전투 미반영).")]
        public UnitGrade grade = UnitGrade.Common;

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

        [Tooltip("방어력. 피격 시 감산 경감 max(1, 데미지-방어력). 0=경감 없음. (M2에서 TakeDamage에 적용)")]
        public float defense = 0f;

        [Header("테이블 메타(프로토 스텁 — 성장/스킬 보류)")]
        [Tooltip("레벨업당 공격력 증가량(성장=프로토 OUT, 스키마만).")]
        public float atkPerLevel = 0f;

        [Tooltip("레벨업당 체력 증가량(성장=프로토 OUT, 스키마만).")]
        public float hpPerLevel = 0f;

        [Tooltip("스킬 식별자(SkillData.skillId 참조 키). 프로토 미사용 스텁.")]
        public string skillId;

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

        [Tooltip("모델 정면 보정(Y도). 모델 기본 정면이 Unity 표준(+Z)과 다를 때. 행군 방향을 바라보게 회전한 뒤 이 각도를 더한다. 예: 정면이 -Z인 모델은 180.")]
        public float modelYawOffset = 0f;

        [Tooltip("모델 애니메이션 클립(선택). 지정 시 이동/정지/공격 상태에 맞춰 재생.")]
        public AnimationClip walkClip;
        public AnimationClip idleClip;
        public AnimationClip attackClip;

        [Tooltip("사망 애니메이션 클립(선택). 지정 시 죽을 때 1회 재생(마지막 프레임 유지) 후 소멸. 미지정=즉시 소멸(기존).")]
        public AnimationClip deathClip;

        [Header("무기 소켓(선택)")]
        [Tooltip("숨길 모델 자식 노드명(예: 지팡이 'Staff-Global'). 그 하위 Renderer를 끈다. 빈값=숨김 없음.")]
        public string hideChildNode;

        [Tooltip("손에 부착할 무기 prop(예: 활 glb). null이고 usePlaceholderBow면 절차적 활.")]
        public GameObject weaponPrefab;

        [Tooltip("weaponPrefab 미설정 시 절차적 플레이스홀더 활을 생성·부착.")]
        public bool usePlaceholderBow = false;

        [Tooltip("무기 prop의 부모 노드명(예: 손 'Right_Hand-Global'). 빈값=모델 루트.")]
        public string weaponSocketNode;

        [Tooltip("무기 prop 로컬 위치(소켓 기준).")]
        public Vector3 weaponLocalPos;

        [Tooltip("무기 prop 로컬 회전(오일러, 소켓 기준).")]
        public Vector3 weaponLocalEuler;

        [Tooltip("무기 prop 로컬 스케일 배수(소켓 본 스케일에 곱해짐 — 시각 튜닝).")]
        public float weaponScale = 1f;
    }
}
