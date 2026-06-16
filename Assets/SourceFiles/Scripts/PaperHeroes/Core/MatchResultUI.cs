using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

namespace PaperHeroes
{
    /// <summary>
    /// 매치 종료 시 VICTORY/DEFEAT 결과 패널과 RESTART 버튼을 띄운다(런타임 UI 오버레이).
    /// RESTART는 timeScale 복구 후 현재 씬을 리로드해 상태를 깨끗이 초기화한다.
    /// (씬은 Build Settings에 등록되어 있어야 LoadScene 가능.)
    /// </summary>
    public class MatchResultUI : MonoBehaviour
    {
        private MatchManager _match;
        private bool _shown;

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

        private void OnMatchEnded(MatchState result)
        {
            if (_shown) return;
            _shown = true;
            BuildPanel(result == MatchState.Won);
        }

        private void BuildPanel(bool won)
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("ResultCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 게임/소환 UI 위에 오버레이
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            // 전체 화면 딤(뒤 게임 클릭 차단)
            var dim = new GameObject("Dim").AddComponent<Image>();
            dim.transform.SetParent(canvasGo.transform, false);
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            var drt = dim.rectTransform;
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = drt.offsetMax = Vector2.zero;

            // 결과 텍스트
            var txt = CreateText(canvasGo.transform, won ? "승리" : "패배", 120f);
            txt.color = won ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.45f, 0.45f);
            var trt = txt.rectTransform;
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(1000f, 220f);
            trt.anchoredPosition = new Vector2(0f, 90f);

            // RESTART 버튼
            var btnGo = new GameObject("RestartButton");
            btnGo.transform.SetParent(canvasGo.transform, false);
            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.2f, 0.42f, 0.72f, 1f);
            var brt = img.rectTransform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(380f, 120f);
            brt.anchoredPosition = new Vector2(0f, -130f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(Restart);
            var label = CreateText(btnGo.transform, "다시 시작", 46f);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            Scene s = SceneManager.GetActiveScene();
            SceneManager.LoadScene(s.buildIndex);
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

        private TextMeshProUGUI CreateText(Transform parent, string content, float fontSize)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = fontSize;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = false;
            t.color = Color.white;
            return t;
        }
    }
}
