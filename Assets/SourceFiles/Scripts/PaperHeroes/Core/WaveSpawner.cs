using System.Collections;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// WaveData 타임라인대로 적 유닛을 스폰한다. 적 거점은 스스로 공격하지 않고 이걸로 적을 내보낸다.
    /// wave 참조는 에디터에서 에셋으로 주입한다(런타임 AssetDatabase 사용 금지 — 빌드에서 깨짐).
    /// </summary>
    [RequireComponent(typeof(UnitSpawner))]
    public class WaveSpawner : MonoBehaviour
    {
        [Tooltip("스폰할 웨이브(에디터에서 WaveData 에셋 참조로 주입)")]
        public WaveData wave;

        [Tooltip("스폰되는 측 진영")]
        public Faction faction = Faction.Enemy;

        private UnitSpawner _spawner;

        private void Awake()
        {
            _spawner = GetComponent<UnitSpawner>();
        }

        private void Start()
        {
            if (wave == null)
            {
                Debug.LogWarning("[PaperHeroes] WaveSpawner: wave가 할당되지 않았습니다.");
                return;
            }

            // 엔트리마다 독립 코루틴 → 각자 spawnTime에 발화, count만큼 interval 간격으로 스폰.
            foreach (var entry in wave.entries)
            {
                StartCoroutine(SpawnEntry(entry));
            }
        }

        private IEnumerator SpawnEntry(WaveData.Entry entry)
        {
            if (entry == null || entry.enemy == null) yield break;

            yield return new WaitForSeconds(entry.spawnTime);
            for (int i = 0; i < entry.count; i++)
            {
                _spawner.SpawnUnit(entry.enemy, faction);
                if (i < entry.count - 1)
                    yield return new WaitForSeconds(entry.interval);
            }
        }
    }
}
