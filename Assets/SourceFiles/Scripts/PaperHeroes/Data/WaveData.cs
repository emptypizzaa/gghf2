using System.Collections.Generic;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 한 스테이지의 적 스폰 타임라인. 적 거점은 스스로 공격하지 않고 이 타임라인대로 적을 스폰한다.
    /// (스폰 실행 로직은 M2에서 붙는다 — M0/M1 단계에선 데이터 골격만.)
    /// </summary>
    [CreateAssetMenu(fileName = "WaveData", menuName = "PaperHeroes/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public EnemyData enemy;

            [Tooltip("매치 시작 후 스폰 시각(초)")]
            public float spawnTime;

            [Tooltip("연속 스폰 수")]
            public int count = 1;

            [Tooltip("연속 스폰 간격(초)")]
            public float interval = 0.5f;
        }

        [Tooltip("시간순 스폰 항목들")]
        public List<Entry> entries = new List<Entry>();
    }
}
