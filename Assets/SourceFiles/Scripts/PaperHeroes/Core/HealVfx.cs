using UnityEngine;
using TMPro;

namespace PaperHeroes
{
    /// <summary>
    /// 힐러 "보이는 힐" 코스메틱 연출 — 힐 적용 로직·수치와 완전 디커플(위치·수치만 받아 시각만 낸다).
    /// 힐러 staff → 대상 머리로 날아가는 초록 회복 오브(글로우+트레일). 도달 시 대상 머리 위 "+N"(HealNumber).
    /// 핵심: Play 시점에 양 끝점을 Vector3로 **스냅샷** → 비행 중 외부 Combatant 참조 0 →
    /// 타겟이 비행 중 죽어도 MissingReferenceException 구조적으로 불가능. 씬 편집 없이 코드 생성(.glb 모델 비의존).
    /// </summary>
    public class HealVfx : MonoBehaviour
    {
        const float FlightDur = 0.25f;
        static readonly Color HealColor = new Color(0.4f, 1f, 0.5f);

        const float BlinkPeriod = 0.045f;                        // ~11Hz 점멸

        private Vector3 _from, _to;
        private int _amount;
        private float _age;
        private Renderer[] _barRenderers;                        // 적십자 막대만(트레일 제외 — 직선 자취는 끊기지 않게)

        public static void Play(Vector3 from, Vector3 to, int amount)
        {
            var go = new GameObject("HealOrb");
            go.transform.position = from;

            // 초록 적십자(+) — 세로·가로 얇은 큐브 2개(Emission 초록 글로우)
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", HealColor);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", HealColor * 2.5f);    // 글로우(×2.5 — 색이 흰색으로 날아가지 않게)
            MakeBar(go.transform, mat, new Vector3(0.22f, 0.78f, 0.09f)); // 세로 막대(크게 — 적십자로 또렷이)
            MakeBar(go.transform, mat, new Vector3(0.78f, 0.22f, 0.09f)); // 가로 막대

            // 적십자가 정면으로 읽히게 카메라 향해 1회 정렬. Camera.main은 씬 카메라 Untagged라 null → 폴백.
            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam != null) go.transform.rotation = Quaternion.LookRotation(go.transform.position - cam.transform.position);

            // 직선 비행 자취(초록 트레일) — "쏘는" 직선이 보이게(점멸 안 함, 끊김 방지)
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.16f;
            trail.startWidth = 0.16f;
            trail.endWidth = 0f;
            trail.numCapVertices = 2;
            trail.material = mat;
            trail.startColor = HealColor;
            trail.endColor = new Color(HealColor.r, HealColor.g, HealColor.b, 0f);

            var fx = go.AddComponent<HealVfx>();
            fx._from = from;
            fx._to = to;
            fx._amount = amount;
            fx._barRenderers = go.GetComponentsInChildren<MeshRenderer>(); // 큐브 막대만(트레일 제외)
        }

        // 적십자 막대 1개(콜라이더 제거, 공유 머티리얼) — 부모에 부착.
        private static void MakeBar(Transform parent, Material mat, Vector3 scale)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Bar";
            var col = bar.GetComponent<Collider>();
            if (col != null) Destroy(col);
            bar.transform.SetParent(parent, false);
            bar.transform.localScale = scale;
            var r = bar.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / FlightDur);
            transform.position = Vector3.Lerp(_from, _to, t);   // 직선 비행(고정 두 점 — 외부 참조 0)

            // 점멸(blink) — "회복되는 느낌". 적십자 막대만 깜빡(트레일은 유지돼 직선 자취가 이어짐).
            bool on = (Mathf.FloorToInt(_age / BlinkPeriod) & 1) == 0;
            if (_barRenderers != null)
                for (int i = 0; i < _barRenderers.Length; i++)
                    if (_barRenderers[i] != null) _barRenderers[i].enabled = on;

            if (t >= 1f)
            {
                HealNumber.Spawn(_to, _amount);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 대상 머리 위 "+N" 회복량(초록 월드텍스트). 위로 떠오르며 페이드 후 소멸.
    /// 스폰 시 1회 카메라 향해 정렬(고정 3/4뷰라 per-frame 빌보드 불필요 — 모바일 쌈).
    /// </summary>
    public class HealNumber : MonoBehaviour
    {
        const float Life = 0.8f;
        const float RiseSpeed = 1.3f;
        static readonly Color NumColor = new Color(0.45f, 1f, 0.55f);

        private TextMeshPro _tmp;
        private float _age;

        public static void Spawn(Vector3 pos, int amount)
        {
            var go = new GameObject("HealNumber");
            go.transform.position = pos;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = "+" + amount;
            tmp.fontSize = 6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = NumColor;
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 2f);

            // Camera.main은 씬 카메라가 Untagged라 null → 폴백 탐색. 스폰 시 1회 정렬.
            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam != null)
                go.transform.rotation = Quaternion.LookRotation(go.transform.position - cam.transform.position);

            var n = go.AddComponent<HealNumber>();
            n._tmp = tmp;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / Life);
            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);
            if (_tmp != null)
            {
                Color c = NumColor;
                c.a = 1f - t;                                    // 페이드 아웃
                _tmp.color = c;
            }
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
