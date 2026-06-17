using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 스킬 테이블 1행(워드 21번). 프로토 범위에서는 스텁 — 필드만 정의하고 런타임 미사용.
    /// 코어 루프(자동정렬·전선·자원·승급) 검증이 끝난 뒤 실제 스킬 시스템을 별도 설계한다.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData", menuName = "PaperHeroes/Skill Data (stub)")]
    public class SkillData : ScriptableObject
    {
        [Tooltip("스킬 식별자(테이블 키)")]
        public string skillId;

        [Tooltip("표시 이름")]
        public string displayName;

        [Tooltip("스킬 타입(공격/버프/디버프/힐 등). 프로토 미사용 — 문자열 스텁.")]
        public string type;

        [Tooltip("쿨타임(초)")]
        public float cooldown;

        [Tooltip("사거리(X축 1D)")]
        public float range;

        [Tooltip("효과 설명/식별(프로토 미사용)")]
        public string effect;

        [Tooltip("효과 수치")]
        public float value;

        [Tooltip("지속시간(초)")]
        public float duration;

        [Tooltip("리소스/연출 식별(프로토 미사용)")]
        public string resource;
    }
}
