using System;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// A destructible base at one end of the lane. Only takes damage (never attacks).
    /// Fires events so HP bars and the BattleManager can react.
    /// </summary>
    public class Base : MonoBehaviour
    {
        public Team Team { get; private set; }
        public float MaxHP { get; private set; }
        public float CurrentHP { get; private set; }
        public bool IsDestroyed { get; private set; }

        public event Action<Base> OnDamaged;
        public event Action<Base> OnDestroyed;

        public float HPRatio => MaxHP > 0f ? Mathf.Clamp01(CurrentHP / MaxHP) : 0f;

        public void Init(Team team, float maxHP)
        {
            Team = team;
            MaxHP = maxHP;
            CurrentHP = maxHP;
            IsDestroyed = false;
            OnDamaged?.Invoke(this);
        }

        public void TakeDamage(float amount)
        {
            if (IsDestroyed) return;
            CurrentHP -= amount;
            if (CurrentHP < 0f) CurrentHP = 0f;
            OnDamaged?.Invoke(this);
            if (CurrentHP <= 0f)
            {
                IsDestroyed = true;
                OnDestroyed?.Invoke(this);
            }
        }
    }
}
