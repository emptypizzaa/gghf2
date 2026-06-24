# 페이퍼 히어로즈 (Paper Heroes)

> 시간에 차오르는 꿈에너지로 **종이공작 용병**을 소환해 적 거점을 부수는 **1라인 디펜스**.
> 악몽에 시달리는 아이들이 꿈속에서 종이상자·종이무기로 무장해 빌런 **부기맨**을 막는다.

| | |
|---|---|
| **레퍼런스** | 냥코 대전쟁 (자원 타이밍 · 전선 밀당 · 포지션 조합) |
| **엔진** | Unity **6000.4.11f1** · URP 17 |
| **플랫폼** | 모바일 · WebGL · Android (IL2CPP) |
| **브랜치** | `paper-heroes-mvp` |
| **저장소** | https://github.com/emptypizzaa/gghf2 |

---

## 코어 루프

```
인트로 스토리(앱당 1회) → 전투 시작
  → 꿈에너지 자동 회복(상한)
  → 용병 소환(비용 · 쿨다운 · 동시 배치 제한)
  → 1D 라인 자동 진군 · 사거리 교전
  → 적 거점 HP 0 = 승리 / 아군 거점 함락 = 패배
  → 결과 패널 → 재시작
```

---

## 구현 현황

### 전투
- **아군 4종** (ScriptableObject): 종이상자 탱커 · 종이칼 근접 · 종이활 원거리(아크 화살) · 종이지팡이 힐러
- **적 2종**: 졸개(빨간 캡슐) · 부기맨 보스(복셀 Boogeyman)
- 1D(X축) 이동·교전 · 방어력 경감 · 사망 애니메이션
- 웨이브 스폰(스테이지 1·2) · 승패/재시작 · 전투 HUD(거점 HP · 나가기)

### 소환·경제
- 자원(꿈에너지) 매초 회복 · 유닛별 소환 비용·쿨다운
- **강화(승급)** 버튼 — 소환 비용의 2배
- **동시 배치 제한** (기본 5기) · 용병 수 +1 확장

### 연출
- **인트로**: 커버 → 프롤로그 슬라이드 → 시네마틱 영상 → 타이틀 (런타임 UI, `timeScale=0`으로 전투 게이트)
- BGM 루프 · 내레이션 · 복셀 SD 유닛(glTFast) · Point 필터 텍스처 · 3/4 아이소 카메라
- 아군 부채꼴 Z 분리 연출 (`FanConfig`) · 힐 VFX · 무기 소켓(활)

### 비주얼 파이프라인
프리미티브(캡슐+색) → `visualPrefab` glb 교체 → 애니메이션 클립 연결. 코드 수정 없이 SO 에셋만 바꿔 튜닝.

---

## 기술 스택

- Unity 6000.4.11f1 · URP 17 · Input System · TextMeshPro · **glTFast**
- **데이터-로직 분리**: `UnitData` · `EnemyData` · `EconomyConfig` · `WaveData` · `StageData` · `FanConfig`
- **unity-cli-bridge** — CLI로 에디터 원격 제어
- 빌드: `Assets/Editor/AndroidBuilder.cs` · `WebGLBuilder.cs`

---

## 프로젝트 구조

```
Assets/
  Scenes/PaperHeroes_Game.unity       # 메인 게임 씬
  glb260620/                          # 복셀 캐릭터 .glb (Git LFS)
  SourceFiles/
    Scripts/PaperHeroes/
      Core/                           # MatchManager, Combatant, Lane, Economy,
                                      #   SummonController, WaveSpawner, IntroController ...
      Data/                           # CombatantData, StageData, FanConfig ...
    Data/
      Units/                          # Tank, Melee, Ranged, Healer SO
      Enemies/                        # Zolgae, Boogeyman SO
      Waves/                          # WaveData_Stage1, Stage2
    Resources/
      Stage1.asset                    # 스테이지 진입점
      Story/                          # 인트로 이미지·BGM·내레이션·영상
  StreamingAssets/Story/              # 시네마틱 mp4 (런타임 스트리밍)
  Editor/                             # Android/WebGL 빌드, 스토리 에셋 후처리
```

---

## 실행

1. Unity Hub에서 **6000.4.11f1**로 프로젝트를 연다
2. `Assets/Scenes/PaperHeroes_Game.unity` 를 연다
3. **Play** — 첫 실행 시 인트로 → 타이틀 **시작** 클릭 후 전투
4. 하단 버튼으로 용병 소환 · 상단 **강화**로 유닛 승급

**팁**
- 원거리(종이활)는 탱커 뒤에 두면 갭 너머로 화살을 쏜다
- Game 뷰를 크게·포커스 상태로 두면 복셀 텍스처가 선명하고 유닛이 정상 이동한다

---

## 범위

**목적:** 재미 검증 프로토타입 — 자원 타이밍 · 전선 밀당 · 포지션 조합 플레이테스트

| IN | OUT |
|----|-----|
| 1라인 전투 · 소환·강화 · 웨이브 | 가챠 · 메타 성장 · 세이브 |
| 인트로 스토리(프로토) | 정식 스토리/연출 |
| 복셀 프로토 아트 | 정식 아트 · 정식 밸런싱 |
| Android · WebGL 빌드 | 멀티 |

---

## 팀

MarcC (Unity · 리포 · 통합) · 토도 (복셀 아트 · 공동기획) · 에그 (기획 · 데이터 테이블) · Claude Code (코어 로직)
