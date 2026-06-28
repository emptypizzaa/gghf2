using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace PaperHeroes
{
    /// <summary>
    /// 학교 락커룸/교실 테마의 정적 로비. 인트로 다음·전투 시작 전 허브 — 로스터 영웅을 골라
    /// 분장(외형 색)과 무장(무기 프리셋)을 커스터마이즈하고 "전투 시작"으로 전투를 개시한다.
    /// 선택은 PlayerPrefs(LoadoutStore)에 영속화되어, UnitSpawner가 아군 스폰 시 색 틴트 + 스탯 보정을 적용한다.
    ///
    /// ★전투 게이트: Awake에서 Time.timeScale=0(인트로와 동일한 검증된 방식)으로 전투를 멈춰 두고
    ///   "전투 시작" 클릭 시 1로 복구한다. 첫 실행엔 인트로(sortingOrder 200)가 로비(150) 위를 덮고,
    ///   인트로 종료 시 게이트를 로비가 이어받는다. 매치 종료 후 "로비로"가 씬을 리로드 → 로비가 다시 게이트.
    /// 코루틴(페이드인)은 게이트 중(timeScale=0)이라 Time.unscaledDeltaTime 기반으로 대기한다.
    /// 모든 UI는 런타임 코드 생성(씬 편집 없음). 한글 렌더 위해 NanumGothic SDF를 명시 할당한다.
    /// </summary>
    public class LobbyController : MonoBehaviour
    {
        // 배경 후보(Resources/Story, 확장자 없이) — 앞에서부터 로드되는 첫 이미지를 교실/락커룸 배경으로.
        static readonly string[] BgCandidates = { "Story/lobby_bg", "Story/prologue_01", "Story/title_bg" };
        const float FadeDur = 0.45f;

        // 한글 폰트(TMP 기본도 NanumGothic이지만, TMP Settings 변경에 견고하도록 명시 할당).
        static TMP_FontAsset _krFont;
        static TMP_FontAsset KrFont => _krFont != null ? _krFont
            : (_krFont = Resources.Load<TMP_FontAsset>("Fonts/NanumGothic SDF"));

        Canvas _canvas;
        CanvasGroup _group;
        BgmController _bgm;
        TextMeshProUGUI _bgmLabel;
        bool _entered;   // 전투 시작됨(중복 진입 방지)

        // 락커룸 상태
        UnitData[] _roster;
        int _selectedIndex;
        Button[] _rosterButtons;
        RectTransform _detailRoot;   // 프리뷰/스탯/분장/무장 — 선택·변경 때마다 통째로 재구축
        LobbyDiorama _diorama;       // 대기 영웅 3D 디오라마(RenderTexture)

        static readonly Color Dark    = new Color(0.02f, 0.02f, 0.04f, 1f);
        static readonly Color Scrim   = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color BtnGold = new Color(0.85f, 0.62f, 0.18f, 1f);
        static readonly Color BtnDim  = new Color(0f, 0f, 0f, 0.5f);

        void Awake()
        {
            Time.timeScale = 0f; // 전투 게이트. EnterBattle()에서 1로 복구.
        }

        void Start()
        {
            _bgm = BgmController.Instance != null ? BgmController.Instance : FindFirstObjectByType<BgmController>();
            EnsureEventSystem();
            BuildUI();
            StartCoroutine(FadeIn());
        }

        void OnDestroy()
        {
            // 전투 시작 없이 외부 요인으로 파괴되면 전투가 멈춘 채 남지 않게 timeScale 복구.
            if (!_entered && Time.timeScale == 0f) Time.timeScale = 1f;
            // 디오라마(RT·카메라·리그) 해제 — EnterBattle(Destroy this)·씬 리로드 양쪽서 호출됨.
            if (_diorama != null) { Destroy(_diorama.gameObject); _diorama = null; }
        }

        // ---------- 동작 ----------
        /// <summary>"전투 시작" — 게이트 해제 후 로비 오버레이 제거. 전투는 이미 씬에 구성돼 있어 즉시 진행된다.</summary>
        void EnterBattle()
        {
            if (_entered) return;
            _entered = true;
            Time.timeScale = 1f;
            if (_canvas != null) Destroy(_canvas.gameObject);
            Destroy(this);
        }

        void ToggleBgm()
        {
            if (_bgm == null) return;
            bool muted = _bgm.ToggleMute();
            if (_bgmLabel != null) _bgmLabel.text = muted ? "♪ BGM 꺼짐" : "♪ BGM 켜짐";
        }

        void SelectUnit(int i)
        {
            _selectedIndex = i;
            Refresh();
        }

        // ---------- 런타임 UI 빌드 ----------
        void BuildUI()
        {
            var go = new GameObject("LobbyCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 150;                          // HUD(1)/소환(0)/결과(100) 위, 인트로(200) 아래
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            go.AddComponent<GraphicRaycaster>();
            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;                                   // FadeIn에서 1로

            // 베이스 — 여백 채움 + 뒤(전투 HUD) 클릭 차단(로비는 모달).
            var baseImg = NewUI("Base", _canvas.transform).gameObject.AddComponent<Image>();
            baseImg.color = Dark;
            Stretch(baseImg.rectTransform);
            baseImg.raycastTarget = true;

            // 교실/락커룸 배경 — 화면을 채우도록 Envelope(여백 없이 가장자리 크롭).
            var bg = NewUI("Bg", _canvas.transform).gameObject.AddComponent<RawImage>();
            Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            var tex = LoadFirst(BgCandidates);
            if (tex != null)
            {
                bg.texture = tex;
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = tex.height > 0 ? (float)tex.width / tex.height : 16f / 9f;
            }
            else
            {
                bg.color = new Color(0.10f, 0.12f, 0.18f, 1f);  // 이미지 없을 때 폴백 단색
            }

            // 어둑한 오버레이 — 배경 위 UI 가독성(전체 살짝 어둡게).
            var veil = NewUI("Veil", _canvas.transform).gameObject.AddComponent<Image>();
            Stretch(veil.rectTransform);
            veil.color = new Color(0f, 0f, 0f, 0.35f);
            veil.raycastTarget = false;

            // 타이틀(상단 중앙)
            var title = CreateText(_canvas.transform, "Title", 90f);
            title.text = "전투 준비 · 락커룸";
            title.fontStyle = FontStyles.Bold;
            var trt = title.rectTransform;
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(1600f, 150f);
            trt.anchoredPosition = new Vector2(0f, -56f);

            // 스테이지 라벨(상단 중앙, 타이틀 아래)
            var sub = CreateText(_canvas.transform, "StageLabel", 46f);
            string stageName = ResolveStageName();
            sub.text = string.IsNullOrEmpty(stageName) ? "교실" : "교실 · " + stageName;
            sub.color = new Color(1f, 0.95f, 0.8f, 0.95f);
            var srt = sub.rectTransform;
            srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(1400f, 64f);
            srt.anchoredPosition = new Vector2(0f, -196f);

            // BGM 토글(우상단)
            var bgmBtn = CreateButton(_canvas.transform, "BgmToggle",
                new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(300f, 84f),
                BtnDim, "", 40f);
            _bgmLabel = bgmBtn.GetComponentInChildren<TextMeshProUGUI>();
            _bgmLabel.text = (_bgm != null && _bgm.IsMuted) ? "♪ BGM 꺼짐" : "♪ BGM 켜짐";
            bgmBtn.onClick.AddListener(ToggleBgm);

            // 전투 시작(하단 중앙)
            var start = CreateButton(_canvas.transform, "StartBattle",
                new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(560f, 132f),
                BtnGold, "▶  전투 시작", 56f);
            start.onClick.AddListener(EnterBattle);

            // 로스터 로드(분장/무장 대상) — 전투가 실제로 쓰는 것과 동일 소스(MatchManager.stage, 없으면 Stage1).
            var stage = ResolveStage();
            _roster = (stage != null && stage.roster != null) ? stage.roster : new UnitData[0];

            // 좌측 "용병 선택" 칼럼
            var rosterHeader = CreateText(_canvas.transform, "RosterHeader", 40f);
            rosterHeader.text = "용병 선택";
            rosterHeader.fontStyle = FontStyles.Bold;
            var rh = rosterHeader.rectTransform;
            rh.anchorMin = rh.anchorMax = rh.pivot = new Vector2(0.5f, 0.5f);
            rh.sizeDelta = new Vector2(360f, 56f);
            rh.anchoredPosition = new Vector2(-700f, 332f);

            _rosterButtons = new Button[_roster.Length];
            for (int i = 0; i < _roster.Length; i++)
            {
                int idx = i;
                var d = _roster[i];
                var b = CreateButton(_canvas.transform, "Roster_" + i,
                    new Vector2(0.5f, 0.5f), new Vector2(-700f, 230f - i * 118f), new Vector2(360f, 100f),
                    BtnDim, (d != null ? d.displayName : "-") + "\n" + RoleKo(d), 30f);
                b.onClick.AddListener(() => SelectUnit(idx));
                _rosterButtons[i] = b;
            }

            // 동적 디테일(프리뷰/스탯/분장/무장) 루트 — 전체 스트레치(자식은 중앙 기준 anchoredPosition).
            _detailRoot = NewUI("Detail", _canvas.transform);
            Stretch(_detailRoot);

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _roster.Length - 1));

            // 대기 영웅 디오라마(화면 밖 리그 → RenderTexture). Refresh가 RT를 RawImage에 바인딩한다.
            if (_roster != null && _roster.Length > 0)
            {
                var dgo = new GameObject("LobbyDiorama");
                _diorama = dgo.AddComponent<LobbyDiorama>();
                _diorama.Build(_roster, _selectedIndex);
            }

            Refresh();
        }

        /// <summary>선택 영웅의 프리뷰/스탯 + 분장 그리드 + 무장 리스트를 현재 저장값 기준으로 재구축한다.</summary>
        void Refresh()
        {
            // 로스터 버튼 하이라이트
            if (_rosterButtons != null)
                for (int i = 0; i < _rosterButtons.Length; i++)
                    if (_rosterButtons[i] != null && _rosterButtons[i].targetGraphic is Image img)
                        img.color = (i == _selectedIndex) ? BtnGold : BtnDim;

            if (_detailRoot == null) return;
            for (int c = _detailRoot.childCount - 1; c >= 0; c--) Destroy(_detailRoot.GetChild(c).gameObject);

            if (_roster == null || _roster.Length == 0) return;
            UnitData d = _roster[_selectedIndex];
            if (d == null) return;

            string unitId = LoadoutStore.UnitId(d);
            string cosId = LoadoutStore.GetCosmeticId(unitId);
            string wpnId = LoadoutStore.GetWeaponId(unitId);
            CosmeticSwatch cos = LoadoutCatalog.CosmeticById(cosId);
            WeaponPreset wpn = LoadoutCatalog.WeaponById(wpnId);

            // ----- 중앙: 대기 영웅 디오라마(RenderTexture) + 이름/역할 + 스탯 -----
            // RT를 교실 배경 위에 합성. 영웅 선택/분장 변경이 Refresh를 타므로 여기서 디오라마도 갱신.
            if (_diorama != null) { _diorama.SetSelected(_selectedIndex); _diorama.SyncTints(); }

            var preview = NewUI("Diorama", _detailRoot).gameObject.AddComponent<RawImage>();
            preview.texture = _diorama != null ? _diorama.Texture : null;
            preview.raycastTarget = false;
            var chrt = preview.rectTransform;       // 16:9 (시작값 — 라이브 튜닝 대상)
            chrt.anchorMin = chrt.anchorMax = chrt.pivot = new Vector2(0.5f, 0.5f);
            chrt.sizeDelta = new Vector2(720f, 405f);
            chrt.anchoredPosition = new Vector2(-110f, 150f);
            if (_diorama == null)                    // 디오라마 빌드 실패 시 폴백 색칩
            {
                preview.color = (cos.id == LoadoutCatalog.DefaultCosmeticId) ? d.prototypeColor : cos.tint;
            }

            var nameTxt = CreateText(_detailRoot, "Name", 50f);
            nameTxt.text = d.displayName;
            nameTxt.fontStyle = FontStyles.Bold;
            PlaceCentered(nameTxt.rectTransform, new Vector2(-110f, -90f), new Vector2(700f, 60f));

            var roleTxt = CreateText(_detailRoot, "Role", 34f);
            roleTxt.text = RoleKo(d);
            roleTxt.color = new Color(0.8f, 0.85f, 0.95f, 1f);
            PlaceCentered(roleTxt.rectTransform, new Vector2(-110f, -136f), new Vector2(700f, 44f));

            // 스탯(무장 보정 반영) — Combatant 생성 없이 data × 프리셋 배수로 계산.
            string atkLabel = d.isHealer ? "회복량" : "공격력";
            float spd = (d.attackInterval * wpn.intervalMul) > 0.0001f ? 1f / (d.attackInterval * wpn.intervalMul) : 0f;
            string stats =
                $"체력        {d.maxHp * wpn.hpMul:0}\n" +
                $"{atkLabel}      {d.attackDamage * wpn.damageMul:0}\n" +
                $"사거리      {d.attackRange * wpn.rangeMul:0.0}\n" +
                $"공격속도   {spd:0.00}/초\n" +
                $"방어        {d.defense + wpn.defenseBonus:0}";
            var statTxt = CreateText(_detailRoot, "Stats", 32f);
            statTxt.text = stats;
            statTxt.alignment = TextAlignmentOptions.Left;
            PlaceCentered(statTxt.rectTransform, new Vector2(-110f, -250f), new Vector2(420f, 210f));

            // ----- 우상단: 분장(외형) 견본 그리드 4×2 -----
            var cosHeader = CreateText(_detailRoot, "CosHeader", 40f);
            cosHeader.text = "분장 (외형)";
            cosHeader.fontStyle = FontStyles.Bold;
            PlaceCentered(cosHeader.rectTransform, new Vector2(610f, 366f), new Vector2(620f, 56f));

            for (int i = 0; i < LoadoutCatalog.Cosmetics.Length; i++)
            {
                var sw = LoadoutCatalog.Cosmetics[i];
                int col = i % 4, row = i / 4;
                var pos = new Vector2(380f + col * 150f, 256f - row * 168f);
                CreateSwatch(_detailRoot, pos, sw, sw.id == cosId, unitId);
            }

            // ----- 우하단: 무장(장비) 리스트 -----
            var wpnHeader = CreateText(_detailRoot, "WpnHeader", 40f);
            wpnHeader.text = "무장 (장비)";
            wpnHeader.fontStyle = FontStyles.Bold;
            PlaceCentered(wpnHeader.rectTransform, new Vector2(610f, -36f), new Vector2(620f, 56f));

            var presets = LoadoutCatalog.WeaponsForRole(d.role);
            for (int i = 0; i < presets.Count; i++)
            {
                var p = presets[i];
                string pid = p.id;
                var b = CreateButton(_detailRoot, "Wpn_" + p.id,
                    new Vector2(0.5f, 0.5f), new Vector2(610f, -116f - i * 104f), new Vector2(470f, 92f),
                    (p.id == wpnId) ? BtnGold : BtnDim, p.displayName + "\n" + p.flavor, 28f);
                b.onClick.AddListener(() => { LoadoutStore.SetWeaponId(unitId, pid); Refresh(); });
            }
        }

        /// <summary>분장 견본 버튼(색 칩 + 아래 라벨 + 선택 시 금색 아웃라인).</summary>
        void CreateSwatch(Transform parent, Vector2 pos, CosmeticSwatch sw, bool selected, string unitId)
        {
            var rt = NewUI("Sw_" + sw.id, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(122f, 122f);
            rt.anchoredPosition = pos;
            var img = rt.gameObject.AddComponent<Image>();
            // "기본"은 색이 흰색(틴트 없음)이라 중립 회색 칩으로 표시.
            img.color = sw.id == LoadoutCatalog.DefaultCosmeticId ? new Color(0.5f, 0.5f, 0.52f, 1f) : sw.tint;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            string sid = sw.id;
            btn.onClick.AddListener(() => { LoadoutStore.SetCosmeticId(unitId, sid); Refresh(); });

            if (selected)
            {
                var ol = rt.gameObject.AddComponent<Outline>();
                ol.effectColor = BtnGold;
                ol.effectDistance = new Vector2(6f, 6f);
            }

            var label = CreateText(rt, "L", 22f);
            label.text = sw.displayName;
            var lrt = label.rectTransform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.sizeDelta = new Vector2(150f, 34f);
            lrt.anchoredPosition = new Vector2(0f, -4f);
        }

        IEnumerator FadeIn()
        {
            float t = 0f;
            while (t < FadeDur)
            {
                t += Time.unscaledDeltaTime;
                if (_group != null) _group.alpha = Mathf.Clamp01(t / FadeDur);
                yield return null;
            }
            if (_group != null) _group.alpha = 1f;
        }

        /// <summary>역할 한글 라벨(힐러 플래그 우선, 그 외 role).</summary>
        static string RoleKo(UnitData d)
        {
            if (d == null) return "";
            if (d.isHealer) return "힐러";
            switch (d.role)
            {
                case UnitRole.Tank:   return "탱커";
                case UnitRole.Ranged: return "원거리";
                case UnitRole.Healer: return "힐러";
                default:              return "근접";
            }
        }

        /// <summary>전투(MatchManager)가 쓰는 스테이지와 동일 소스. 인스펙터 override(MatchManager.stage)를 우선, 없으면 Stage1 폴백.</summary>
        StageData ResolveStage()
        {
            var mm = FindFirstObjectByType<MatchManager>();
            if (mm != null && mm.stage != null) return mm.stage;
            return Resources.Load<StageData>("Stage1");
        }

        string ResolveStageName()
        {
            var stage = ResolveStage();
            return stage != null ? stage.displayName : null;
        }

        static Texture2D LoadFirst(string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                var t = Resources.Load<Texture2D>(paths[i]);
                if (t != null) return t;
            }
            return null;
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

        // 중앙(0.5,0.5) 기준 위치/크기 배치 헬퍼(디테일 자식 공통).
        static void PlaceCentered(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        TextMeshProUGUI CreateText(Transform parent, string name, float fontSize)
        {
            var rt = NewUI(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (KrFont != null) t.font = KrFont;   // 한글 렌더 보장(TMP 기본 변경에도 견고)
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.enableWordWrapping = false;
            return t;
        }

        Button CreateButton(Transform parent, string name, Vector2 corner, Vector2 pos,
                            Vector2 size, Color color, string labelText, float labelSize)
        {
            var rt = NewUI(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            rt.anchorMin = rt.anchorMax = rt.pivot = corner;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var label = CreateText(rt, "Label", labelSize);
            label.text = labelText;
            Stretch(label.rectTransform);
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
