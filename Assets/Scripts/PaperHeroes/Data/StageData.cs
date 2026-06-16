using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>One scheduled enemy spawn burst in a stage's wave timeline.</summary>
    [Serializable]
    public struct WaveEntry
    {
        public float time;          // seconds after stage start
        public CharacterData enemy; // which enemy to spawn
        public int count;           // how many
        public float spacing;       // seconds between each spawn in this burst
    }

    /// <summary>
    /// A single stage: base HP, economy, lane length, the ally roster (= buttons),
    /// and the enemy wave timeline. All tuning lives here.
    /// </summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "PaperHeroes/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("Bases")]
        public float playerBaseHP = 1000f;
        public float enemyBaseHP = 1500f;

        [Header("Money")]
        public float moneyStart = 50f;
        public float moneyPerSecond = 5f;
        public float moneyMax = 300f;

        [Header("Lane")]
        public float laneLength = 18f;

        [Header("Ally roster (drives the spawn buttons, in order)")]
        public List<CharacterData> allyRoster = new List<CharacterData>();

        [Header("Enemy wave timeline")]
        public List<WaveEntry> waves = new List<WaveEntry>();
    }
}
