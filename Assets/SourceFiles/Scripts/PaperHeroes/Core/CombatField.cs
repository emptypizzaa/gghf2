using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// Combatant 공간 질의 가속기(1D / X축). 기존엔 모든 Combatant가 매 프레임 Targetables.All
    /// 전체를 선형 스캔(타게팅·힐·부채꼴·줄유지) → 전체 O(n²). 이 클래스는 프레임당 1회만
    /// (Time.frameCount 키) 살아있는 타겟을 X 기준 정렬 + 진영별 SpawnSeq 순서로 캐시하고,
    /// 각 질의를 이진탐색 + 사거리 국소 확장으로 처리한다 → 프레임당 O(n log n) + 질의당 O(log n + 사거리내 수).
    ///
    /// 위치는 스냅샷(프레임 시작 무렵) 값으로 "후보 창(window) 선택"에만 쓰고, 실제 거리·HP·Z는
    /// 질의 시점에 라이브로 읽는다(프레임 중 이동량 ≈ 속도×dt 는 kMargin으로 흡수). IsDead는 항상
    /// 라이브 확인 → 같은 프레임에 죽은 유닛은 즉시 제외(기존 동작 보존). 새로 스폰된 유닛은 다음
    /// 프레임부터 후보에 포함(1프레임 지연 — 시각적으로 무시 가능).
    /// </summary>
    public static class CombatField
    {
        private struct Node { public IDamageable t; public float x; }

        // X 정렬 스냅샷(사거리 질의용) / 진영별 SpawnSeq 정렬 리스트(줄유지=리더 캡용) / 전선 비힐러(힐러 standoff용).
        private static readonly List<Node> _sorted = new List<Node>();
        private static readonly List<Combatant> _allyUnits = new List<Combatant>();
        private static readonly List<Combatant> _enemyUnits = new List<Combatant>();
        private static Combatant _allyFront;
        private static Combatant _enemyFront;

        private static int _builtFrame = -1;

        // 프레임 중 유닛 X 이동(속도×dt, 보통 <0.1)으로 인한 창 누락 방지 마진. 후보만 넓힐 뿐
        // 실제 채택 조건(사거리/overlap)은 호출부가 라이브 좌표로 다시 검사하므로 결과는 불변.
        private const float kMargin = 0.25f;

        private static readonly Comparison<Node> _byX = (a, b) => a.x.CompareTo(b.x);
        private static readonly Comparison<Combatant> _bySeq = (a, b) => a.SpawnSeq.CompareTo(b.SpawnSeq);

        // Enter Play Mode Options에서 도메인 리로드를 꺼도 정적 캐시가 오염되지 않도록 매 플레이 시작 시 초기화.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            _builtFrame = -1;
            _sorted.Clear(); _allyUnits.Clear(); _enemyUnits.Clear();
            _allyFront = null; _enemyFront = null;
        }

        private static void EnsureBuilt()
        {
            int frame = Time.frameCount;
            if (frame == _builtFrame) return;
            _builtFrame = frame;

            _sorted.Clear(); _allyUnits.Clear(); _enemyUnits.Clear();
            _allyFront = null; _enemyFront = null;

            var all = Targetables.All;
            for (int i = 0; i < all.Count; i++)
            {
                var t = all[i];
                if (t == null || t.IsDead) continue;
                _sorted.Add(new Node { t = t, x = t.PositionX });

                var c = t as Combatant;
                if (c == null) continue;                            // 거점(BaseController) 등 비전투원은 사거리 질의에만 포함
                if (c.faction == Faction.Ally) _allyUnits.Add(c); else _enemyUnits.Add(c);

                // 전선(가장 전진한) 비힐러 — 진영 forward 방향 기준(아군=+X, 적=-X).
                if (c.data == null || !c.data.isHealer)
                {
                    if (c.faction == Faction.Ally)
                    {
                        if (_allyFront == null || c.transform.position.x > _allyFront.transform.position.x) _allyFront = c;
                    }
                    else
                    {
                        if (_enemyFront == null || c.transform.position.x < _enemyFront.transform.position.x) _enemyFront = c;
                    }
                }
            }

            _sorted.Sort(_byX);
            _allyUnits.Sort(_bySeq);   // 레지스트리 삽입 순서가 곧 SpawnSeq지만, 그 가정에 의존하지 않도록 방어적 정렬.
            _enemyUnits.Sort(_bySeq);
        }

        /// <summary>X 구간 [xMin, xMax](드리프트 마진 포함)에 걸치는 정렬 스냅샷 인덱스 구간 [lo, hi)를 돌려준다.</summary>
        public static void NeighborsInX(float xMin, float xMax, out int lo, out int hi)
        {
            EnsureBuilt();
            lo = LowerBound(xMin - kMargin);
            hi = LowerBound(xMax + kMargin);
        }

        /// <summary>NeighborsInX가 돌려준 인덱스의 타겟. 호출부는 라이브 IsDead/사거리를 다시 검사한다.</summary>
        public static IDamageable At(int i) => _sorted[i].t;

        /// <summary>(줄 유지) self와 같은 진영, self보다 먼저 스폰된(SpawnSeq 작은) 생존 유닛 중 바로 앞(SpawnSeq 최대). 없으면 null.</summary>
        public static Combatant Leader(Combatant self)
        {
            EnsureBuilt();
            var list = self.faction == Faction.Ally ? _allyUnits : _enemyUnits;
            // SpawnSeq 오름차순 정렬 → self.SpawnSeq의 lower bound 직전이 "바로 앞" 후보.
            int lo = 0, hi = list.Count;
            while (lo < hi) { int mid = (lo + hi) >> 1; if (list[mid].SpawnSeq < self.SpawnSeq) lo = mid + 1; else hi = mid; }
            for (int i = lo - 1; i >= 0; i--)   // 죽은 앞 유닛은 건너뛰고 다음으로 먼저 스폰된 생존 유닛이 리더.
            {
                var c = list[i];
                if (c != null && c != self && !c.IsDead) return c;
            }
            return null;
        }

        /// <summary>해당 진영에서 가장 전진한 비힐러 전투원(힐러 standoff 기준). 없으면 null.</summary>
        public static Combatant FrontNonHealer(Faction faction)
        {
            EnsureBuilt();
            return faction == Faction.Ally ? _allyFront : _enemyFront;
        }

        // _sorted에서 x >= v 인 첫 인덱스(없으면 Count).
        private static int LowerBound(float v)
        {
            int lo = 0, hi = _sorted.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (_sorted[mid].x < v) lo = mid + 1; else hi = mid;
            }
            return lo;
        }
    }
}
