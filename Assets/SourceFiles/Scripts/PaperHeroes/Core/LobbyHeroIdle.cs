using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 로비 디오라마 표시 모델용 idle 모션 드라이버. 로비는 Time.timeScale=0이고 레거시 Animation은
    /// 전역 timeScale을 따라 멈추므로, idle 클립을 매 프레임 unscaledDeltaTime으로 직접 진행+Sample()한다.
    /// 클립이 없으면(Tank 프리미티브) 절차적 bob/sway로 대체. Sleeping은 누운 채 느린 호흡 bob.
    /// 모든 변위는 CaptureBase()로 캐시한 기준(FitModel이 잡은 위치/회전)에 상대 적용한다.
    /// </summary>
    public sealed class LobbyHeroIdle : MonoBehaviour
    {
        public enum Mode { Standing, Sleeping }

        Animation _anim;
        AnimationState _idleState;
        bool _hasIdle;
        float _clipLen = 1f;
        float _idleTime;
        float _speed = 1f;
        Mode _mode = Mode.Standing;

        Vector3 _basePos;
        Quaternion _baseRot;
        bool _baseCaptured;

        /// <param name="anim">표시 모델의 Animation(없으면 null → 절차적).</param>
        /// <param name="idle">idle 레거시 클립(없으면 절차적).</param>
        public void Init(Animation anim, AnimationClip idle, Mode mode, float speed)
        {
            _anim = anim;
            _mode = mode;
            _speed = Mathf.Max(0.01f, speed);

            if (_anim != null && idle != null)
            {
                if (!idle.legacy) idle.legacy = true;                 // Animation 컴포넌트는 legacy 클립만
                if (_anim.GetClip(idle.name) == null) _anim.AddClip(idle, idle.name);
                _anim.playAutomatically = false;
                _idleState = _anim[idle.name];
                if (_idleState != null)
                {
                    _idleState.wrapMode = WrapMode.Loop;
                    _idleState.enabled = true;
                    _idleState.weight = 1f;
                    _clipLen = Mathf.Max(0.01f, idle.length);
                    _hasIdle = true;
                }
            }
            CaptureBase();
        }

        /// <summary>슬롯 재배치(SetSelected) 후 호출 — 현재 localPosition/Rotation을 모션 기준으로 다시 캐시.</summary>
        public void CaptureBase()
        {
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
            _baseCaptured = true;
        }

        public void SetMode(Mode mode) => _mode = mode;

        void Update()
        {
            if (!_baseCaptured) return;
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;

            if (_mode == Mode.Sleeping)
            {
                // 누운 채 느린 호흡 — 기준 위치에 미세 bob(눕는 회전은 슬롯이 소유하므로 회전은 건드리지 않음).
                transform.localPosition = _basePos + Vector3.up * (Mathf.Sin(t * 0.8f) * 0.02f);
                return;
            }

            if (_hasIdle && _idleState != null)
            {
                // idle 클립을 unscaled로 직접 진행(컴포넌트 자동진행에 의존하지 않게 time을 매 프레임 명시).
                _idleTime = (_idleTime + dt * _speed) % _clipLen;
                _idleState.enabled = true;
                _idleState.weight = 1f;
                _idleState.time = _idleTime;
                _anim.Sample();
            }
            else
            {
                // 클립 없음(Tank 등): 절차적 bob + 미세 yaw 흔들림.
                transform.localPosition = _basePos + Vector3.up * (Mathf.Sin(t * 1.6f) * 0.03f);
                transform.localRotation = _baseRot * Quaternion.Euler(0f, Mathf.Sin(t * 0.9f) * 1.5f, 0f);
            }
        }
    }
}
