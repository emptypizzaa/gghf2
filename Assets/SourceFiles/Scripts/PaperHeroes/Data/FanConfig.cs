using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 겹침 회피 "부채꼴" 연출 튜닝(시각 전용 Z 분리). `Combatant`가 `Resources/FanConfig`에서 읽는다.
    /// 전투/타게팅은 X축만 쓰므로 Z 분리는 **게임플레이에 영향 0** — 순수 연출. 인스펙터로 손맛 조절.
    /// </summary>
    [CreateAssetMenu(fileName = "FanConfig", menuName = "PaperHeroes/Fan Config")]
    public class FanConfig : ScriptableObject
    {
        [Tooltip("이 거리(XZ)보다 가까운 같은 진영 유닛끼리만 Z로 밀어낸다(=겹침 판정 반경). 대략 캡슐 폭. 0=분리 끔.")]
        public float overlapRadius = 0.8f;

        [Tooltip("Z 분리로 밀어내는 속도(유닛/초). 클수록 빨리 부채꼴로 펼쳐진다.")]
        public float zSeparationStrength = 3f;

        [Tooltip("라인 중심(laneZ)에서 벗어날 수 있는 최대 Z(±). 유닛이 라인 밖으로 흩어지지 않게 클램프.")]
        public float zBand = 1.8f;
    }
}
