# 페이퍼 히어로즈 (Paper Heroes)

> 시간에 차오르는 돈으로 **종이공작 용병**을 뽑아 적 거점을 부수는 **1라인 디펜스**.
> 악몽에 시달리는 아이들이 꿈속에서 종이상자·종이무기로 무장해 빌런 ‘부기맨’을 막는다.
> 재미 검증 프로토타입 · 핵심 레퍼런스: 냥코 대전쟁 · Unity 6 · 캐주얼 싱글 · 모바일

## 코어 루프
자원 매초 회복(상한) → 버튼으로 용병 소환(비용·유닛별 쿨다운) → 유닛이 1D 라인을 따라 자동 진군 → 사거리 교전 → **적 거점 HP 0 = 승리 / 아군 거점 함락 = 패배** → 결과 패널 → 재시작

## 구현 현황
- **아군 4종**(데이터 주도): 종이상자 탱커 · 종이칼 근접딜러 · 종이활 원거리딜러(발사체 화살) · 종이지팡이 힐러
- **적 2종**: 졸개 · 부기맨(보스)
- **시스템**: 자원/쿨다운 소환 UI · 1D 전투 + **전선 블로킹(밀당)** · 웨이브 스폰(스테이지 1·2) · **발사체(아크 화살)** · 힐 · 승패/재시작
- **비주얼**: SD 복셀 룩(밝은 데이라이트 · **Point 필터로 크리스프**) · 3D 모델 적용(glTFast) · 3/4 아이소 카메라 · 한글 폰트(나눔고딕)

## 기술 스택
- Unity **6000.4.11f1** · URP 17 · Input System · TextMeshPro · **glTFast**(.glb 임포트)
- **데이터-로직 분리**: 모든 수치를 ScriptableObject(`UnitData`/`EnemyData`/`EconomyConfig`/`WaveData`)에 둬 코드 수정 없이 튜닝
- **unity-cli-bridge**: CLI로 에디터를 원격 제어하며 개발(씬/프리팹/플레이/스크린샷)

## 프로젝트 구조
```
Assets/
  Scenes/PaperHeroes_Game.unity          # 게임 씬
  SourceFiles/
    Scripts/PaperHeroes/Core/            # Lane, BaseController, MatchManager, Combatant,
                                         #   UnitSpawner, Projectile, ModelAnimator,
                                         #   Economy, SummonController, MatchResultUI, WaveSpawner ...
    Scripts/PaperHeroes/Data/            # CombatantData, UnitData, EnemyData, EconomyConfig, WaveData
    Data/Units|Enemies|Waves             # 유닛/적/웨이브/경제 SO 에셋
  Test_Ch.glb                            # 근접 캐릭터 모델 (Git LFS)
```

## 실행
1. Unity Hub에서 본 프로젝트를 **6000.4.11f1**로 연다
2. `Assets/Scenes/PaperHeroes_Game.unity` 를 연다
3. **Play** → 하단 버튼으로 용병을 소환한다
   - 팁: 원거리(종이활)는 **탱커 뒤**에 두면 갭 너머로 화살을 쏜다
   - 에디터 Game 뷰는 **창을 크게**(고해상)·**포커스** 상태로 보아야 선명하고 유닛이 움직인다

## 상태 / 범위
재미 검증 프로토타입(작업 브랜치 `paper-heroes-mvp`). 3가지 재미(자원 타이밍 · 전선 밀당 · 포지션 조합)를 플레이테스트하는 것이 목적.
**범위 밖(OUT):** 가챠/성장 · 스토리/연출 · 세이브 · 멀티 · 정식 아트 · 정식 밸런싱.

## 팀
MarcC(Unity 베이스·리포·통합) · 토도(복셀 아트·공동기획) · 에그(기획 리딩·데이터 테이블) · Claude Code(코어 로직 구현)
