using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace PaperHeroes
{
    /// <summary>
    /// 플레이어 소환 컨트롤러 + 런타임 UI. 코어 루프의 "용병을 소환"(비용 차감 + 유닛별 쿨다운).
    /// UI(Canvas/버튼/텍스트)는 코드로 생성한다 — uGUI onClick의 직렬화 와이어링을 피하고
    /// 데이터(로스터)로부터 버튼을 만든다. (유예: 런타임 UI라 아티스트가 에디터에서 편집 불가)
    /// 로스터/참조는 에디터에서 주입(런타임 AssetDatabase 미사용).
    /// </summary>
    public class SummonController : MonoBehaviour
    {
        public UnitData[] roster;
        public Economy economy;
        public UnitSpawner spawner;
        public Faction faction = Faction.Ally;

        private float[] _cooldownRemaining;
        private TextMeshProUGUI _moneyText;
        private Button[] _buttons;
        private TextMeshProUGUI[] _labels;

        private void Awake()
        {
            if (economy == null) economy = FindFirstObjectByType<Economy>();
            if (spawner == null) spawner = FindFirstObjectByType<UnitSpawner>();
        }

        private void Start()
        {
            _cooldownRemaining = new float[roster != null ? roster.Length : 0];
            BuildUI();
            RefreshUI();
        }

        private void Update()
        {
            for (int i = 0; i < _cooldownRemaining.Length; i++)
                if (_cooldownRemaining[i] > 0f) _cooldownRemaining[i] -= Time.deltaTime;

            RefreshUI();
        }

        public bool CanSummon(int index)
        {
            if (roster == null || index < 0 || index >= roster.Length) return false;
            UnitData d = roster[index];
            return d != null
                && _cooldownRemaining[index] <= 0f
                && economy != null && economy.CanAfford(d.cost);
        }

        public bool TrySummon(int index)
        {
            if (!CanSummon(index)) return false;
            UnitData d = roster[index];
            if (!economy.TrySpend(d.cost)) return false;

            if (spawner != null) spawner.SpawnUnit(d, faction);
            _cooldownRemaining[index] = d.summonCooldown;
            return true;
        }

        // ---------- 런타임 UI ----------

        private void BuildUI()
        {
            // EventSystem (없으면 생성). 새 Input System 전용 프로젝트 → InputSystemUIInputModule.
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                var module = esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                module.AssignDefaultActions(); // 런타임 추가 시 기본 UI 액션 할당(클릭 동작 보장)
            }

            var canvasGo = new GameObject("SummonCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            // 자원 텍스트 (상단 중앙)
            _moneyText = CreateText(canvasGo.transform, "MoneyText", 56f);
            var mrt = _moneyText.rectTransform;
            mrt.anchorMin = mrt.anchorMax = mrt.pivot = new Vector2(0.5f, 1f);
            mrt.sizeDelta = new Vector2(600f, 90f);
            mrt.anchoredPosition = new Vector2(0f, -70f);

            // 버튼들 (하단 중앙)
            int n = roster != null ? roster.Length : 0;
            _buttons = new Button[n];
            _labels = new TextMeshProUGUI[n];
            float bw = 300f, bh = 150f, gap = 36f;
            float totalW = n * bw + (n - 1) * gap;
            for (int i = 0; i < n; i++)
            {
                float x = -totalW / 2f + bw / 2f + i * (bw + gap);
                int idx = i;
                _buttons[i] = CreateButton(canvasGo.transform, "Summon_" + i,
                    new Vector2(x, 110f), new Vector2(bw, bh), out _labels[i]);
                _buttons[i].onClick.AddListener(() => TrySummon(idx));
            }
        }

        private void RefreshUI()
        {
            if (_moneyText != null && economy != null)
                _moneyText.text = "돈  " + Mathf.FloorToInt(economy.CurrentMoney);

            if (_buttons == null) return;
            for (int i = 0; i < _buttons.Length; i++)
            {
                UnitData d = roster[i];
                _buttons[i].interactable = CanSummon(i);
                string cd = _cooldownRemaining[i] > 0f
                    ? "\n쿨 " + _cooldownRemaining[i].ToString("F1") + "s"
                    : "";
                _labels[i].text = (d != null ? d.displayName : "-") + "\n$" + (d != null ? (int)d.cost : 0) + cd;
            }
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

        private Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, out TextMeshProUGUI label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.32f, 0.96f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            label = CreateText(go.transform, "Label", 30f);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            return btn;
        }
    }
}
