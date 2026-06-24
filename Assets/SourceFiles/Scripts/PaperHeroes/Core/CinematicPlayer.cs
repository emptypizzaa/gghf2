using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace PaperHeroes
{
    /// <summary>
    /// 풀스크린 인트로 시네마틱 1개 재생. VideoPlayer → RenderTexture → 주어진 RawImage(surface)로 출력.
    /// 네이티브/에디터 = Resources VideoClip(검증 가능·안정), WebGL = StreamingAssets URL(VideoClip 불가).
    /// 종료(loopPointReached)·에러·Skip() 중 무엇이든 onFinished를 정확히 1회 호출. RenderTexture 명시적 해제.
    /// ★timeUpdateMode=UnscaledGameTime: 인트로는 Time.timeScale=0 게이트 중이므로 GameTime이면 영상이 멈춘다.
    /// </summary>
    public sealed class CinematicPlayer : MonoBehaviour
    {
        VideoPlayer _player;
        AudioSource _audio;
        RawImage _surface;
        RenderTexture _rt;
        Action _onFinished;
        bool _done;

        public bool IsPlaying => _player != null && _player.isPlaying;
        public long Frame => _player != null ? (long)_player.frame : -1L;

        /// <param name="surface">시네마틱을 그릴 풀스크린 RawImage.</param>
        /// <param name="resourcePath">Resources 경로(확장자 없이), 예: "Story/cine1".</param>
        /// <param name="streamingFileName">WebGL 폴백: StreamingAssets/Story 내 파일명, 예: "cine1.mp4".</param>
        public void Play(RawImage surface, string resourcePath, string streamingFileName, Action onFinished)
        {
            _surface = surface;
            _onFinished = onFinished;
            _done = false;

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _audio.ignoreListenerPause = true;

            _player = gameObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.renderMode = VideoRenderMode.RenderTexture;     // RT는 우리가 소유
            _player.isLooping = false;
            _player.waitForFirstFrame = true;                       // 첫 프레임 준비 후 표시(검은 깜빡임 방지)
            _player.skipOnDrop = true;
            _player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime; // timeScale=0 게이트 중에도 재생

            _player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _player.controlledAudioTrackCount = 1;
            _player.EnableAudioTrack(0, true);
            _player.SetTargetAudioSource(0, _audio);

#if UNITY_WEBGL && !UNITY_EDITOR
            _player.source = VideoSource.Url;
            _player.url = Path.Combine(Application.streamingAssetsPath, "Story", streamingFileName);
#else
            var clip = Resources.Load<VideoClip>(resourcePath);
            if (clip != null)
            {
                _player.source = VideoSource.VideoClip;
                _player.clip = clip;
            }
            else
            {
                _player.source = VideoSource.Url;
                _player.url = Path.Combine(Application.streamingAssetsPath, "Story", streamingFileName);
            }
#endif
            _player.prepareCompleted += OnPrepared;
            _player.loopPointReached += OnLoopPoint;
            _player.errorReceived += OnError;
            _player.Prepare();
        }

        void OnPrepared(VideoPlayer vp)
        {
            int w = (int)vp.width, h = (int)vp.height;
            if (w <= 0 || h <= 0) { w = Screen.width; h = Screen.height; }
            _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = "CinematicRT" };
            _rt.Create();
            vp.targetTexture = _rt;
            if (_surface != null)
            {
                _surface.texture = _rt;
                _surface.color = Color.white;
            }
            vp.Play();
            if (_audio != null) _audio.Play();
        }

        void OnLoopPoint(VideoPlayer vp) => Finish();

        void OnError(VideoPlayer vp, string msg)
        {
            Debug.LogWarning($"[CinematicPlayer] {msg}");
            Finish();   // 에러 시 막히지 말고 다음 상태로 진행
        }

        /// <summary>외부에서 스킵(탭). 멱등.</summary>
        public void Skip() => Finish();

        void Finish()
        {
            if (_done) return;
            _done = true;
            if (_player != null)
            {
                _player.loopPointReached -= OnLoopPoint;
                _player.prepareCompleted -= OnPrepared;
                _player.errorReceived -= OnError;
                _player.Stop();
            }
            var cb = _onFinished; _onFinished = null;
            cb?.Invoke();
            ReleaseRt();
        }

        void ReleaseRt()
        {
            // RenderTexture는 GC가 회수하지 않는 GPU 리소스 → 명시적 Release 필수.
            if (_rt != null) { _rt.Release(); Destroy(_rt); _rt = null; }
        }

        void OnDestroy() => ReleaseRt();
    }
}
