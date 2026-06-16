# gghf2 — Paper Heroes

Unity 6 기반 **단일 레인 디펜스/라인전** 프로토타입입니다.  
The Battle Cats / 성 디펜스류의 1D 자동 전투 구조를 따르며, **코드로 씬·데이터를 생성**하고 **CLI로 헤드리스 테스트**하는 워크플로를 전제로 개발 중입니다.

---

## 프로젝트 개요

| 항목 | 내용 |
|------|------|
| 프로젝트명 | `gghf2` |
| 게임 | **Paper Heroes** (종이 영웅) |
| 엔진 | Unity **6000.4.11f1** |
| 렌더링 | **URP 17.4.0** |
| 입력 | New Input System **1.19.0** |
| UI | UGUI + TextMeshPro |
| CLI 연동 | `com.yhc509.unity-cli-bridge` v0.3.1 |
| 저장소 | https://github.com/emptypizzaa/gghf2 |

---

## 게임 구조

```
PlayerBase (X=0)  ←── 아군 ──→     ←── 적군 ──→  EnemyBase (X=laneLength)
  HP 1000           돈 + 쿨다운 소환      웨이브 타임라인        HP 1500
```

- **승리**: 적 기지 HP 0
- **패배**: 아군 기지 HP 0
- 웨이브가 모두 끝나도 게임은 **자동 종료되지 않음** (기지 파괴만 승패 조건)
- 전투는 **물리 없음**, **X축 1D 거리**만 사용 (`transform.Translate`, 전역 Unit 레지스트리)

---

## 디렉터리 구조

```
Assets/
├── Scripts/PaperHeroes/     # 게임 핵심 (16개 .cs)
│   ├── Data/                # CharacterData, StageData, Enums
│   ├── Battle/              # BattleManager, Base, MoneyManager
│   ├── Unit/                # Unit FSM, Targeting
│   ├── Spawn/               # AllySpawner, WaveSpawner
│   ├── UI/                  # UnitButton, MoneyUI, BaseHPBar, ResultPanel
│   └── Editor/              # BattleSceneBuilder, PaperHeroesTestMenu
├── Scenes/
│   ├── GetStarted_Scene.unity   # Unity 튜토리얼 샘플 씬 (레거시)
│   └── Battle.unity               # ⚠️ 빌더 실행 후 생성됨
├── Data/                          # ⚠️ 빌더 실행 후 생성됨
│   ├── Characters/                # ally_tank, ally_melee, ally_ranged, enemy_*
│   └── Stage1.asset
├── SourceFiles/               # Unity Starter Assets 잔재 (3D 튜토리얼용, PH와 무관)
├── Tutorials/                 # Unity IET 튜토리얼 프레임워크
└── Prefabs/, Materials/, ...  # 튜토리얼 샘플 에셋
```

---

## 아키텍처

### 데이터 계층 (ScriptableObject)

- **`CharacterData`**: 유닛 스탯, 비용, 쿨다운, 역할(Role), 색상/프리팹
- **`StageData`**: 기지 HP, 경제, 레인 길이, 아군 로스터, 적 웨이브 타임라인
- 모든 밸런스 수치는 `.asset`에만 존재 — 코드에 매직넘버 없음

### 런타임

| 컴포넌트 | 역할 |
|----------|------|
| `BattleManager` | 상태기계 `Ready → Playing → Win/Lose`, 매치 초기화 |
| `Base` | 파괴 가능 거점, HP 이벤트 |
| `MoneyManager` | 시간당 돈 회복 (Playing 중만) |
| `Unit` | Move ↔ Attack FSM, 0.1초 틱 결정 |
| `Targeting` | X축 최근접 적 탐색 |
| `AllySpawner` | 돈 소비 + 쿨다운 기반 아군 소환 |
| `WaveSpawner` | 코루틴 웨이브 타임라인 (`Time.timeScale` 가속 가능) |

### UI

- `MoneyUI` — 상단 돈 표시
- `BaseHPBar` — 양측 기지 HP 바
- `UnitButton` — 아군 소환 버튼 (쿨다운 스윕, 비용/상태 반영)
- `ResultPanel` — WIN/LOSE + RESTART

### 에디터 도구

- **`BattleSceneBuilder`**: 데이터·씬·UI·와이어링을 코드로 일괄 생성 (멱등)
- **`PaperHeroesTestMenu`**: Play 모드 테스트 헬퍼 (유닛 스폰, 시간 가속, 상태 로그)

---

## 콘텐츠 (Stage 1 기본값)

### 아군 (3종)

| ID | 이름 | 역할 | 비용 | 쿨다운 |
|----|------|------|------|--------|
| `ally_tank` | 종이상자 탱커 | Tank | 80 | 6s |
| `ally_melee` | 종이칼 근접딜러 | MeleeDealer | 50 | 3s |
| `ally_ranged` | 종이활 원거리딜러 | RangedDealer | 90 | 5s |

### 적 (2종)

| ID | 이름 | 비고 |
|----|------|------|
| `enemy_mob` | 졸개 | 다수 웨이브 |
| `enemy_bushman` | 부시맨 | 고HP 보스형 |

### 웨이브 (Stage1)

| 시간(s) | 적 | 수량 | 간격(s) |
|---------|-----|------|---------|
| 3 | 졸개 | 3 | 2.0 |
| 18 | 졸개 | 4 | 1.5 |
| 30 | 부시맨 | 1 | — |
| 45 | 졸개 | 5 | 1.2 |
| 60 | 부시맨 | 2 | 4.0 |

### 스트레치 (M5, 미구현)

- `Role.Healer` enum 및 `CharacterData` 힐 필드만 존재 — MVP에서 스폰/로직 없음

---

## 시작하기

### 1. Unity 에디터에서 씬·데이터 생성

에디터 메뉴를 **순서대로** 실행:

1. `PaperHeroes → 1. Create Data Assets`
2. `PaperHeroes → 2. Build Battle Scene`

생성 결과:

- `Assets/Data/Characters/*.asset`
- `Assets/Data/Stage1.asset`
- `Assets/Scenes/Battle.unity` (Build Settings에 자동 추가)

### 2. 플레이

- `Battle.unity` 씬을 열고 Play
- 하단 버튼으로 아군 소환, 적은 웨이브 타임라인에 따라 자동 등장

### 3. CLI 테스트 (unity-cli-bridge)

콘솔 마커로 상태 검증:

| 로그 | 의미 |
|------|------|
| `PH_STATE=Playing` | 매치 시작 |
| `PH_WIN` | 승리 |
| `PH_LOSE` | 패배 |
| `PH_BUILD ...` | 빌더 완료 |
| `PH_TEST ...` | 테스트 메뉴 동작 |
| `PH_ERROR ...` | 오류 |

테스트 메뉴 (`PaperHeroes/Test/`):

- 유닛 스폰 (Tank/Melee/Ranged/Mob/Bushman)
- Fast Forward x8 (Time.timeScale 토글)
- Give Money 999
- Log State

---

## 패키지 의존성

```json
com.yhc509.unity-cli-bridge   // Git: v0.3.1
com.unity.cinemachine         // 3.1.6
com.unity.inputsystem         // 1.19.0
com.unity.learn.iet-framework // 5.0.3 (튜토리얼)
com.unity.render-pipelines.universal // 17.4.0
com.unity.ugui                // 2.0.0
```

---

## 현재 상태 (분석 기준)

### 완료

- Paper Heroes 런타임/에디터 스크립트 16개
- 씬 빌더 + 테스트 메뉴
- `.gitignore` (Unity 표준)
- Git push 완료 (`main`)

### 미완 / 수동 실행 필요

- `Assets/Data/` — **빌더 메뉴 1번 미실행 시 없음**
- `Assets/Scenes/Battle.unity` — **빌더 메뉴 2번 미실행 시 없음**
- Build Settings 기본 씬은 현재 `GetStarted_Scene.unity`만 등록 (빌더 2번 실행 시 `Battle.unity`가 index 0에 추가)

### 레거시 (Paper Heroes와 무관)

Unity "Get started with Unity" 튜토리얼 잔재:

- `Assets/SourceFiles/` — ThirdPersonController, Pickup, RespawnPlayer 등
- `Assets/Tutorials/` — IET 튜토리얼
- `Assets/Scenes/GetStarted_Scene.unity`

#### 레거시 스크립트 알려진 이슈

| 파일 | 이슈 |
|------|------|
| `UpdateCollectibleCount.cs` | 존재하지 않는 `Collectible` 타입 검색 → 카운트 0 고정 |
| `TutorialCallbacks.cs` | `EnableGameObject`가 `SetActive(false)` 호출 (버그) |
| `TutorialCallbacks.cs` | `UseMainCamera()` 미구현 (빈 메서드) |
| `RespawnPlayer.cs` | private 필드 리플렉션 접근, 회전 90° 하드코딩 |

---

## 설계 특징

1. **코드 기반 씬 빌드** — 에셋/씬/와이어링을 에디터 메뉴 2개로 재현 가능 → CI/CLI 자동화에 적합
2. **헤드리스 테스트** — ASCII 로그 마커 + `Time.timeScale` 가속
3. **물리 없는 1D 전투** — 결정론적, 디버깅 단순
4. **데이터 주도 설계** — ScriptableObject로 밸런스 분리

---

## MVP 마일스톤 (참고)

| 단계 | 내용 | 상태 |
|------|------|------|
| M0 | 베이스 씬 빌드 & 검증 | 빌더 준비됨, 씬 생성 대기 |
| M1 | 단일 유닛 행군 + 기지 타격 (WIN) | 스크립트 준비됨 |
| M2 | 전투: 웨이브, 타게팅, 사망 | 스크립트 준비됨 |
| M3 | 돈, 로스터, 쿨다운, UI | 스크립트 준비됨 |
| M5 | Healer (스트레치) | enum/필드만 |

---

## 라이선스 / 기여

- Unity 튜토리얼 템플릿 및 Starter Assets는 Unity Technologies 라이선스 적용
- Paper Heroes 스크립트: 프로젝트 소유 (emptypizzaa/gghf2)
