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

        [Header("배치 제한 (설계 8번)")]
        [Tooltip("초기 최대 동시 배치 용병 수")]
        public int initialMaxUnits = 5;
        [Tooltip("최대 용병 수 +1 증가 비용(꿈에너지)")]
        public float slotIncreaseCost = 300f;

        private int _maxUnits;
        private float[] _cooldownRemaining;
        private TextMeshProUGUI _moneyText;
        private TextMeshProUGUI _unitCountText;
        private Button[] _buttons;
        private TextMeshProUGUI[] _labels;
        private Button[] _upgradeButtons;
        private TextMeshProUGUI[] _upgradeLabels;
        private Button _slotButton;
        private TextMeshProUGUI _slotLabel;

        private void Awake()
        {
            if (economy == null) economy = FindFirstObjectByType<Economy>();
            if (spawner == null) spawner = FindFirstObjectByType<UnitSpawner>();
        }

        private void Start()
        {
            _maxUnits = Mathf.Max(1, initialMaxUnits);
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

        /// <summary>현재 살아있는 아군(this.faction) 용병 수. (설계 8번 배치 제한 판정용)</summary>
        public int AllyUnitCount
        {
            get
            {
                int n = 0;
                var all = Targetables.All;
                for (int i = 0; i < all.Count; i++)
                {
                    var c = all[i] as Combatant;
                    if (c != null && c.faction == faction && !c.IsDead) n++;
                }
                return n;
            }
        }

        public int MaxUnits => _maxUnits;

        public bool CanSummon(int index)
        {
            if (roster == null || index < 0 || index >= roster.Length) return false;
            UnitData d = roster[index];
            if (d == null || _cooldownRemaining[index] > 0f) return false;
            if (economy == null || !economy.CanAfford(d.cost)) return false;
            // 소환은 항상 새 유닛 추가 → 배치 캡 적용(승급은 별도 강화 버튼 TryUpgrade로 분리).
            if (AllyUnitCount >= _maxUnits) return false;
            return true;
        }

        /// <summary>같은 UnitData의 승급 가능(Tier&lt;3) 아군 중 가장 낮은 티어. 없으면 null. (설계 12번 머지)</summary>
        private Combatant FindPromotable(UnitData d)
        {
            Combatant best = null;
            var all = Targetables.All;
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i] as Combatant;
                if (c == null || c.faction != faction || c.IsDead) continue;
                if (c.data != d || c.Tier >= 3) continue;
                if (best == null || c.Tier < best.Tier) best = c;
            }
            return best;
        }

        public bool CanIncreaseSlot() => economy != null && economy.CanAfford(slotIncreaseCost);

        /// <summary>최대 배치 수 +1 (비용 차감). (설계 8번: 증가 비용 300, 상한 없음)</summary>
        public bool TryIncreaseSlot()
        {
            if (!CanIncreaseSlot()) return false;
            if (!economy.TrySpend(slotIncreaseCost)) return false;
            _maxUnits++;
            return true;
        }

        public bool TrySummon(int index)
        {
            if (!CanSummon(index)) return false;
            UnitData d = roster[index];
            if (!economy.TrySpend(d.cost)) return false;

            // 소환은 항상 새 유닛 추가(승급은 TryUpgrade로 분리). (설계 8번 배치 캡 적용)
            if (spawner != null) spawner.SpawnUnit(d, faction);

            _cooldownRemaining[index] = d.summonCooldown;
            return true;
        }

        /// <summary>강화(승급) 가능? 같은 유닛의 승급 가능(Tier&lt;3) 아군이 있고 비용을 감당할 수 있으면 true. (배치 캡 무관 — 새 유닛 안 생김)</summary>
        public bool CanUpgrade(int index)
        {
            if (roster == null || index < 0 || index >= roster.Length) return false;
            UnitData d = roster[index];
            if (d == null) return false;
            if (economy == null || !economy.CanAfford(d.cost)) return false;
            return FindPromotable(d) != null;
        }

        /// <summary>강화: 같은 유닛 중 가장 낮은 티어 아군을 1단계 승급(비용 차감). 새 유닛은 안 생긴다. (설계 12번 머지 — 트리거를 소환에서 분리)</summary>
        public bool TryUpgrade(int index)
        {
            if (!CanUpgrade(index)) return false;
            UnitData d = roster[index];
            Combatant promo = FindPromotable(d);
            if (promo == null) return false;
            if (!economy.TrySpend(d.cost)) return false;
            promo.TryPromote();
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

            // 버튼들 (하단 중앙) — 소환 버튼(크게) + 바로 위 강화 버튼(작게, per-type)
            int n = roster != null ? roster.Length : 0;
            _buttons = new Button[n];
            _labels = new TextMeshProUGUI[n];
            _upgradeButtons = new Button[n];
            _upgradeLabels = new TextMeshProUGUI[n];
            float bw = 300f, bh = 150f, gap = 36f, ubh = 64f;
            float totalW = n * bw + (n - 1) * gap;
            for (int i = 0; i < n; i++)
            {
                float x = -totalW / 2f + bw / 2f + i * (bw + gap);
                int idx = i;
                // 소환 버튼(하단) — 항상 새 유닛 추가
                _buttons[i] = CreateButton(canvasGo.transform, "Summon_" + i,
                    new Vector2(x, 110f), new Vector2(bw, bh), out _labels[i]);
                _buttons[i].onClick.AddListener(() => TrySummon(idx));
                // 강화 버튼(소환 버튼 바로 위, 작게) — 승급을 소환과 분리. 호박색으로 구분.
                _upgradeButtons[i] = CreateButton(canvasGo.transform, "Upgrade_" + i,
                    new Vector2(x, 110f + bh + 12f), new Vector2(bw, ubh), out _upgradeLabels[i]);
                _upgradeButtons[i].onClick.AddListener(() => TryUpgrade(idx));
                if (_upgradeButtons[i].targetGraphic is Image uimg)
                    uimg.color = new Color(0.34f, 0.28f, 0.12f, 0.96f);
            }

            // 용병 수 / 최대 표시 (자원 텍스트 아래)
            _unitCountText = CreateText(canvasGo.transform, "UnitCountText", 40f);
            var urt = _unitCountText.rectTransform;
            urt.anchorMin = urt.anchorMax = urt.pivot = new Vector2(0.5f, 1f);
            urt.sizeDelta = new Vector2(600f, 60f);
            urt.anchoredPosition = new Vector2(0f, -168f);

            // 용병 수 증가 버튼 (소환 버튼 행 우측 — 설계 5번/8번)
            float slotW = 230f;
            _slotButton = CreateButton(canvasGo.transform, "IncreaseSlot",
                new Vector2(totalW / 2f + gap + slotW / 2f, 110f), new Vector2(slotW, bh), out _slotLabel);
            _slotButton.onClick.AddListener(() => TryIncreaseSlot());
        }

        private void RefreshUI()
        {
            if (_moneyText != null && economy != null)
                _moneyText.text = "돈  " + Mathf.FloorToInt(economy.CurrentMoney);

            if (_unitCountText != null)
                _unitCountText.text = "용병  " + AllyUnitCount + " / " + _maxUnits;

            if (_slotButton != null)
            {
                _slotButton.interactable = CanIncreaseSlot();
                _slotLabel.text = "용병 수 +1\n$" + (int)slotIncreaseCost;
            }

            if (_buttons == null) return;
            for (int i = 0; i < _buttons.Length; i++)
            {
                UnitData d = roster[i];
                _buttons[i].interactable = CanSummon(i);
                string cd = _cooldownRemaining[i] > 0f
                    ? "\n쿨 " + _cooldownRemaining[i].ToString("F1") + "s"
                    : "";
                _labels[i].text = (d != null ? d.displayName : "-") + "\n$" + (d != null ? (int)d.cost : 0) + cd;

                if (_upgradeButtons != null && _upgradeButtons[i] != null)
                {
                    _upgradeButtons[i].interactable = CanUpgrade(i);
                    _upgradeLabels[i].text = "강화\n$" + (d != null ? (int)d.cost : 0);
                }
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
