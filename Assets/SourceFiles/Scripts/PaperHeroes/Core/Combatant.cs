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

        /// <summary>외형 애니메이션용 행동 상태(이동/교전/정지).</summary>
        public enum ActState { Idle, Moving, Attacking }
        public ActState Motion { get; private set; } = ActState.Idle;

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

            if (data.isHealer)
            {
                // 힐러: 사거리 내 가장 다친 아군 회복.
                Combatant patient = FindNeediestAllyInRange();
                if (patient != null)
                {
                    Motion = ActState.Attacking;
                    _attackTimer += Time.deltaTime;
                    if (_attackTimer >= data.attackInterval)
                    {
                        _attackTimer = 0f;
                        patient.Heal(data.attackDamage);
                    }
                    return;
                }
            }
            else
            {
                // 공격수: 사거리 내 적 공격.
                IDamageable target = FindNearestEnemyInRange();
                if (target != null)
                {
                    Motion = ActState.Attacking;
                    _attackTimer += Time.deltaTime;
                    if (_attackTimer >= data.attackInterval)
                    {
                        _attackTimer = 0f;
                        if (data.usesProjectile)
                            Projectile.Spawn(MuzzlePosition(), target, data, faction);
                        else
                            target.TakeDamage(data.attackDamage);
                    }
                    return;
                }
            }

            // 행동 대상이 없고 전방이 막히지 않았으면 전진(1D). 막혀 있으면 정지 → 공간적 전선 형성.
            if (!BlockedByFriendlyAhead())
            {
                Motion = ActState.Moving;
                float step = _lane.ForwardDir(faction) * data.moveSpeed * Time.deltaTime;
                transform.position += new Vector3(step, 0f, 0f);
            }
            else
            {
                Motion = ActState.Idle;
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

        /// <summary>사거리 내, HP가 가득 차지 않은 아군 중 가장 다친(HP 비율 최저) 유닛.</summary>
        private Combatant FindNeediestAllyInRange()
        {
            float myX = transform.position.x;
            Combatant best = null;
            float bestRatio = 1f;

            var all = Targetables.All;
            for (int i = 0; i < all.Count; i++)
            {
                var other = all[i] as Combatant;
                if (other == null || other == this || other.IsDead) continue;
                if (other.faction != faction || other.data == null || other.data.maxHp <= 0f) continue;

                float ratio = other.CurrentHp / other.data.maxHp;
                if (ratio >= 1f) continue; // 가득 찬 아군은 제외

                float d = Mathf.Abs(other.transform.position.x - myX);
                if (d <= data.attackRange && ratio < bestRatio)
                {
                    bestRatio = ratio;
                    best = other;
                }
            }
            return best;
        }

        // 같은 진영 유닛 사이 최소 간격(통과 방지·줄서기). 프로토 캡슐 지름 ~1 기준.
        private const float BodySpacing = 0.85f;

        /// <summary>전진 방향 앞에 같은 진영 유닛이 BodySpacing 안에 있으면 true → 전진 정지(블로킹).</summary>
        private bool BlockedByFriendlyAhead()
        {
            float myX = transform.position.x;
            int dir = _lane.ForwardDir(faction);

            var all = Targetables.All;
            for (int i = 0; i < all.Count; i++)
            {
                var other = all[i] as Combatant; // 거점은 블로킹하지 않음(유닛만)
                if (other == null || other == this || other.IsDead) continue;
                if (other.faction != faction) continue;

                float rel = (other.transform.position.x - myX) * dir; // > 0 이면 전방
                if (rel > 0f && rel < BodySpacing) return true;
            }
            return false;
        }

        /// <summary>발사체 발사 위치 — 몸 위쪽·전방 끝(머즐).</summary>
        private Vector3 MuzzlePosition()
        {
            float dir = _lane != null ? _lane.ForwardDir(faction) : 1f;
            Vector3 p = transform.position;
            p.y += transform.localScale.y * 0.5f;
            p.x += dir * transform.localScale.x * 0.5f;
            return p;
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead) return;
            _hp -= amount;
            if (_hp <= 0f) Destroy(gameObject);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead || data == null) return;
            _hp = Mathf.Min(data.maxHp, _hp + amount);
        }
    }
}
