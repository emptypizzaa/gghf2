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

            // 자기 거점 바로 앞에서 출발.
            float startX = lane.SpawnX(faction) + lane.ForwardDir(faction) * 1f;
            go.transform.position = new Vector3(startX, lane.groundY + 1f, lane.laneZ);

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
