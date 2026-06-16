using System;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 거점(아군/적). HP를 가지며 0이 되면 파괴 이벤트를 쏜다 → 승패 판정의 트리거.
    /// IDamageable이므로 유닛에게 "또 하나의 타겟"으로 취급된다(전선을 뚫으면 거점을 때린다).
    /// 적 거점은 스스로 공격하지 않고 WaveSpawner로 적을 스폰만 한다.
    /// </summary>
    public class BaseController : MonoBehaviour, IDamageable
    {
        [Tooltip("이 거점의 진영")]
        public Faction faction;

        [Tooltip("거점 최대 체력 (※ 스테이지 수치 — 추후 StageData SO로 이전 예정)")]
        public float maxHp = 1000f;

        public float CurrentHp { get; private set; }
        public bool IsDestroyed => CurrentHp <= 0f;

        /// <summary>HP가 0이 되어 파괴된 순간 1회 발생.</summary>
        public event Action<BaseController> Destroyed;

        // IDamageable
        Faction IDamageable.Faction => faction;
        float IDamageable.PositionX => transform.position.x;
        bool IDamageable.IsDead => IsDestroyed;

        private void Awake()
        {
            CurrentHp = maxHp;
        }

        private void OnEnable() => Targetables.Register(this);
        private void OnDisable() => Targetables.Unregister(this);

        public void TakeDamage(float amount)
        {
            if (IsDestroyed || amount <= 0f) return;

            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            if (CurrentHp <= 0f)
            {
                Destroyed?.Invoke(this);
            }
        }
    }
}
