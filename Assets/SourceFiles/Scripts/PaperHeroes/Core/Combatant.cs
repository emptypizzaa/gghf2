using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 라인 위의 전투 유닛(아군/적 공용). 1D(X축)로 적 거점을 향해 전진하고,
    /// 사거리 안에 들어오면 멈춰서 거점을 주기적으로 공격한다.
    /// M1 범위: 거점 타격만. 유닛 vs 유닛 교전·사망 연출은 M2에서 확장.
    /// 수치는 전부 CombatantData(SO)에서 읽는다(데이터-로직 분리).
    /// </summary>
    public class Combatant : MonoBehaviour
    {
        public CombatantData data;
        public Faction faction;

        private Lane _lane;
        private BaseController _targetBase;
        private float _hp;
        private float _attackTimer;

        public float CurrentHp => _hp;
        public bool IsDead => _hp <= 0f;

        /// <summary>스폰 시 스포너가 호출. 데이터/진영/라인을 주입한다.</summary>
        public void Init(CombatantData data, Faction faction, Lane lane)
        {
            this.data = data;
            this.faction = faction;
            _lane = lane;
            if (data != null) _hp = data.maxHp;
        }

        private void Start()
        {
            if (_lane == null) _lane = FindFirstObjectByType<Lane>();
            if (data != null && _hp <= 0f) _hp = data.maxHp; // Init에서 이미 설정됐으면 유지

            // 상대 진영 거점을 타겟으로.
            foreach (var b in FindObjectsByType<BaseController>(FindObjectsSortMode.None))
            {
                if (b.faction != faction) { _targetBase = b; break; }
            }
        }

        private void Update()
        {
            if (data == null || _lane == null) return;

            float x = transform.position.x;
            float targetX = _lane.TargetBaseX(faction);

            if (Mathf.Abs(targetX - x) > data.attackRange)
            {
                // 적 거점 방향으로 전진(1D).
                float step = _lane.ForwardDir(faction) * data.moveSpeed * Time.deltaTime;
                transform.position += new Vector3(step, 0f, 0f);
            }
            else
            {
                // 사거리 안 → 거점 공격.
                _attackTimer += Time.deltaTime;
                if (_attackTimer >= data.attackInterval)
                {
                    _attackTimer = 0f;
                    if (_targetBase != null && !_targetBase.IsDestroyed)
                    {
                        _targetBase.TakeDamage(data.attackDamage);
                    }
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead) return;
            _hp -= amount;
            if (_hp <= 0f) Destroy(gameObject);
        }
    }
}
