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

        [Header("스테이지 데이터(선택)")]
        [Tooltip("미할당 시 Resources/Stage1 자동 로드. 그래도 없으면 기존 씬 와이어링(경제/웨이브/로스터/거점HP) 유지.")]
        public StageData stage;
        public Economy economy;
        public WaveSpawner waveSpawner;
        public SummonController summon;

        public MatchState State { get; private set; } = MatchState.Playing;

        /// <summary>매치 종료(승/패) 시 1회 발생. 결과 UI 등이 구독한다.</summary>
        public event System.Action<MatchState> MatchEnded;

        void Awake()
        {
            // 참조 해소(미할당 시 씬에서 자동 탐색).
            if (lane == null) lane = FindFirstObjectByType<Lane>();
            ResolveBasesIfNeeded();
            if (economy == null) economy = FindFirstObjectByType<Economy>();
            if (waveSpawner == null) waveSpawner = FindFirstObjectByType<WaveSpawner>();
            if (summon == null) summon = FindFirstObjectByType<SummonController>();

            // 스테이지 주입: 미할당 시 Resources/Stage1 자동 로드. Awake에서 적용해야
            // 각 컴포넌트(Economy/WaveSpawner/SummonController)의 Start()가 값을 읽기 전에 반영된다.
            if (stage == null) stage = Resources.Load<StageData>("Stage1");
            if (stage != null) ApplyStage(stage);
        }

        void Start()
        {
            // (참조·스테이지는 Awake에서 처리됨) 폴백 한 번 더 — 혹시 Awake 경로를 못 탄 경우 대비.
            if (lane == null) lane = FindFirstObjectByType<Lane>();
            ResolveBasesIfNeeded();

            if (allyBase != null) allyBase.Destroyed += OnAllyBaseDestroyed;
            if (enemyBase != null) enemyBase.Destroyed += OnEnemyBaseDestroyed;

            // 전투 HUD(거점 HP·나가기) 부착 — 런타임 UI라 씬 편집 없이 코드로 생성.
            // 거점 해소·구독 이후에 부착해 HUD가 거점을 안전히 읽도록 한다(재시작 시 Start 재실행 → 재부착).
            if (GetComponent<CombatHUD>() == null) gameObject.AddComponent<CombatHUD>();
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

        /// <summary>StageData를 씬 구성요소에 주입(거점 HP/경제/웨이브/로스터). null 보호 — 빠진 참조는 건너뛰고 기존 값을 유지한다.</summary>
        void ApplyStage(StageData s)
        {
            if (economy != null && s.economy != null) economy.config = s.economy;
            if (waveSpawner != null && s.wave != null) waveSpawner.wave = s.wave;
            if (summon != null && s.roster != null && s.roster.Length > 0) summon.roster = s.roster;
            if (allyBase != null) allyBase.ConfigureHp(s.allyBaseHp);
            if (enemyBase != null) enemyBase.ConfigureHp(s.enemyBaseHp);
        }

        void OnDestroy()
        {
            if (allyBase != null) allyBase.Destroyed -= OnAllyBaseDestroyed;
            if (enemyBase != null) enemyBase.Destroyed -= OnEnemyBaseDestroyed;
        }

        void OnEnemyBaseDestroyed(BaseController _) => EndMatch(MatchState.Won);
        void OnAllyBaseDestroyed(BaseController _) => EndMatch(MatchState.Lost);

        /// <summary>플레이어가 매치를 포기(전투 UI 나가기). 패배로 종료해 기존 결과 패널(다시 시작)을 재사용한다.</summary>
        public void Concede() => EndMatch(MatchState.Lost);

        void EndMatch(MatchState result)
        {
            if (State != MatchState.Playing) return;
            State = result;
            Debug.Log($"[PaperHeroes] Match ended: {result}");
            Time.timeScale = 0f; // 결과 패널 뒤로 일시정지(재시작 시 1로 복구)
            MatchEnded?.Invoke(result);
        }
    }
}
