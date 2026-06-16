using UnityEngine;

namespace PaperHeroes
{
    public enum MatchState { Playing, Won, Lost }

    /// <summary>
    /// 매치 전체 상태를 쥐는 최상위 매니저(M0 골격).
    /// 양쪽 거점의 파괴 이벤트를 듣고 승/패를 판정한다.
    /// M1~M4에서 진군·전투·자원·웨이브 시스템이 여기에 연결된다.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        [Header("씬 참조")]
        public Lane lane;
        public BaseController allyBase;
        public BaseController enemyBase;

        public MatchState State { get; private set; } = MatchState.Playing;

        void Start()
        {
            // 명시적 참조가 없으면 씬에서 자동 탐색(브리지/프로토 편의 — 수동 와이어링 없이도 동작).
            if (lane == null) lane = FindFirstObjectByType<Lane>();
            ResolveBasesIfNeeded();

            if (allyBase != null) allyBase.Destroyed += OnAllyBaseDestroyed;
            if (enemyBase != null) enemyBase.Destroyed += OnEnemyBaseDestroyed;
        }

        void ResolveBasesIfNeeded()
        {
            if (allyBase != null && enemyBase != null) return;

            var bases = FindObjectsByType<BaseController>(FindObjectsSortMode.None);
            foreach (var b in bases)
            {
                if (b.faction == Faction.Ally && allyBase == null) allyBase = b;
                else if (b.faction == Faction.Enemy && enemyBase == null) enemyBase = b;
            }
        }

        void OnDestroy()
        {
            if (allyBase != null) allyBase.Destroyed -= OnAllyBaseDestroyed;
            if (enemyBase != null) enemyBase.Destroyed -= OnEnemyBaseDestroyed;
        }

        void OnEnemyBaseDestroyed(BaseController _) => EndMatch(MatchState.Won);
        void OnAllyBaseDestroyed(BaseController _) => EndMatch(MatchState.Lost);

        void EndMatch(MatchState result)
        {
            if (State != MatchState.Playing) return;
            State = result;
            Debug.Log($"[PaperHeroes] Match ended: {result}");
            // 프로토: 종료 표시는 일시정지로 대신. M4에서 결과 UI/재시작으로 교체.
            Time.timeScale = 0f;
        }
    }
}
