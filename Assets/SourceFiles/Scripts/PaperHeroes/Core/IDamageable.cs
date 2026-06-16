using System.Collections.Generic;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>피격 가능한 전장 요소(유닛/거점)의 공통 타겟 인터페이스.</summary>
    public interface IDamageable
    {
        Faction Faction { get; }
        float PositionX { get; }
        bool IsDead { get; }
        void TakeDamage(float amount);
    }

    /// <summary>
    /// 모든 피격 대상(유닛·거점)의 런타임 레지스트리.
    /// 타겟 탐색 시 매 프레임 FindObjects를 도는 대신 이 리스트를 스캔한다.
    /// </summary>
    public static class Targetables
    {
        public static readonly List<IDamageable> All = new List<IDamageable>();

        public static void Register(IDamageable t)
        {
            if (t != null && !All.Contains(t)) All.Add(t);
        }

        public static void Unregister(IDamageable t)
        {
            All.Remove(t);
        }

        // Enter Play Mode Options에서 도메인 리로드를 꺼도 정적 리스트가 오염되지 않도록 매 플레이 시작 시 초기화.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => All.Clear();
    }
}
