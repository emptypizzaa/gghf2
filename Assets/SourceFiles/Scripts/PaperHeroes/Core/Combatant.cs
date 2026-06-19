using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 라인 위의 전투 유닛(아군/적 공용). 1D(X축)로 적 거점을 향해 전진하다가
    /// 사거리 안에 적(유닛 또는 거점)이 들어오면 멈춰서 주기적으로 공격한다.
    /// 수치는 전부 CombatantData(SO)에서 읽는다(데이터-로직 분리).
    ///
    /// 이동(2026-06-17 확정): **아군 = 냥코식 자유 행군** — 자기 moveSpeed로 적 거점 향해 전진,
    /// 서로 통과·겹침 허용(블로킹 없음, 추월 가능). **적 = 줄 유지** — 먼저 스폰된 앞 유닛을
    /// 추월하지 않고 footprint 간격으로 따라감(TryLeaderCapX, 적 전용). 물리(Rigidbody) 없음.
    /// </summary>
    public class Combatant : MonoBehaviour, IDamageable
    {
        public CombatantData data;
        public Faction faction;

        private Lane _lane;
        private float _hp;
        private float _attackTimer;

        // 스폰 순번 = 줄 순서. 먼저 소환될수록 작고(앞), 늦을수록 큼(뒤). Init에서 1회 할당. 승급해도 유지(자기 자리 보존).
        private static int _spawnCounter;
        public int SpawnSeq { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _spawnCounter = 0; _economy = null; }

        // 적 처치 보상 지급용 경제 참조(최초 1회 탐색·캐시, 플레이 시작 시 리셋).
        private static Economy _economy;
        private static Economy EconomyRef
        {
            get { if (_economy == null) _economy = FindFirstObjectByType<Economy>(); return _economy; }
        }

        // 겹침 회피 부채꼴 연출(시각 전용 Z 분리) 설정. Resources/FanConfig에서 읽음(없으면 폴백 기본값).
        private static FanConfig _fanConfig;
        private static FanConfig Fan
        {
            get { if (_fanConfig == null) _fanConfig = Resources.Load<FanConfig>("FanConfig"); return _fanConfig; }
        }

        public float CurrentHp => _hp;
        public bool IsDead => _hp <= 0f;

        // 승급(머지) 티어(1~3). 동일 유닛 중복 소환 시 1단계씩 상승. 스탯은 read-time 배수로 적용(공유 SO 불변). (설계 12번)
        public int Tier { get; private set; } = 1;
        private float TierMul => Tier >= 3 ? 2f : (Tier == 2 ? 1.5f : 1f);
        public float MaxHp => (data != null ? data.maxHp : 0f) * TierMul;
        public float AttackDamage => (data != null ? data.attackDamage : 0f) * TierMul;
        private Vector3 _baseScale = Vector3.one;

        /// <summary>외형 애니메이션용 행동 상태(이동/교전/정지).</summary>
        public enum ActState { Idle, Moving, Attacking }
        public ActState Motion { get; private set; } = ActState.Idle;

        // IDamageable
        Faction IDamageable.Faction => faction;
        float IDamageable.PositionX => transform.position.x;

        /// <summary>유닛 footprint 반폭(캡슐 폭 = localScale.x). UnitSpawner가 role/보스별 스케일을 세팅하므로 단일 진실원천.</summary>
        public static float HalfWidth(Combatant c) => 0.5f * Mathf.Max(0.0001f, c.transform.localScale.x);

        /// <summary>스폰 시 스포너가 호출. 데이터/진영/라인을 주입한다.</summary>
        public void Init(CombatantData data, Faction faction, Lane lane)
        {
            this.data = data;
            this.faction = faction;
            _lane = lane;
            _baseScale = transform.localScale;
            if (data != null) _hp = MaxHp;
            SpawnSeq = _spawnCounter++;
        }

        private void OnEnable() => Targetables.Register(this);
        private void OnDisable() => Targetables.Unregister(this);

        private void Start()
        {
            if (_lane == null) _lane = FindFirstObjectByType<Lane>();
            if (data != null && _hp <= 0f) _hp = MaxHp; // Init에서 이미 설정됐으면 유지
        }

        private void Update()
        {
            if (data == null || _lane == null) return;

            ApplyFanSeparation(); // 겹침 회피 Z 분리(시각 전용 — X/전투/타게팅 불변). 공격·이동과 무관하게 매 프레임.

            if (data.isHealer)
            {
                // 힐러: 사거리 내 가장 다친 아군이 있으면 정지·회복.
                Combatant patient = FindNeediestAllyInRange();
                if (patient != null)
                {
                    Motion = ActState.Attacking;
                    _attackTimer += Time.deltaTime;
                    if (_attackTimer >= data.attackInterval)
                    {
                        _attackTimer = 0f;
                        float before = patient.CurrentHp;
                        patient.Heal(AttackDamage);                    // 힐 로직·수치 무변경
                        float healed = patient.CurrentHp - before;     // 실제 회복량(클램프 반영)
                        if (healed > 0f)                               // 코스메틱 연출만 — 힐 틱(attackInterval)당 1회 = 도배 아님
                            HealVfx.Play(
                                transform.position + Vector3.up * transform.localScale.y * 0.6f,          // 힐러 staff(스냅샷)
                                patient.transform.position + Vector3.up * patient.transform.localScale.y, // 대상 머리(스냅샷)
                                Mathf.RoundToInt(healed));
                    }
                    return;
                }
            }
            else
            {
                // 공격수: 사거리 내 적이 있으면 정지·공격.
                IDamageable target = FindNearestEnemyInRange();
                if (target != null)
                {
                    Motion = ActState.Attacking;
                    _attackTimer += Time.deltaTime;
                    if (_attackTimer >= data.attackInterval)
                    {
                        _attackTimer = 0f;
                        if (data.usesProjectile)
                            Projectile.Spawn(MuzzlePosition(), target, data, faction, AttackDamage);
                        else
                            target.TakeDamage(AttackDamage);
                    }
                    return;
                }
            }

            // 행동 대상이 없으면 적 거점 방향으로 전진.
            // 아군 = 자유 행군(통과·겹침·추월 허용, 캡 없음). 적 = 줄 유지(앞 유닛 추월 금지). 아군 힐러 = 전선 뒤 standoff 유지.
            Motion = ActState.Moving;
            float dir = _lane.ForwardDir(faction);
            float curX = transform.position.x;
            float newX;
            if (data.isHealer && faction != Faction.Enemy)
            {
                // 힐러: 적진 직진 대신 가장 앞선 아군 전투원 뒤 standoff 슬롯으로 호밍(돌진 금지).
                // 선두 사망으로 슬롯이 뒤로 점프하면 후퇴 허용(안전 리포지션). 앞에 전투원이 없으면 단독 전진 금지(대기).
                newX = TryHealerStandoffX(dir, out float standoffX)
                    ? Mathf.MoveTowards(curX, standoffX, data.moveSpeed * Time.deltaTime)
                    : curX;
            }
            else
            {
                newX = curX + dir * data.moveSpeed * Time.deltaTime;
                if (faction == Faction.Enemy)
                {
                    if (TryLeaderCapX(dir, out float capX) && newX * dir > capX * dir)
                        newX = capX;                          // 앞 유닛 뒤 한계로 캡(추월 금지)
                    if (newX * dir < curX * dir) newX = curX; // 후퇴 금지(한계가 현 위치보다 뒤면 정지)
                }
            }
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
            if (Mathf.Approximately(newX, curX)) Motion = ActState.Idle; // 캡으로 제자리면(적 줄/힐러 대기) 걷기 애니 방지
        }

        /// <summary>
        /// (적 전용 — 줄 유지) 내 바로 앞 유닛(= 나보다 먼저 스폰된 SpawnSeq 중 최대) 뒤 spacing 지점을 전진 한계 X로 돌려준다.
        /// 앞 유닛이 죽으면 그 다음으로 먼저 스폰된 생존 유닛이 새 리더가 된다. 내가 선두(앞에 아무도 없음)면 false.
        /// spacing = 두 유닛 footprint 반폭 합 → 겹침 없이 따라붙는다. (아군은 자유 행군이라 호출하지 않음.)
        /// </summary>
        private bool TryLeaderCapX(float dir, out float capX)
        {
            capX = 0f;
            Combatant leader = null;
            var all = Targetables.All;
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i] as Combatant;
                if (c == null || c == this || c.IsDead || c.faction != faction) continue;
                if (c.SpawnSeq >= SpawnSeq) continue;                          // 나보다 먼저 스폰된(앞) 유닛만
                if (leader == null || c.SpawnSeq > leader.SpawnSeq) leader = c; // 바로 앞(SpawnSeq 최대)
            }
            if (leader == null) return false;
            float spacing = HalfWidth(this) + HalfWidth(leader); // footprint(겹침 없음)
            capX = leader.transform.position.x - dir * spacing;
            return true;
        }

        // 힐러가 머무는 전선 뒤 거리 = attackRange * 이 비율. 0.6 → 선두 전투원이 힐 사거리 안(여유). (튜닝 노브)
        private const float HealerStandoffFrac = 0.6f;

        /// <summary>
        /// (아군 힐러 전용) 가장 전진한 아군 전투원(비힐러) 뒤 standoff 지점을 전진 목표 X로 돌려준다.
        /// 그 지점으로 호밍하면 전선 전투원이 힐 사거리에 들어와 회복 대상이 된다(포지션 삼각).
        /// 앞에 아군 전투원이 없으면(단독) false → 혼자 적진 돌진 금지(대기). 적·비힐러는 호출하지 않음.
        /// </summary>
        private bool TryHealerStandoffX(float dir, out float standoffX)
        {
            standoffX = 0f;
            Combatant front = null;
            var all = Targetables.All;
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i] as Combatant;
                if (c == null || c == this || c.IsDead || c.faction != faction) continue;
                if (c.data != null && c.data.isHealer) continue;                          // 다른 힐러는 전선 기준 제외
                if (front == null || c.transform.position.x * dir > front.transform.position.x * dir)
                    front = c;                                                             // 가장 전진(forward 방향 X 최대)
            }
            if (front == null) return false;
            standoffX = front.transform.position.x - dir * (data.attackRange * HealerStandoffFrac);
            return true;
        }

        /// <summary>
        /// 겹침 회피 "부채꼴" 연출: 같은 진영 유닛 중 XZ로 가까운(겹치는) 유닛에게서 Z로 밀어낸다.
        /// **transform.x(전투 위치)는 절대 안 건드림** → 타게팅/사거리/이동 게임플레이 0 변화. Z만 조정해 시각적으로 펼침.
        /// overlapRadius 밖이면 힘 0(deadzone) → 떨림 없음. 같은 Z 동률은 SpawnSeq 순서로 결정적 분리. laneZ±zBand로 클램프. 아군/적 공용.
        /// </summary>
        private void ApplyFanSeparation()
        {
            var cfg = Fan;
            float overlapR = cfg != null ? cfg.overlapRadius : 0.8f;
            float strength = cfg != null ? cfg.zSeparationStrength : 3f;
            float zBand = cfg != null ? cfg.zBand : 1.8f;

            // 적은 줄(컬럼)을 유지하므로 부채꼴 Z 분리를 약하게(덜 퍼지게). 아군은 자유 행군 → 겹침 방지로 풀 적용.
            if (faction == Faction.Enemy)
            {
                float es = cfg != null ? cfg.enemyFanScale : 0.3f;
                strength *= es;
                zBand *= es;
            }

            // 근접(Melee)이 교전 중이면 부채꼴을 더 세고·넓게·멀리 — 뭉친 근접이 옆으로 펼쳐져 '부채꼴 공격'처럼 보인다.
            // 시각 전용(Z만): 사거리·타게팅은 X축이라 불변. 적은 EnemyData라 (data is UnitData)=false → 자동 제외(아군 근접 한정).
            if (Motion == ActState.Attacking && data is UnitData ud && ud.role == UnitRole.Melee)
            {
                strength *= cfg != null ? cfg.meleeAttackFanStrengthMul : 2f;
                zBand   *= cfg != null ? cfg.meleeAttackFanBandMul     : 1.5f;
                overlapR *= cfg != null ? cfg.meleeAttackFanOverlapMul  : 1.3f;
            }

            if (overlapR <= 0f || strength <= 0f) return;

            float myX = transform.position.x;
            float myZ = transform.position.z;
            float push = 0f;
            var all = Targetables.All;
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i] as Combatant;
                if (c == null || c == this || c.IsDead || c.faction != faction) continue;
                float dx = c.transform.position.x - myX;
                if (Mathf.Abs(dx) > overlapR) continue;                 // X로 충분히 떨어졌으면 겹침 아님(sqrt 전 조기 컷)
                float dz = myZ - c.transform.position.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist > overlapR) continue;                          // XZ deadzone — 안 겹치면 밀지 않음(떨림 방지)
                float pushDir = Mathf.Abs(dz) > 1e-4f ? Mathf.Sign(dz)
                              : (SpawnSeq < c.SpawnSeq ? -1f : 1f);     // 같은 Z: SpawnSeq 순서로 결정적 분리
                push += pushDir * (overlapR - dist) / overlapR;         // 가까울수록 강하게
            }
            if (push == 0f) return;

            float newZ = myZ + push * strength * Time.deltaTime;
            float laneZ = _lane.laneZ;
            newZ = Mathf.Clamp(newZ, laneZ - zBand, laneZ + zBand);
            transform.position = new Vector3(myX, transform.position.y, newZ); // X·Y 불변, Z만 조정
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
                if (other.faction != faction || other.data == null || other.MaxHp <= 0f) continue;

                float ratio = other.CurrentHp / other.MaxHp;
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
            if (_hp <= 0f)
            {
                GrantKillReward();
                Destroy(gameObject);
            }
        }

        /// <summary>적 처치 시 플레이어(아군) 경제에 꿈에너지 지급. (설계 6번: 몬스터 처치 시 획득)</summary>
        private void GrantKillReward()
        {
            if (faction != Faction.Enemy) return;
            if (data is EnemyData ed && ed.killReward > 0f)
                EconomyRef?.AddMoney(ed.killReward);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead || data == null) return;
            _hp = Mathf.Min(MaxHp, _hp + amount);
        }

        /// <summary>승급(머지): 티어 1단계 상승(최대 3). 스탯 배수↑, 체력 완전회복, 크기↑·발 지면 재정렬. (설계 12번)</summary>
        public bool TryPromote()
        {
            if (Tier >= 3) return false;
            Tier++;
            _hp = MaxHp;                 // 체력 완전 회복(새 최대치)
            ApplyTierVisual();
            return true;
        }

        /// <summary>티어별 크기 변화(크기만, 리소스 불변). 스케일 후 발을 지면에 재정렬(가라앉음 방지).</summary>
        private void ApplyTierVisual()
        {
            float mul = Tier >= 3 ? 1.4f : (Tier == 2 ? 1.2f : 1f);
            transform.localScale = _baseScale * mul;
            if (_lane != null)
            {
                Vector3 p = transform.position;
                p.y = _lane.groundY + transform.localScale.y; // 프리미티브 bottom = pos.y - scale.y → groundY
                transform.position = p;
            }
        }
    }
}
