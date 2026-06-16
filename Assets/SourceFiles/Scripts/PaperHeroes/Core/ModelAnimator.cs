using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace PaperHeroes
{
    /// <summary>
    /// 모델에 Combatant 행동 상태(이동/교전/정지)에 맞는 애니메이션 클립을 재생한다.
    /// Playables로 단일 클립을 재생하고 상태 변경 시 교체한다(트랜스폼 기반 glTF 클립용 — 스키닝/컨트롤러 불필요).
    /// 클립이 looping이 아니어도 수동 시간 래핑으로 반복.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class ModelAnimator : MonoBehaviour
    {
        public Combatant combatant;
        public AnimationClip walk;
        public AnimationClip idle;
        public AnimationClip attack;

        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationClipPlayable _playable;
        private AnimationPlayableOutput _output;
        private AnimationClip _current;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _graph = PlayableGraph.Create("PH_ModelAnim_" + GetInstanceID());
            _output = AnimationPlayableOutput.Create(_graph, "out", _animator);
            PlayClip(idle != null ? idle : walk);
            _graph.Play();
        }

        private void Update()
        {
            if (combatant != null)
            {
                AnimationClip want;
                switch (combatant.Motion)
                {
                    case Combatant.ActState.Attacking: want = attack != null ? attack : idle; break;
                    case Combatant.ActState.Moving:    want = walk != null ? walk : idle; break;
                    default:                           want = idle != null ? idle : walk; break;
                }
                PlayClip(want);
            }

            // 수동 루프(클립이 looping이 아니어도 반복).
            if (_playable.IsValid() && _current != null && _current.length > 0f)
            {
                double t = _playable.GetTime();
                if (t >= _current.length) _playable.SetTime(t % _current.length);
            }
        }

        private void PlayClip(AnimationClip clip)
        {
            if (clip == null || clip == _current) return;
            _current = clip;
            if (_playable.IsValid()) _playable.Destroy();
            _playable = AnimationClipPlayable.Create(_graph, clip);
            _output.SetSourcePlayable(_playable);
        }

        private void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }
    }
}
