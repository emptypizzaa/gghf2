using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

namespace PaperHeroes
{
    /// <summary>
    /// 전투 중 HUD(런타임 UI 오버레이): 양 거점 HP 바 + 나가기(포기) 버튼·확인 팝업.
    /// 자원·용병 수/최대는 SummonController가 이미 표시하므로 중복하지 않는다.
    /// 씬 편집 없이 MatchManager.Start()가 AddComponent로 부착한다(헤드리스/재시작 세이프).
    /// 거점 참조는 매 프레임 match에서 fresh로 읽어 부착 타이밍·스테이지 주입과 무관하게 안전하다.
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        private MatchManager _match;

        private Canvas _canvas;
        private RectTransform _allyFill, _enemyFill;
        private TextMeshProUGUI _allyHpText, _enemyHpText;
        private GameObject _surrenderPopup;

        // 색감(MatchResultUI 톤과 일치)
        static readonly Color AllyColor  = new Color(0.40f, 0.85f, 0.50f);
        static readonly Color EnemyColor = new Color(0.92f, 0.42f, 0.42f);
        static readonly Color BarBg      = new Color(0.08f, 0.10f, 0.14f, 0.92f);
        static readonly Color PanelCol   = new Color(0.16f, 0.19f, 0.27f, 0.98f);
        static readonly Color BtnCol     = new Color(0.20f, 0.42f, 0.72f, 1f);
        static readonly Color BtnDanger  = new Color(0.72f, 0.26f, 0.26f, 1f);

        private void Awake()
        {
            _match = FindFirstObjectByType<MatchManager>();
        }

        private void OnEnable()
        {
            if (_match != null) _match.MatchEnded += OnMatchEnded;
        }

        private void OnDisable()
        {
            if (_match != null) _match.MatchEnded -= OnMatchEnded;
        }

        private void Start()
        {
            EnsureEventSystem();
            BuildUI();
            RefreshBars();
        }

        private void Update()
        {
            if (_canvas == null || !_canvas.gameObject.activeInHierarchy) return;
            RefreshBars();
        }

        // 결과 패널이 뜨면 전투 HUD(바·팝업)를 통째로 숨긴다(결과 Canvas는 sortingOrder 100으로 위에 옴).
        private void OnMatchEnded(MatchState _)
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        // ---------- 바 갱신 (매 프레임 fresh 읽기 — base는 match를 통해서만) ----------
        private void RefreshBars()
        {
            UpdateBar("아군 거점", _match != null ? _match.allyBase : null, _allyFill, _allyHpText);
            UpdateBar("적 거점",  _match != null ? _match.enemyBase : null, _enemyFill, _enemyHpText);
        }

        private static void UpdateBar(string label, BaseController b, RectTransform fill, TextMeshProUGUI text)
        {
            if (fill == null || text == null) return;
            if (b == null)
            {
                fill.anchorMax = new Vector2(0f, 1f);
                text.text = label + "  -";
                return;
            }
            float max = b.maxHp;
            float frac = max > 0f ? Mathf.Clamp01(b.CurrentHp / max) : 0f;
            fill.anchorMax = new Vector2(frac, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            text.text = label + "  " + Mathf.CeilToInt(b.CurrentHp) + " / " + Mathf.CeilToInt(max);
        }

        // ---------- 나가기/포기 ----------
        private void OnExitClicked()
        {
            // 이미 종료된 매치면 무시(결과 패널과 중복 방지).
            if (_match != null && _match.State != MatchState.Playing) return;
            if (_surrenderPopup == null) return;
            Time.timeScale = 0f;                       // 확인하는 동안 일시정지
            _surrenderPopup.transform.SetAsLastSibling();
            _surrenderPopup.SetActive(true);
        }

        private void OnCancelSurrender()
        {
            if (_surrenderPopup != null) _surrenderPopup.SetActive(false);
            Time.timeScale = 1f;                       // 일시정지 해제
        }

        private void OnConfirmSurrender()
        {
            if (_surrenderPopup != null) _surrenderPopup.SetActive(false);
            if (_match != null)
            {
                // 패배로 종료 → 기존 결과 패널(다시 시작) 재사용. timeScale은 EndMatch가 0으로 유지(다시 시작 시 1로 복구).
                _match.Concede();
            }
            else
            {
                // 폴백: 매니저가 없으면 현재 씬 리로드.
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        // ---------- 런타임 UI 빌드 ----------
        private void BuildUI()
        {
            var canvasGo = new GameObject("CombatHUDCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1;                  // 소환 UI(0) 위, 결과 패널(100) 아래
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            // 아군 거점 HP — 좌상단
            BuildBar(canvasGo.transform, new Vector2(0f, 1f), new Vector2(30f, -30f),
                     AllyColor, out _allyFill, out _allyHpText);
            // 적 거점 HP — 우상단(나가기 버튼 아래)
            BuildBar(canvasGo.transform, new Vector2(1f, 1f), new Vector2(-30f, -110f),
                     EnemyColor, out _enemyFill, out _enemyHpText);

            // 나가기 버튼 — 우상단 코너
            var exitBtn = CreateButton(canvasGo.transform, "ExitButton",
                new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(160f, 66f),
                BtnCol, "나가기", out _);
            exitBtn.onClick.AddListener(OnExitClicked);

            BuildSurrenderPopup(canvasGo.transform);
        }

        private void BuildBar(Transform parent, Vector2 corner, Vector2 pos, Color fillColor,
                              out RectTransform fill, out TextMeshProUGUI text)
        {
            const float w = 480f, h = 56f;
            // 배경
            var bgGo = new GameObject("Bar");
            bgGo.transform.SetParent(parent, false);
            var bg = bgGo.AddComponent<Image>();
            bg.color = BarBg;
            bg.raycastTarget = false;                  // HUD 바는 비상호작용(아래 소환 UI 클릭 방해 안 함)
            var brt = bg.rectTransform;
            brt.anchorMin = brt.anchorMax = brt.pivot = corner;
            brt.sizeDelta = new Vector2(w, h);
            brt.anchoredPosition = pos;

            // 채움(좌측 고정, 폭 = HP 비율 → anchorMax.x로 조절)
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.raycastTarget = false;
            fill = fillImg.rectTransform;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            // HP 텍스트(바 위 오버레이)
            text = CreateText(bgGo.transform, "HpText", 28f);
            text.raycastTarget = false;
            var trt = text.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }

        private void BuildSurrenderPopup(Transform parent)
        {
            _surrenderPopup = new GameObject("SurrenderPopup");
            var prt = _surrenderPopup.AddComponent<RectTransform>();
            prt.SetParent(parent, false);
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            // 전체 딤(뒤 클릭 차단)
            var dim = new GameObject("Dim").AddComponent<Image>();
            dim.transform.SetParent(_surrenderPopup.transform, false);
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            var drt = dim.rectTransform;
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = drt.offsetMax = Vector2.zero;

            // 패널
            var panel = new GameObject("Panel").AddComponent<Image>();
            panel.transform.SetParent(_surrenderPopup.transform, false);
            panel.color = PanelCol;
            var panrt = panel.rectTransform;
            panrt.anchorMin = panrt.anchorMax = panrt.pivot = new Vector2(0.5f, 0.5f);
            panrt.sizeDelta = new Vector2(720f, 360f);
            panrt.anchoredPosition = Vector2.zero;

            // 메시지
            var msg = CreateText(panel.transform, "Msg", 48f);
            msg.text = "매치를 포기하시겠습니까?";
            var mrt = msg.rectTransform;
            mrt.anchorMin = mrt.anchorMax = mrt.pivot = new Vector2(0.5f, 1f);
            mrt.sizeDelta = new Vector2(660f, 120f);
            mrt.anchoredPosition = new Vector2(0f, -70f);

            // 취소 / 포기 버튼(패널 하단 좌·우)
            var cancel = CreateButton(panel.transform, "Cancel",
                new Vector2(0.5f, 0f), new Vector2(-150f, 60f), new Vector2(260f, 110f),
                BtnCol, "취소", out _);
            cancel.onClick.AddListener(OnCancelSurrender);

            var confirm = CreateButton(panel.transform, "Confirm",
                new Vector2(0.5f, 0f), new Vector2(150f, 60f), new Vector2(260f, 110f),
                BtnDanger, "포기", out _);
            confirm.onClick.AddListener(OnConfirmSurrender);

            _surrenderPopup.SetActive(false);
        }

        // ---------- 헬퍼 ----------
        private Button CreateButton(Transform parent, string name, Vector2 corner, Vector2 pos,
                                    Vector2 size, Color color, string labelText, out TextMeshProUGUI label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = corner;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            label = CreateText(go.transform, "Label", 38f);
            label.text = labelText;
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            return btn;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = false;
            return t;
        }

        private void EnsureEventSystem()
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
