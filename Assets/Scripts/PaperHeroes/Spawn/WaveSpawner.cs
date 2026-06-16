using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// Runs StageData.waves as a timeline from match start, spawning enemies at the enemy
    /// base (X=laneLength). Uses scaled time so Time.timeScale fast-forwards the whole
    /// timeline for headless testing. The enemy base only spawns; it never attacks.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        StageData _stage;
        BattleManager _battle;

        public void Begin(StageData stage, BattleManager battle)
        {
            _stage = stage;
            _battle = battle;
            StopAllCoroutines();
            StartCoroutine(RunWaves());
        }

        IEnumerator RunWaves()
        {
            if (_stage == null || _stage.waves == null) yield break;

            var waves = new List<WaveEntry>(_stage.waves);
            waves.Sort((a, b) => a.time.CompareTo(b.time)); // defensive: allow out-of-order entries

            float start = Time.time;
            foreach (var w in waves)
            {
                while (Time.time - start < w.time)
                {
                    if (_battle != null && _battle.IsOver) yield break;
                    yield return null;
                }
                if (_battle != null && _battle.IsOver) yield break;
                if (w.enemy == null) continue;

                for (int i = 0; i < w.count; i++)
                {
                    if (_battle != null && _battle.IsOver) yield break;
                    Unit.Spawn(w.enemy, Team.Enemy, _stage.laneLength, _battle, _battle.playerBase);
                    if (w.spacing > 0f && i < w.count - 1)
                        yield return new WaitForSeconds(w.spacing);
                }
            }
        }
    }
}
