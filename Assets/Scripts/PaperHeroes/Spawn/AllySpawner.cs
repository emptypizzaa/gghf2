using System.Collections.Generic;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// Spawns allies at the player base (X=0) on demand: spend money, start a per-type
    /// cooldown. UI buttons query CanSpawn / CooldownRemaining01 to drive enable/sweep.
    /// </summary>
    public class AllySpawner : MonoBehaviour
    {
        StageData _stage;
        MoneyManager _money;
        BattleManager _battle;
        readonly Dictionary<CharacterData, float> _cooldownUntil = new Dictionary<CharacterData, float>();

        public void Init(StageData stage, MoneyManager money, BattleManager battle)
        {
            _stage = stage;
            _money = money;
            _battle = battle;
            _cooldownUntil.Clear();
        }

        public bool IsOnCooldown(CharacterData data)
            => _cooldownUntil.TryGetValue(data, out var until) && Time.time < until;

        /// <summary>0 = ready, 1 = just spawned (drives the cooldown sweep fill).</summary>
        public float CooldownRemaining01(CharacterData data)
        {
            if (data == null || data.spawnCooldown <= 0f) return 0f;
            if (!_cooldownUntil.TryGetValue(data, out var until)) return 0f;
            float remain = until - Time.time;
            return remain <= 0f ? 0f : Mathf.Clamp01(remain / data.spawnCooldown);
        }

        public bool CanSpawn(CharacterData data)
            => _battle != null && _battle.IsPlaying
               && _money != null && _money.CanAfford(data.cost)
               && !IsOnCooldown(data);

        /// <summary>Try to spawn; returns false if not playing, on cooldown, or unaffordable.</summary>
        public bool TrySpawn(CharacterData data)
        {
            if (data == null) return false;
            if (_battle == null || !_battle.IsPlaying) return false;
            if (IsOnCooldown(data)) return false;
            if (_money == null || !_money.TrySpend(data.cost)) return false;

            Unit.Spawn(data, Team.Ally, 0f, _battle, _battle.enemyBase);
            _cooldownUntil[data] = Time.time + data.spawnCooldown;
            return true;
        }
    }
}
