using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// UnitData/CombatantData로부터 프로토 유닛(프리미티브+색)을 생성한다.
    /// "프리미티브+색으로 먼저 구현 → 복셀 프리팹 교체" 파이프라인의 프로토 단계 구현.
    /// M1: 테스트 스폰용. M3에서 자원/쿨다운/로스터 UI가 SpawnUnit을 호출하게 된다.
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        [SerializeField] private Lane lane;

        [Tooltip("프로토 유닛 메쉬 형태")]
        public PrimitiveType primitive = PrimitiveType.Capsule;

        [Tooltip("스폰 시 Z 지터(±). 겹치는 유닛을 시각적으로 부채꼴로 펼친다 — 전투/타게팅은 X축만 쓰므로 게임플레이 영향 0. 0=옛 1라인 정렬.")]
        [SerializeField] private float zJitter = 0.4f;

        private void Awake()
        {
            if (lane == null) lane = FindFirstObjectByType<Lane>();
        }

        /// <summary>데이터로부터 유닛 1기를 자기 진영 거점 앞에 생성한다.</summary>
        public Combatant SpawnUnit(CombatantData data, Faction faction)
        {
            if (data == null)
            {
                Debug.LogError("[PaperHeroes] UnitSpawner.SpawnUnit: data가 null입니다.");
                return null;
            }
            if (lane == null) lane = FindFirstObjectByType<Lane>();

            var go = GameObject.CreatePrimitive(primitive);
            go.name = $"Unit_{faction}_{data.name}";

            // 스폰은 자기 거점 앞. 아래에서 같은 진영 최후미 유닛 뒤로 옮겨 줄 맨 뒤에 세운다(소환 순서 = 줄 순서).
            float dir = lane.ForwardDir(faction);
            float startX = lane.SpawnX(faction) + dir * 0.5f;

            // 역할별 실루엣 차별화(P0): 복셀 교체 전까지 스케일로 포지션을 구분.
            Vector3 scale = Vector3.one;
            var ud = data as UnitData;
            if (ud != null)
            {
                switch (ud.role)
                {
                    case UnitRole.Tank:   scale = new Vector3(1.3f, 1.15f, 1.3f); break;
                    case UnitRole.Ranged: scale = new Vector3(0.72f, 1.18f, 0.72f); break;
                    case UnitRole.Healer: scale = new Vector3(0.8f, 0.9f, 0.8f); break;
                }
            }
            else if (data is EnemyData ed && ed.isBoss)
            {
                scale = new Vector3(1.45f, 1.45f, 1.45f);
            }
            go.transform.localScale = scale;

            // 같은 진영 최후미(가장 덜 전진한) 유닛 뒤에 스폰 → 새 유닛이 맨 뒤에서 출발(겹침 없는 초기 배치). 이후 아군=자유 행군, 적=줄(TryLeaderCapX).
            Combatant rear = null;
            var roster = Targetables.All;
            for (int i = 0; i < roster.Count; i++)
            {
                var c = roster[i] as Combatant;
                if (c == null || c.faction != faction || c.IsDead) continue;
                if (rear == null || c.transform.position.x * dir < rear.transform.position.x * dir) rear = c;
            }
            if (rear != null)
            {
                float spacing = 0.5f * scale.x + Combatant.HalfWidth(rear);
                float behind = rear.transform.position.x - dir * spacing;
                if (behind * dir < startX * dir) startX = behind; // 최후미가 스폰점보다 앞이면 그 뒤에 배치
            }
            // 거점 뒤(플랫폼 밖)로는 넘기지 않음 — 과다 소환 시 거점 부근에 모인다(적은 후퇴금지라 화면밖 갇힘 방지).
            if (startX * dir < lane.SpawnX(faction) * dir) startX = lane.SpawnX(faction);

            go.transform.position = new Vector3(startX, lane.groundY + scale.y, lane.laneZ + Random.Range(-zJitter, zJitter));

            // 프로토 색 구분.
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", data.prototypeColor);
                renderer.sharedMaterial = mat;
            }

            var combatant = go.AddComponent<Combatant>();
            combatant.Init(data, faction, lane);

            // 모델 비주얼(선택): 프리미티브 메쉬를 숨기고 3D 모델을 자식으로 부착(자동 스케일 + 애니메이션).
            if (data.visualPrefab != null)
            {
                if (renderer != null) renderer.enabled = false;
                var model = Instantiate(data.visualPrefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                // 행군 방향(아군=적진 +X / 적=아군 -X)을 바라보게. modelYawOffset로 모델 기본 정면 보정.
                var march = new Vector3(lane.ForwardDir(faction), 0f, 0f);
                model.transform.rotation = Quaternion.LookRotation(march, Vector3.up) * Quaternion.Euler(0f, data.modelYawOffset, 0f);
                FitModel(go.transform, model, data.visualHeight);
                SharpenTextures(model); // 복셀/픽셀아트 텍스처를 Point 필터로(보간 뭉개짐 방지)

                if (data.walkClip != null || data.idleClip != null || data.attackClip != null)
                {
                    var anim = model.AddComponent<ModelAnimator>();
                    anim.combatant = combatant;
                    anim.walk = data.walkClip;
                    anim.idle = data.idleClip;
                    anim.attack = data.attackClip;
                }
            }

            return combatant;
        }

        /// <summary>
        /// 모델 텍스처를 Point 필터로 설정 — 복셀/픽셀아트(작은 텍스처)가 Bilinear 보간으로
        /// 뭉개지지 않고 또렷하게 보이도록(눈·나무 방패 등). aniso도 끔.
        /// </summary>
        private void SharpenTextures(GameObject model)
        {
            var ids = new System.Collections.Generic.List<int>();
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                var mats = renderers[r].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null) continue;
                    ids.Clear();
                    mat.GetTexturePropertyNameIDs(ids);
                    for (int i = 0; i < ids.Count; i++)
                    {
                        var tex = mat.GetTexture(ids[i]) as Texture2D;
                        if (tex != null)
                        {
                            tex.filterMode = FilterMode.Point;
                            tex.anisoLevel = 0;
                        }
                    }
                }
            }
        }

        /// <summary>모델을 목표 높이로 스케일하고 바닥을 유닛 발(지면)에 맞춘다.</summary>
        private void FitModel(Transform root, GameObject model, float targetHeight)
        {
            var rends = model.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float h = b.size.y;
            if (h < 1e-4f) return;

            float parentY = Mathf.Max(1e-4f, root.lossyScale.y);
            model.transform.localScale = Vector3.one * (targetHeight / h / parentY);

            // 스케일 후 바운즈 재측정 → 모델 바닥을 유닛 발(지면)에 정렬.
            Bounds b2 = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b2.Encapsulate(rends[i].bounds);
            float footY = root.position.y - root.lossyScale.y;
            model.transform.position += new Vector3(0f, footY - b2.min.y, 0f);
        }
    }
}
