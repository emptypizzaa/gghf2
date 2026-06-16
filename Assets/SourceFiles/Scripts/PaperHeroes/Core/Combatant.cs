using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 라인 위의 전투 유닛(아군/적 공용). 1D(X축)로 적 거점을 향해 전진하다가
    /// 사거리 안에 적(유닛 또는 거점)이 들어오면 멈춰서 주기적으로 공격한다.
    /// 수치는 전부 CombatantData(SO)에서 읽는다(데이터-로직 분리).
    ///
    /// M2 단순화(의식적 유예): 같은 진영 블로킹이 없어 유닛들이 같은 X에 겹쳐 "스택 교전"한다.
    /// 진짜 공간적 전선(밀당)은 추후 블로킹 도입 시 구현.
    /// </summary>
    public class Combatant : MonoBehaviour, IDamageable
    {
        public CombatantData data;
        public Faction faction;

        private Lane _lane;
        private float _hp;
        private float _attackTimer;

        public float CurrentHp => _hp;
        public bool IsDead => _hp <= 0f;

        // IDamageable
        Faction IDamageable.Faction => faction;
        float IDamageable.PositionX => transform.position.x;

        /// <summary>스폰 시 스포너가 호출. 데이터/진영/라인을 주입한다.</summary>
        public void Init(CombatantData data, Faction faction, Lane lane)
        {
            this.data = data;
            this.faction = faction;
            _lane = lane;
            if (data != null) _hp = data.maxHp;
        }

        private void OnEnable() => Targetables.Register(this);
        private void OnDisable() => Targetables.Unregister(this);

        private void Start()
        {
            if (_lane == null) _lane = FindFirstObjectByType<Lane>();
            if (data != null && _hp <= 0f) _hp = data.maxHp; // Init에서 이미 설정됐으면 유지
        }

        private void Update()
        {
            if (data == null || _lane == null) return;

            IDamageable target = FindNearestEnemyInRange();
            if (target != null)
            {
                // 사거리 안 적 → 공격.
                _attackTimer += Time.deltaTime;
                if (_attackTimer >= data.attackInterval)
                {
                    _attackTimer = 0f;
                    target.TakeDamage(data.attackDamage);
                }
            }
            else
            {
                // 적이 없으면 적 거점 방향으로 전진(1D).
                float step = _lane.ForwardDir(faction) * data.moveSpeed * Time.deltaTime;
                transform.position += new Vector3(step, 0f, 0f);
            }
        }

        /// <summary>사거리 내 가장 가까운 적(유닛·거점 동급). 거점 우선순위 없음.</summary>
        private IDamageable FindNearestEnemyInRange()
        {
            float myX = transform.position.x;
            IDamageable best = null;
            float bestDist = float.MaxValue;

            var all = Targetables.All;
            for (int i = 0; i < all.Count; i++)
            {
                IDamageable t = all[i];
                if (t == null || t.IsDead || t.Faction == faction) continue;

                float d = Mathf.Abs(t.PositionX - myX);
                if (d <= data.attackRange && d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }
            return best;
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead) return;
            _hp -= amount;
            if (_hp <= 0f) Destroy(gameObject);
        }
    }
}
