using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 1라인 디펜스의 전장 = X축 1D 라인.
    /// 모든 위치 계산은 X 스칼라로 처리하고(내비/패스파인딩 없음), Y/Z는 시각 배치용으로 고정한다.
    /// 아군 거점은 왼쪽(-X), 적 거점은 오른쪽(+X)에 둔다.
    /// </summary>
    public class Lane : MonoBehaviour
    {
        [Tooltip("아군 거점 X 좌표(왼쪽 끝)")]
        public float allyBaseX = -10f;

        [Tooltip("적 거점 X 좌표(오른쪽 끝)")]
        public float enemyBaseX = 10f;

        [Tooltip("유닛이 서는 지면 높이")]
        public float groundY = 0f;

        [Tooltip("라인의 Z 위치(1라인이므로 고정)")]
        public float laneZ = 0f;

        /// <summary>라인 전체 길이.</summary>
        public float Length => Mathf.Abs(enemyBaseX - allyBaseX);

        /// <summary>해당 진영이 전진하는 방향(+1: 오른쪽, -1: 왼쪽).</summary>
        public int ForwardDir(Faction f) => f == Faction.Ally ? 1 : -1;

        /// <summary>해당 진영의 스폰(자기 거점) X 좌표.</summary>
        public float SpawnX(Faction f) => f == Faction.Ally ? allyBaseX : enemyBaseX;

        /// <summary>해당 진영이 공격하는 상대 거점 X 좌표.</summary>
        public float TargetBaseX(Faction f) => f == Faction.Ally ? enemyBaseX : allyBaseX;

        /// <summary>X 스칼라를 라인 위 월드 좌표로 변환.</summary>
        public Vector3 PointAt(float x) => new Vector3(x, groundY, laneZ);
    }
}
