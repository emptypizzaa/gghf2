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

            // 자기 거점 바로 앞에서 출발하되, 같은 진영 최후미 유닛이 있으면 그 뒤에 줄세운다(겹침 방지).
            float dir = lane.ForwardDir(faction);
            float startX = lane.SpawnX(faction) + dir * 0.5f;
            const float queueSpacing = 0.9f;
            bool found = false; float rear = 0f;
            var all = Targetables.All;
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i] as Combatant;
                if (c == null || c.faction != faction || c.IsDead) continue;
                float cx = c.transform.position.x;
                if (!found || cx * dir < rear * dir) { rear = cx; found = true; }
            }
            if (found)
            {
                float behind = rear - dir * queueSpacing;
                if (behind * dir < startX * dir) startX = behind; // 최후미가 스폰점보다 앞이면 그 뒤에 배치
            }
            // 거점 뒤(플랫폼 밖)로는 넘기지 않음 — 극단적 과소환 시 거점 부근에 모인다.
            if (startX * dir < lane.SpawnX(faction) * dir) startX = lane.SpawnX(faction);

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
            go.transform.position = new Vector3(startX, lane.groundY + scale.y, lane.laneZ);

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
            return combatant;
        }
    }
}
