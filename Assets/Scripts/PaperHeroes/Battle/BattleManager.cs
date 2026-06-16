using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaperHeroes
{
    /// <summary>
    /// Owns game state and orchestrates runtime init from StageData. Win = enemy base
    /// destroyed; Lose = player base destroyed. The game does NOT end when waves run out
    /// (only base destruction wins). Logs ASCII markers (PH_WIN / PH_LOSE / PH_STATE=...)
    /// so the headless test harness can read state via read-console.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public enum State { Ready, Playing, Win, Lose }

        [Header("Data")]
        public StageData stage;

        [Header("Scene refs (wired by BattleSceneBuilder)")]
        public Base playerBase;
        public Base enemyBase;
        public MoneyManager money;
        public AllySpawner allySpawner;
        public WaveSpawner waveSpawner;
        public ResultPanel resultPanel;

        public State CurrentState { get; private set; } = State.Ready;
        public bool IsPlaying => CurrentState == State.Playing;
        public bool IsOver => CurrentState == State.Win || CurrentState == State.Lose;

        void Start()
        {
            if (stage == null)
            {
                Debug.LogError("PH_ERROR BattleManager has no StageData assigned.");
                return;
            }

            if (playerBase != null)
            {
                playerBase.Init(Team.Ally, stage.playerBaseHP);
                playerBase.OnDestroyed += OnBaseDestroyed;
            }
            if (enemyBase != null)
            {
                enemyBase.Init(Team.Enemy, stage.enemyBaseHP);
                enemyBase.OnDestroyed += OnBaseDestroyed;
            }
            if (money != null) money.Init(stage, this);
            if (allySpawner != null) allySpawner.Init(stage, money, this);
            if (resultPanel != null) resultPanel.Hide();

            CurrentState = State.Playing;
            Debug.Log("PH_STATE=Playing");

            if (waveSpawner != null) waveSpawner.Begin(stage, this);
        }

        void OnDestroy()
        {
            if (playerBase != null) playerBase.OnDestroyed -= OnBaseDestroyed;
            if (enemyBase != null) enemyBase.OnDestroyed -= OnBaseDestroyed;
        }

        void OnBaseDestroyed(Base b)
        {
            if (IsOver) return;
            if (b == enemyBase)
            {
                CurrentState = State.Win;
                Debug.Log("PH_WIN");
                if (resultPanel != null) resultPanel.Show(true);
            }
            else if (b == playerBase)
            {
                CurrentState = State.Lose;
                Debug.Log("PH_LOSE");
                if (resultPanel != null) resultPanel.Show(false);
            }
        }

        /// <summary>Reload the active scene (restart the match). Also restores timeScale.</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
