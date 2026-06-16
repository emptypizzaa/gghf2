#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PaperHeroes.EditorTools
{
    /// <summary>
    /// Play-mode test helpers invoked via `unity-cli execute-menu`. They spawn units,
    /// fast-forward time, and log ASCII state markers (read back via `read-console`),
    /// since the dynamic `execute --code` path does not compile in this project.
    /// </summary>
    public static class PaperHeroesTestMenu
    {
        const string CharDir = "Assets/Data/Characters";

        static BattleManager BM => UnityEngine.Object.FindFirstObjectByType<BattleManager>();
        static CharacterData Load(string id) => AssetDatabase.LoadAssetAtPath<CharacterData>(CharDir + "/" + id + ".asset");

        [MenuItem("PaperHeroes/Test/Spawn Tank")]
        static void SpawnTank() => SpawnAlly("ally_tank");
        [MenuItem("PaperHeroes/Test/Spawn Melee")]
        static void SpawnMelee() => SpawnAlly("ally_melee");
        [MenuItem("PaperHeroes/Test/Spawn Ranged")]
        static void SpawnRanged() => SpawnAlly("ally_ranged");
        [MenuItem("PaperHeroes/Test/Spawn Enemy Mob")]
        static void SpawnMob() => SpawnEnemy("enemy_mob");
        [MenuItem("PaperHeroes/Test/Spawn Enemy Bushman")]
        static void SpawnBushman() => SpawnEnemy("enemy_bushman");

        static void SpawnAlly(string id)
        {
            var bm = BM; var d = Load(id);
            if (bm == null || d == null) { Debug.LogWarning("PH_TEST missing BM/data for " + id); return; }
            Unit.Spawn(d, Team.Ally, 0f, bm, bm.enemyBase);
            Debug.Log("PH_TEST spawn ally " + id);
        }

        static void SpawnEnemy(string id)
        {
            var bm = BM; var d = Load(id);
            if (bm == null || d == null) { Debug.LogWarning("PH_TEST missing BM/data for " + id); return; }
            float x = bm.stage != null ? bm.stage.laneLength : 18f;
            Unit.Spawn(d, Team.Enemy, x, bm, bm.playerBase);
            Debug.Log("PH_TEST spawn enemy " + id);
        }

        [MenuItem("PaperHeroes/Test/Fast Forward x8 (toggle)")]
        static void FastForward()
        {
            Time.timeScale = Mathf.Approximately(Time.timeScale, 1f) ? 8f : 1f;
            Debug.Log("PH_TEST timeScale=" + Time.timeScale);
        }

        [MenuItem("PaperHeroes/Test/Reset TimeScale")]
        static void ResetTime()
        {
            Time.timeScale = 1f;
            Debug.Log("PH_TEST timeScale=1");
        }

        [MenuItem("PaperHeroes/Test/Give Money 999")]
        static void GiveMoney()
        {
            var m = UnityEngine.Object.FindFirstObjectByType<MoneyManager>();
            if (m != null) { m.DebugGive(999); Debug.Log("PH_TEST give money 999"); }
            else Debug.LogWarning("PH_TEST no MoneyManager");
        }

        [MenuItem("PaperHeroes/Test/Log State")]
        static void LogState()
        {
            var bm = BM;
            if (bm == null) { Debug.Log("PH_STATE=none"); return; }
            string eh = bm.enemyBase != null ? Mathf.CeilToInt(bm.enemyBase.CurrentHP).ToString() : "?";
            string ph = bm.playerBase != null ? Mathf.CeilToInt(bm.playerBase.CurrentHP).ToString() : "?";
            Debug.Log($"PH_STATE={bm.CurrentState} enemyHP={eh} playerHP={ph} ally={Unit.AllyCount} enemy={Unit.EnemyCount}");
        }
    }
}
#endif
