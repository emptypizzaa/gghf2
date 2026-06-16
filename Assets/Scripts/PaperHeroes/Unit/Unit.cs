using System.Collections.Generic;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// A runtime combatant (ally or enemy). FSM: Move -> Attack -> (Dead). Decisions run
    /// on a fixed ~0.1s tick; movement is continuous. No physics — overlap is allowed,
    /// movement is transform.Translate, targeting is 1D X-distance via a global registry.
    /// </summary>
    public class Unit : MonoBehaviour
    {
        // Global registry used by Targeting (no physics colliders).
        public static readonly List<Unit> All = new List<Unit>();
        public static int AllyCount;
        public static int EnemyCount;

        /// <summary>Clear registry/counters — call when a new match starts (in-play scene reload).</summary>
        public static void ResetRegistry()
        {
            All.Clear();
            AllyCount = 0;
            EnemyCount = 0;
        }

        /// <summary>Factory: create a unit GameObject (prefab or bare) and initialize it.</summary>
        public static Unit Spawn(CharacterData data, Team team, float x, BattleManager battle, Base targetBase)
        {
            GameObject go = data.prefab != null
                ? Instantiate(data.prefab)
                : new GameObject(string.IsNullOrEmpty(data.id) ? "Unit" : data.id);
            go.transform.position = new Vector3(x, 0f, 0f);
            var unit = go.GetComponent<Unit>();
            if (unit == null) unit = go.AddComponent<Unit>();
            unit.Init(data, team, battle, targetBase);
            return unit;
        }

        public CharacterData Data { get; private set; }
        public Team Team { get; private set; }
        public float CurrentHP { get; private set; }
        public bool IsDead { get; private set; }

        BattleManager _battle;
        Base _targetBase;     // the opposing base this unit marches toward
        float _moveDir;       // +1 ally (toward +X), -1 enemy
        float _tickTimer;
        float _attackTimer;
        Unit _targetUnit;

        const float TickInterval = 0.1f;
        enum FSM { Move, Attack }
        FSM _state = FSM.Move;

        public void Init(CharacterData data, Team team, BattleManager battle, Base targetBase)
        {
            Data = data;
            Team = team;
            _battle = battle;
            _targetBase = targetBase;
            CurrentHP = data.maxHP;
            _moveDir = team == Team.Ally ? 1f : -1f;
            BuildVisualIfNeeded();
            All.Add(this);
            if (team == Team.Ally) AllyCount++; else EnemyCount++;
        }

        void OnDisable()
        {
            // Keep registry clean even on unexpected destroy / scene unload.
            if (!IsDead && All.Remove(this))
            {
                if (Team == Team.Ally) AllyCount--; else EnemyCount--;
            }
        }

        void Update()
        {
            if (IsDead || Data == null) return;
            if (_battle != null && !_battle.IsPlaying) return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= TickInterval)
            {
                _tickTimer -= TickInterval;
                Decide();
            }

            if (_state == FSM.Move)
            {
                transform.Translate(_moveDir * Data.moveSpeed * Time.deltaTime, 0f, 0f, Space.World);
            }
            else // Attack
            {
                _attackTimer += Time.deltaTime;
                if (_attackTimer >= Data.attackInterval)
                {
                    _attackTimer = 0f;
                    DoAttack();
                }
            }
        }

        void Decide()
        {
            // (Healer role is STRETCH — falls through to normal targeting for MVP.)
            _targetUnit = Targeting.FindNearestEnemyInRange(transform.position.x, Team, Data.attackRange);
            if (_targetUnit != null) { _state = FSM.Attack; return; }

            if (TargetBaseInRange()) { _state = FSM.Attack; return; }

            _state = FSM.Move;
        }

        void DoAttack()
        {
            if (_targetUnit != null && !_targetUnit.IsDead &&
                Mathf.Abs(_targetUnit.transform.position.x - transform.position.x) <= Data.attackRange)
            {
                _targetUnit.TakeDamage(Data.attackPower);
                return;
            }
            if (TargetBaseInRange())
            {
                _targetBase.TakeDamage(Data.attackPower);
                return;
            }
            _state = FSM.Move; // nothing valid in range — resume marching
        }

        bool TargetBaseInRange()
        {
            return _targetBase != null && !_targetBase.IsDestroyed &&
                   Mathf.Abs(_targetBase.transform.position.x - transform.position.x) <= Data.attackRange;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            CurrentHP -= amount;
            if (CurrentHP <= 0f) Die();
        }

        void Die()
        {
            IsDead = true;
            if (All.Remove(this))
            {
                if (Team == Team.Ally) AllyCount--; else EnemyCount--;
            }
            Destroy(gameObject);
        }

        void BuildVisualIfNeeded()
        {
            if (Data.prefab != null) return; // prefab is the visual

            var prim = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            prim.name = "Visual";
            prim.transform.SetParent(transform, false);
            prim.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var col = prim.GetComponent<Collider>();
            if (col != null) Destroy(col); // no physics

            var rend = prim.GetComponent<Renderer>();
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            var mat = new Material(sh);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Data.colorTag);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Data.colorTag);
            rend.sharedMaterial = mat;
        }
    }
}
