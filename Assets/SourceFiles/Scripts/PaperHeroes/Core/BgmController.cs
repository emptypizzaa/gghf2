using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 게임 BGM 루프 재생 + AudioListener 보장. MatchManager.Start()가 AddComponent로 부착(씬 편집 없음).
    /// ★씬에 AudioListener가 0개라서(확인됨) 런타임에 1개를 보장한다 — 없으면 BGM/내레이션/영상 오디오가 전부 무음.
    /// per-scene(매 매치 재생성), DontDestroyOnLoad 미사용 → 재시작(씬 리로드) 시 소스 중복 누적을 방지한다.
    /// 인트로 시네마틱처럼 영상 자체 오디오가 있을 때 IntroController가 SetDuck()으로 잠시 줄인다.
    /// </summary>
    public class BgmController : MonoBehaviour
    {
        public static BgmController Instance { get; private set; }

        AudioSource _src;
        float _baseVolume = 0.55f;
        bool _muted;

        /// <summary>현재 음소거 상태(로비 BGM 토글 등 UI 표시용).</summary>
        public bool IsMuted => _muted;

        void Awake()
        {
            Instance = this;
            EnsureAudioListener();

            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = Resources.Load<AudioClip>("Story/bgm");
            _src.loop = true;
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;            // 2D — 임포터의 3D 플래그와 무관하게 비공간 재생
            _src.volume = _baseVolume;
            _src.ignoreListenerPause = true;   // AudioListener.pause 와 무관하게 유지
            if (_src.clip != null) _src.Play();
            else Debug.LogWarning("[BgmController] Resources/Story/bgm 로드 실패");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>영상 오디오가 우선일 때 BGM 볼륨을 일시적으로 배수(0~1)만큼 줄인다.</summary>
        public void SetDuck(float mul)
        {
            if (_src != null) _src.volume = _baseVolume * Mathf.Clamp01(mul);
        }

        public void SetVolume(float v)
        {
            _baseVolume = Mathf.Clamp01(v);
            if (_src != null && !_muted) _src.volume = _baseVolume;
        }

        /// <summary>BGM 음소거 토글(로비 버튼용). 새 음소거 상태를 반환한다.</summary>
        public bool ToggleMute()
        {
            _muted = !_muted;
            if (_src != null) _src.volume = _muted ? 0f : _baseVolume;
            return _muted;
        }

        /// <summary>씬에 AudioListener가 없으면 카메라(없으면 전용 GO)에 하나 보장한다. 멱등.</summary>
        static void EnsureAudioListener()
        {
            if (FindFirstObjectByType<AudioListener>() != null) return;
            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            var host = cam != null ? cam.gameObject : new GameObject("AudioListener");
            host.AddComponent<AudioListener>();
        }
    }
}
