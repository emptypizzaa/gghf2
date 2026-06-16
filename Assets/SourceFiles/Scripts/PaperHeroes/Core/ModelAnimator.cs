using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 모델에 Combatant 행동 상태(이동/교전/정지)에 맞는 애니메이션 클립을 재생한다.
    /// glTFast가 가져온 클립은 Legacy라 Playables(AnimationClipPlayable)에 못 쓴다
    /// ("Legacy clips cannot be used in Playables") → 레거시 전용 `Animation` 컴포넌트로 재생한다.
    /// </summary>
    [RequireComponent(typeof(Animation))]
    public class ModelAnimator : MonoBehaviour
    {
        public Combatant combatant;
        public AnimationClip walk;
        public AnimationClip idle;
        public AnimationClip attack;

        private Animation _anim;
        private string _walkName;
        private string _idleName;
        private string _attackName;
        private string _current;

        private void Start()
        {
            _anim = GetComponent<Animation>();
            _anim.playAutomatically = false;

            // 같은 transform을 두 시스템이 건드리지 않도록 Animator가 있으면 끈다.
            var animator = GetComponent<Animator>();
            if (animator != null) animator.enabled = false;

            _idleName = Register(idle, WrapMode.Loop);
            _walkName = Register(walk, WrapMode.Loop);
            _attackName = Register(attack, WrapMode.Loop);

            string first = _idleName ?? _walkName ?? _attackName;
            if (!string.IsNullOrEmpty(first)) { _anim.Play(first); _current = first; }
        }

        private void Update()
        {
            if (combatant == null || _anim == null) return;

            string want;
            switch (combatant.Motion)
            {
                case Combatant.ActState.Attacking: want = _attackName ?? _idleName ?? _walkName; break;
                case Combatant.ActState.Moving:    want = _walkName ?? _idleName; break;
                default:                           want = _idleName ?? _walkName; break;
            }

            if (!string.IsNullOrEmpty(want) && want != _current)
            {
                _current = want;
                _anim.CrossFade(want, 0.15f);
            }
        }

        /// <summary>클립을 Animation 컴포넌트에 등록(레거시로 강제) 후 클립 이름 반환. 없으면 null.</summary>
        private string Register(AnimationClip clip, WrapMode wrap)
        {
            if (clip == null) return null;
            if (!clip.legacy) clip.legacy = true; // Animation 컴포넌트는 legacy 클립만 허용
            _anim.AddClip(clip, clip.name);
            var state = _anim[clip.name];
            if (state != null) state.wrapMode = wrap;
            return clip.name;
        }
    }
}
