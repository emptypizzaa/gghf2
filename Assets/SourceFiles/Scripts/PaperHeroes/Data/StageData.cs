using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 한 스테이지의 단일 진입점(워드 20번 스테이지 테이블). 매치 부팅에 필요한 모든 데이터를
    /// 여기 한 곳에서 참조한다 — MatchManager가 이걸 읽어 거점 HP/경제/웨이브/로스터를 주입한다.
    /// 데이터-로직 분리: 코드 수정 없이 이 에셋만 바꿔 스테이지를 튜닝한다.
    /// </summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "PaperHeroes/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("식별")]
        public string stageId;
        public string displayName;

        [Header("거점 HP")]
        [Tooltip("아군 거점 최대 체력")]
        public float allyBaseHp = 1000f;

        [Tooltip("적 거점 최대 체력")]
        public float enemyBaseHp = 1000f;

        [Header("참조")]
        [Tooltip("적 스폰 타임라인")]
        public WaveData wave;

        [Tooltip("자원(꿈에너지) 경제 설정")]
        public EconomyConfig economy;

        [Tooltip("이 스테이지에서 소환 가능한 용병(프로토: 탱커·근딜·원거리 3종, 힐러 제외)")]
        public UnitData[] roster;

        [Header("메타(프로토 미사용 스텁)")]
        [Tooltip("제한 시간(초). 프로토 OUT — 승리는 적 거점 격파. 0=무제한.")]
        public float timeLimit = 0f;

        [Tooltip("클리어 보상(메타). 프로토 미사용.")]
        public int reward = 0;

        [Tooltip("등장 몬스터 레벨(메타/성장). 프로토 미사용.")]
        public int monsterLevel = 1;
    }
}
