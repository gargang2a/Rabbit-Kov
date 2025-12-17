# 🐰 적 시스템 개선 + 메가봉크 보스 구현 보고서

**작성일**: 2024-12-17  
**프로젝트**: Rabbit-Kov (Rabbit Protocol)

---

## 📋 프로젝트 개요

### 목표

- 기존 Enemy AI를 **병렬 FSM 구조**로 리팩토링
- **메가봉크 보스** 3페이즈 시스템 구현
- 폴더 구조 통합 및 정리

### 결과 요약

| Phase               | 완료 상태                                |
| ------------------- | ---------------------------------------- |
| Phase 0~3           | ✅ 완료                                  |
| Phase 4 (보스)      | 🔄 70% (스크립트 완료, 프리팹/에셋 필요) |
| Phase 5 (폴더 통합) | ✅ 완료                                  |

---

## 🏗️ Phase 0~3: 적 시스템 리팩토링

### 병렬 FSM 아키텍처

```mermaid
flowchart TB
    subgraph EnemyController
        direction LR
        subgraph MovementFSM["🚶 MovementFSM"]
            Patrol --> Chase
            Chase --> Stopped
            Chase --> Return
            Return --> Patrol
        end
        subgraph CombatFSM["⚔️ CombatFSM"]
            Inactive --> Ready
            Ready --> Windup
            Windup --> Attacking
            Attacking --> Recovery
        end
    end
```

### 구현 항목

- **MovementFSM**: `PatrolState`, `ChaseState`, `StoppedState`, `ReturnState`, `WaitState`
- **CombatFSM**: `CombatInactiveState`, `CombatReadyState`, `CombatWindupState`, `CombatAttackingState`, `CombatRecoveryState`
- **데이터 분리**: `EnemyDataSO`, `EnemyAttackDataSO`로 ScriptableObject 기반 설정
- **NavMesh 최적화**: `pathfindingIterationsPerFrame` 설정 (100~1000)

---

## 🐰 Phase 4: 메가봉크 보스

### 페이즈 시스템

| 페이즈 | 체력 구간  | 공격 패턴                 |
| ------ | ---------- | ------------------------- |
| **1**  | 100% ~ 66% | 에어본 (플레이어 띄우기)  |
| **2**  | 66% ~ 33%  | 바닥 스파이크 (3갈래)     |
| **3**  | 33% ~ 0%   | 회전 레이저 (4갈래, 360°) |

### 생성된 스크립트

| 파일                                                                                                                                      | 역할                                 |
| ----------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------ |
| [BossController.cs](file:///c:/Users/leeja/Documents/Rabbit-Kov/Assets/Game/01_Scripts/02_Enemy/Boss/BossController.cs)                   | 보스 컨트롤러 (EnemyController 상속) |
| [BossPhaseManager.cs](file:///c:/Users/leeja/Documents/Rabbit-Kov/Assets/Game/01_Scripts/02_Enemy/Boss/BossPhaseManager.cs)               | 페이즈 전환 관리                     |
| [BossDataSO.cs](file:///c:/Users/leeja/Documents/Rabbit-Kov/Assets/Game/01_Scripts/02_Enemy/Data/Boss/BossDataSO.cs)                      | 보스 설정 ScriptableObject           |
| [IBossAttack.cs](file:///c:/Users/leeja/Documents/Rabbit-Kov/Assets/Game/01_Scripts/02_Enemy/Boss/Attacks/IBossAttack.cs)                 | 공격 인터페이스                      |
| [AirborneAttack.cs](file:///c:/Users/leeja/Documents/Rabbit-Kov/Assets/Game/01_Scripts/02_Enemy/Boss/Attacks/AirborneAttack.cs)           | 1페이즈: 에어본                      |
| [GroundSpikeAttack.cs](file:///c:/Users/leeja/Documents/Rabbit-Kov/Assets/Game/01_Scripts/02_Enemy/Boss/Attacks/GroundSpikeAttack.cs)     | 2페이즈: 3갈래 스파이크              |
| [RotatingLaserAttack.cs](file:///c:/Users/leeja/Documents/Rabbit-Kov/Assets/Game/01_Scripts/02_Enemy/Boss/Attacks/RotatingLaserAttack.cs) | 3페이즈: 회전 레이저                 |
| [BossHealthBar.cs](file:///c:/Users/leeja/Documents/Rabbit-Kov/Assets/Game/01_Scripts/02_Enemy/Boss/UI/BossHealthBar.cs)                  | 체력바 UI                            |

---

## 📁 Phase 5: 폴더 구조 통합

### 최종 폴더 구조

```
Assets/Game/01_Scripts/
└── 02_Enemy/
    ├── Boss/                      ← 보스 시스템
    │   ├── BossController.cs
    │   ├── BossPhaseManager.cs
    │   ├── Attacks/              (IBossAttack, 공격 패턴들)
    │   └── UI/                   (BossHealthBar)
    │
    ├── Data/                      ← 모든 DataSO 통합
    │   ├── Normal/               (EnemyDataSO, EnemyAttackDataSO)
    │   ├── Boss/                 (BossDataSO)
    │   └── Common/               (EnemyTier)
    │
    ├── Normal/                    ← 일반 적 시스템
    │   ├── EnemyController.cs
    │   ├── EnemyStats.cs
    │   ├── EnemySenses.cs
    │   ├── EnemyMovement.cs
    │   ├── EnemyCombat.cs
    │   ├── EnemySpawner.cs
    │   └── States/               (Movement/, Combat/)
    │
    └── StateMachine/              ← FSM 인프라
        ├── IMovementState.cs
        ├── ICombatState.cs
        ├── MovementFSM.cs
        └── CombatFSM.cs
```

### 변경 사항

| 기존             | 신규                     |
| ---------------- | ------------------------ |
| `02_Monster/`    | `02_Enemy/Normal/`       |
| `03_Boss/`       | `02_Enemy/Boss/`         |
| 분산된 Data 폴더 | `02_Enemy/Data/` 통합    |
| 없음             | `02_Enemy/StateMachine/` |

---

## 🔧 추가 수정 사항

### 버그 수정

- **EnemyStats.SetMaxHealth()** 메서드 추가 (BossController에서 필요)

### NavMesh 최적화

- `pathfindingIterationsPerFrame`: 100~1000 범위 설정 가능
- 경로 갱신 주기: 매 프레임 (0초)

---

## 📋 남은 작업 (Unity 에디터)

- [ ] 보스 프리팹 생성
- [ ] BossDataSO 에셋 생성
- [ ] 공격 프리팹 생성 (각 공격에 스크립트 연결)
- [ ] 이펙트/파티클 에셋 연결
- [ ] 플레이어 데미지 연동 (PlayerHealth)
- [ ] 보스 Zone 트리거 구현
- [ ] 테스트 및 밸런싱

---

## ✅ 검증 결과

- Unity 컴파일: **성공** ✅
- 폴더 구조 이동: **완료** ✅
- 경고: 미사용 변수 관련 (무시 가능)

---

## 📊 상세 클래스 다이어그램

### 일반 적 시스템 (EnemyController)

```mermaid
classDiagram
    class EnemyController {
        -EnemyDataSO _enemyData
        -MovementFSM _movementFSM
        -CombatFSM _combatFSM
        +EnemyStats Stats
        +EnemyMovement Movement
        +EnemySenses Senses
        +EnemyCombat Combat
        +Transform CurrentTarget
        +EnemyTier Tier
        +bool RestrictToZone
        +SetTarget(Transform)
        +ClearTarget()
        +LockMovement()
        +UnlockMovement()
    }

    class EnemyStats {
        -int _maxHealth
        -int _currentHealth
        +event OnHealthChanged
        +event OnDeath
        +TakeDamage(int, Vector3, Vector3)
        +Heal(int)
        +SetMaxHealth(int)
    }

    class EnemyMovement {
        -NavMeshAgent _agent
        -float _patrolSpeed
        -float _chaseSpeed
        +MoveTo(Vector3)
        +MoveToRandomPoint()
        +FaceTarget(Vector3)
        +Stop()
        +SetChaseSpeed()
        +SetPatrolSpeed()
    }

    class EnemySenses {
        -float _sightRadius
        -float _fieldOfView
        +DetectPlayer()
        +CheckTargetVisible(Transform)
    }

    class EnemyCombat {
        -EnemyAttackDataSO _attackData
        +TryAttack()
        +IsInAttackRange()
    }

    class EnemyDataSO {
        <<ScriptableObject>>
        +string enemyName
        +EnemyTier tier
        +int maxHealth
        +float moveSpeed
        +float chaseSpeed
        +EnemyAttackDataSO primaryAttack
    }

    EnemyController *-- EnemyStats
    EnemyController *-- EnemyMovement
    EnemyController *-- EnemySenses
    EnemyController *-- EnemyCombat
    EnemyController --> EnemyDataSO
```

### FSM 시스템 (MovementFSM / CombatFSM)

```mermaid
classDiagram
    class IMovementState {
        <<interface>>
        +Enter(EnemyController)
        +Execute(EnemyController)
        +Exit(EnemyController)
    }

    class MovementFSM {
        -IMovementState _currentState
        +ChangeState(IMovementState)
        +Update(EnemyController)
    }

    class PatrolState { }
    class ChaseState {
        -Vector3 _lastTargetPos
        -float _moveTimer
    }
    class WaitState { }
    class ReturnState { }
    class StoppedState { }

    IMovementState <|.. PatrolState
    IMovementState <|.. ChaseState
    IMovementState <|.. WaitState
    IMovementState <|.. ReturnState
    IMovementState <|.. StoppedState
    MovementFSM --> IMovementState

    class ICombatState {
        <<interface>>
        +Enter(EnemyController)
        +Execute(EnemyController)
        +Exit(EnemyController)
    }

    class CombatFSM {
        -ICombatState _currentState
        +ChangeState(ICombatState)
        +Update(EnemyController)
    }

    class CombatInactiveState { }
    class CombatReadyState { }
    class CombatWindupState { }
    class CombatAttackingState { }
    class CombatRecoveryState { }

    ICombatState <|.. CombatInactiveState
    ICombatState <|.. CombatReadyState
    ICombatState <|.. CombatWindupState
    ICombatState <|.. CombatAttackingState
    ICombatState <|.. CombatRecoveryState
    CombatFSM --> ICombatState
```

### 보스 시스템 (BossController)

```mermaid
classDiagram
    class BossController {
        -BossDataSO _bossData
        -BossPhaseManager _phaseManager
        -IBossAttack _currentAttack
        -bool _isBossFight
        +int CurrentPhase
        +StartBossFight(Transform)
        +EndBossFight()
        +OnDamageTaken(int, int)
    }

    class BossPhaseManager {
        -int _currentPhase
        +event OnPhaseChanged
        +Initialize(BossController, BossDataSO)
        +CheckPhaseTransition(float)
        +GetCurrentAttackPrefab()
    }

    class BossDataSO {
        <<ScriptableObject>>
        +string bossName
        +int maxHealth
        +float phase2Threshold
        +float phase3Threshold
        +GameObject phase1AttackPrefab
        +GameObject phase2AttackPrefab
        +GameObject phase3AttackPrefab
    }

    class IBossAttack {
        <<interface>>
        +string AttackName
        +float Cooldown
        +bool IsExecuting
        +Execute(BossController, Transform)
        +Cancel()
    }

    class AirborneAttack {
        -float _range
        -float _launchForce
        -int _damage
    }

    class GroundSpikeAttack {
        -int _spikeCount
        -float _maxDistance
        -float _spreadAngle
    }

    class RotatingLaserAttack {
        -int _laserCount
        -float _laserLength
        -float _rotationSpeed
    }

    EnemyController <|-- BossController
    BossController *-- BossPhaseManager
    BossController --> BossDataSO
    BossController --> IBossAttack

    IBossAttack <|.. AirborneAttack
    IBossAttack <|.. GroundSpikeAttack
    IBossAttack <|.. RotatingLaserAttack
```

---

## 🔄 플로우차트

### 일반 적 AI 플로우

```mermaid
flowchart TD
    Start([게임 시작]) --> Init[EnemyController 초기화]
    Init --> CheckTier{Tier 확인}

    CheckTier -->|Normal| ChaseStart[ChaseState 시작]
    CheckTier -->|Epic/Boss| PatrolStart[PatrolState 시작]

    PatrolStart --> PatrolLoop[순찰 중...]
    PatrolLoop --> Detect{플레이어 감지?}
    Detect -->|No| PatrolLoop
    Detect -->|Yes| SetTarget[타겟 설정]
    SetTarget --> ChaseStart

    ChaseStart --> ChaseLoop[추적 중...]
    ChaseLoop --> UpdatePath[경로 갱신]
    UpdatePath --> InRange{공격 사거리?}

    InRange -->|No| ChaseLoop
    InRange -->|Yes| CombatReady[CombatReadyState]

    CombatReady --> Windup[선딜레이]
    Windup --> Attack[공격 실행]
    Attack --> Recovery[후딜레이]
    Recovery --> StillInRange{타겟 사거리 내?}

    StillInRange -->|Yes| CombatReady
    StillInRange -->|No| ChaseLoop
```

### 보스 페이즈 전환 플로우

```mermaid
flowchart TD
    Start([보스전 시작]) --> Phase1[Phase 1: 에어본 공격]

    Phase1 --> Dmg1{HP ≤ 66%?}
    Dmg1 -->|No| Phase1
    Dmg1 -->|Yes| Trans1[페이즈 전환 연출]
    Trans1 --> Phase2[Phase 2: 바닥 스파이크]

    Phase2 --> Dmg2{HP ≤ 33%?}
    Dmg2 -->|No| Phase2
    Dmg2 -->|Yes| Trans2[페이즈 전환 연출]
    Trans2 --> Phase3[Phase 3: 회전 레이저]

    Phase3 --> Dmg3{HP ≤ 0?}
    Dmg3 -->|No| Phase3
    Dmg3 -->|Yes| Death[보스 사망]
    Death --> End([보스전 종료])
```

### 보스 공격 패턴 플로우

```mermaid
flowchart LR
    subgraph Phase1["1페이즈: 에어본"]
        A1[경고 표시] --> A2[범위 내 플레이어 탐색]
        A2 --> A3[AddForce로 위로 발사]
    end

    subgraph Phase2["2페이즈: 스파이크"]
        B1[방향 계산] --> B2[3갈래로 분기]
        B2 --> B3[스파이크 순차 생성]
        B3 --> B4[지면 따라 전진]
    end

    subgraph Phase3["3페이즈: 레이저"]
        C1[4개 레이저 생성] --> C2[360° 회전]
        C2 --> C3[SphereCast 데미지]
    end
```

### NavMesh 경로 갱신 플로우

```mermaid
flowchart TD
    Start([ChaseState.Execute]) --> Timer{moveTimer ≥ 0?}
    Timer -->|No| Face[FaceTarget 호출]
    Timer -->|Yes| CheckDist{타겟 0.5m 이동?}

    CheckDist -->|No| Wait[대기]
    CheckDist -->|Yes| CalcPath[CalculatePath 호출]

    CalcPath --> PathStatus{경로 상태?}
    PathStatus -->|Complete| SetDest[SetDestination]
    PathStatus -->|Partial| Warn[경고 로그]
    Warn --> SetDest
    PathStatus -->|Invalid| Retry[원본 목적지로 재시도]

    SetDest --> ResetTimer[타이머 리셋]
    ResetTimer --> Face
    Face --> End([다음 프레임])
```
