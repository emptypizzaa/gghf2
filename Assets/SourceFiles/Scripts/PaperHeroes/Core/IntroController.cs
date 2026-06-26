using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace PaperHeroes
{
    /// <summary>
    /// 앱 실행당 1회 인트로 스토리 시퀀스(씬 편집 없는 런타임 오버레이, CombatHUD의 UI 빌드 패턴을 따름).
    /// 흐름: 커버(키아트B, 탭 시작) → 프롤로그 슬라이드쇼(교실 7장 + 내레이션) → 시네마틱(영상 2개) → 타이틀(키아트A + 시작).
    /// ★Time.timeScale=0으로 전투를 게이트(검증됨: Economy/SummonController/WaveSpawner 전부 정지). "시작" 클릭 시 1로 복구.
    /// ★재시작(씬 리로드)에선 static 플래그로 인트로를 스킵 → 전투만 즉시 재개(BGM은 BgmController가 매 매치 별도 유지).
    /// 코루틴은 게이트 중(timeScale=0) 진행해야 하므로 Time.unscaledDeltaTime 기반으로 대기한다.
    /// </summary>
    public class IntroController : MonoBehaviour
    {
        static bool _shownThisSession;

        // 미디어 경로(Resources/Story, 확장자 없이)
        const string TitleBg     = "Story/title_bg";    // 키아트 A (메인 타이틀)
        const string CoverBg     = "Story/title_bg_b";  // 키아트 B (커버)
        const string PrologueFmt = "Story/prologue_{0:00}";
        const int    PrologueMax = 12;                  // 있는 만큼만(없으면 중단)
        const string CineFmt     = "Story/cine{0}";
        const int    CineCount   = 2;
        const float  SlideHold   = 2.6f;
        const float  FadeDur     = 0.5f;

        Canvas _canvas;
        RawImage _bg;                 // 정지 이미지(커버/프롤로그/타이틀)
        AspectRatioFitter _bgFit;
        RawImage _video;              // 시네마틱 표면
        TextMeshProUGUI _hint;
        Button _skipBtn;
        GameObject _startGroup;
        BgmController _bgm;
        AudioSource _narration;
        bool _tapped, _skipRequested, _finished;

        static readonly Color Dark    = new Color(0.02f, 0.02f, 0.04f, 1f);
        static readonly Color BtnGold = new Color(0.85f, 0.62f, 0.18f, 1f);
        static readonly Color BtnDim  = new Color(0f, 0f, 0f, 0.5f);

        void Awake()
        {
            if (_shownThisSession) { Destroy(this); return; }  // 재시작: 인트로 스킵, 게이트 안 함
            _shownThisSession = true;
            Time.timeScale = 0f;                                // 전투 게이트
        }

        void Start()
        {
            _bgm = BgmController.Instance != null ? BgmController.Instance : FindFirstObjectByType<BgmController>();
            EnsureEventSystem();
            BuildUI();
            StartCoroutine(Flow());
        }

        void OnDestroy()
        {
            // 인트로 완료(OnStart) 전에 외부 요인으로 파괴당해도 전투가 멈춘 채 남지 않게 timeScale 복구.
            if (!_finished && Time.timeScale == 0f) Time.timeScale = 1f;
        }

        IEnumerator Flow()
        {
            // 1) 커버 — 탭하여 시작(WebGL 오디오 언블록 제스처 겸용)
            SetBg(CoverBg);
            _hint.text = "화면을 탭하여 모험 시작 ▶";
            _hint.gameObject.SetActive(true);
            yield return FadeBg(0f, 1f);
            yield return new WaitUntil(() => _tapped || _skipRequested);
            _hint.gameObject.SetActive(false);

            PlayNarration();   // 내레이션은 프롤로그와 함께 시작

            // 2) 프롤로그 슬라이드쇼(교실) — 현실 세계 프레임
            _skipBtn.gameObject.SetActive(true);
            for (int i = 1; i <= PrologueMax && !_skipRequested; i++)
            {
                var tex = Resources.Load<Texture2D>(string.Format(PrologueFmt, i));
                if (tex == null) break;
                SetBgTexture(tex);
                yield return FadeBg(0f, 1f);
                float t = 0f;
                while (t < SlideHold && !_skipRequested) { t += Time.unscaledDeltaTime; yield return null; }
            }

            // 3) 시네마틱(영상) — 꿈/모험
            if (!_skipRequested)
            {
                _video.gameObject.SetActive(true);
                if (_bgm != null) _bgm.SetDuck(0.25f);
                for (int i = 1; i <= CineCount && !_skipRequested; i++)
                    yield return PlayOneCinematic(i);
                if (_bgm != null) _bgm.SetDuck(1f);
                _video.gameObject.SetActive(false);
            }

            // 4) 타이틀 — 키아트 + 시작 버튼
            _skipBtn.gameObject.SetActive(false);
            SetBg(TitleBg);
            yield return FadeBg(0f, 1f);
            _startGroup.SetActive(true);
            yield return new WaitUntil(() => _finished);
        }

        IEnumerator PlayOneCinematic(int idx)
        {
            bool done = false;
            var go = new GameObject($"Cinematic{idx}");
            go.transform.SetParent(transform, false);
            var cp = go.AddComponent<CinematicPlayer>();
            cp.Play(_video, string.Format(CineFmt, idx), $"cine{idx}.mp4", () => done = true);

            // 종료/스킵 대기(안전장치: 준비 실패 등으로 막히지 않게 최대 20초 realtime)
            float t = 0f;
            while (!done && !_skipRequested && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            cp.Skip();          // 멱등(이미 done이면 무동작)
            Destroy(go);
        }

        // ---------- 입력 콜백 ----------
        void OnTap() => _tapped = true;
        void OnSkip() => _skipRequested = true;

        void OnStart()
        {
            if (_finished) return;
            _finished = true;
            Time.timeScale = 1f;                              // 전투 개시
            if (_narration != null) _narration.Stop();
            if (_bgm != null) _bgm.SetDuck(1f);
            if (_canvas != null) Destroy(_canvas.gameObject);
            Destroy(this);
        }

        // ---------- 미디어/연출 ----------
        void PlayNarration()
        {
            var clip = Resources.Load<AudioClip>("Story/narration");
            if (clip == null) return;
            _narration = gameObject.AddComponent<AudioSource>();
            _narration.clip = clip;
            _narration.loop = false;
            _narration.playOnAwake = false;
            _narration.spatialBlend = 0f;
            _narration.volume = 1f;
            _narration.ignoreListenerPause = true;
            _narration.Play();
        }

        void SetBg(string resPath) => SetBgTexture(Resources.Load<Texture2D>(resPath));

        void SetBgTexture(Texture tex)
        {
            if (tex == null) return;
            _bg.texture = tex;
            if (_bgFit != null && tex.height > 0) _bgFit.aspectRatio = (float)tex.width / tex.height;
        }

        IEnumerator FadeBg(float from, float to)
        {
            var c = _bg.color; c.a = from; _bg.color = c;
            float t = 0f;
            while (t < FadeDur)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(from, to, t / FadeDur);
                _bg.color = c;
                yield return null;
            }
            c.a = to; _bg.color = c;
        }

        // ---------- 런타임 UI 빌드 ----------
        void BuildUI()
        {
            var go = new GameObject("IntroCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;                         // 모든 게임 UI(소환0/HUD1/결과100) 위
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            go.AddComponent<GraphicRaycaster>();

            // 검은 베이스(레터박스 여백 채움, 비상호작용)
            var baseImg = NewUI("Base", _canvas.transform).gameObject.AddComponent<Image>();
            baseImg.color = Dark;
            Stretch(baseImg.rectTransform);
            baseImg.raycastTarget = false;

            // 배경 이미지(정지) — 종횡비 유지(키아트는 4:3, 화면은 16:9 → 필러박스)
            _bg = NewUI("BgImage", _canvas.transform).gameObject.AddComponent<RawImage>();
            var brt = _bg.rectTransform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
            _bg.raycastTarget = false;
            _bg.color = new Color(1f, 1f, 1f, 0f);
            _bgFit = _bg.gameObject.AddComponent<AspectRatioFitter>();
            _bgFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            _bgFit.aspectRatio = 16f / 9f;

            // 시네마틱 표면(풀스크린 스트레치, 16:9 영상이라 왜곡 무시 가능)
            _video = NewUI("VideoSurface", _canvas.transform).gameObject.AddComponent<RawImage>();
            Stretch(_video.rectTransform);
            _video.color = Color.black;
            _video.raycastTarget = false;
            _video.gameObject.SetActive(false);

            // 전체 탭 캐처(커버 탭·시네마틱 탭스킵) — 아래 깔리고, 버튼들이 위에 와 클릭 우선
            var tap = CreateInvisibleButton(_canvas.transform, "TapCatcher");
            Stretch(tap.GetComponent<RectTransform>());
            tap.onClick.AddListener(() => { OnTap(); if (_video.gameObject.activeSelf) OnSkip(); });

            // 탭 힌트(하단)
            _hint = CreateText(_canvas.transform, "Hint", 46f);
            var hrt = _hint.rectTransform;
            hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0f);
            hrt.sizeDelta = new Vector2(1200f, 90f);
            hrt.anchoredPosition = new Vector2(0f, 120f);
            _hint.gameObject.SetActive(false);

            // 건너뛰기(우상단)
            _skipBtn = CreateButton(_canvas.transform, "SkipButton",
                new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(230f, 78f),
                BtnDim, "건너뛰기 ▶▶");
            _skipBtn.onClick.AddListener(OnSkip);
            _skipBtn.gameObject.SetActive(false);

            // 시작 버튼(하단 중앙) — 그룹으로 토글
            _startGroup = NewUI("StartGroup", _canvas.transform).gameObject;
            Stretch((RectTransform)_startGroup.transform);
            var start = CreateButton(_startGroup.transform, "StartButton",
                new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(540f, 132f),
                BtnGold, "▶  시작");
            start.onClick.AddListener(OnStart);
            _startGroup.SetActive(false);
        }

        // ---------- UI 헬퍼 ----------
        static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        TextMeshProUGUI CreateText(Transform parent, string name, float fontSize)
        {
            var rt = NewUI(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.enableWordWrapping = false;
            return t;
        }

        Button CreateButton(Transform parent, string name, Vector2 corner, Vector2 pos,
                            Vector2 size, Color color, string labelText)
        {
            var rt = NewUI(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            rt.anchorMin = rt.anchorMax = rt.pivot = corner;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var label = CreateText(rt, "Label", 44f);
            label.text = labelText;
            Stretch(label.rectTransform);
            return btn;
        }

        Button CreateInvisibleButton(Transform parent, string name)
        {
            var rt = NewUI(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);  // 투명하지만 raycast 수신(알파 무관)
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            return btn;
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
            }
        }
    }
}
