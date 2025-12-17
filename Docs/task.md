# 적 시스템 개선 + 메가봉크 보스 구현

## ✅ Phase 0: 구조 리팩토링 (병렬 FSM) - 완료!

- [x] FSM 인프라: `IMovementState`, `ICombatState`, `MovementFSM`, `CombatFSM`
- [x] `EnemyController` 병렬 FSM 구조로 완전 재작성
- [x] 이동 상태: `PatrolState`, `ChaseState`, `StoppedState`, `ReturnState`, `WaitState`
- [x] 전투 상태: `CombatInactiveState`, `CombatReadyState`, `CombatWindupState`, `CombatAttackingState`, `CombatRecoveryState`
- [x] Epic/Normal 몬스터 동작 분리
- [x] Zone 스폰 시스템 개선
- [x] EnemySenses, EnemyMovement 개선

---

## ✅ Phase 1: 공용 공격 시스템 - 완료!

- [x] `EnemyAttackDataSO` 생성 (공격 설정 ScriptableObject)
- [x] `EnemyCombat` 리팩토링 (데이터 기반)
- [x] `CombatFSM States` 리팩토링 (EnemyCombat 연동)

---

## ✅ Phase 2: 데이터 분리 - 완료!

- [x] `EnemyDataSO.cs` 생성 (ScriptableObject)
- [x] `EnemyTier` enum 생성 (Normal, Epic, Boss)
- [x] EnemyController/Stats/Movement/Senses/Combat 데이터 연동

---

## ✅ Phase 3: 기존 시스템 마이그레이션 - 완료!

- [x] 하드코딩 값 제거/숨김 처리 (SO 참조 유도)
- [x] EnemySpawner 디버그 로그 정리
- [x] NavMesh 최적화 (`pathfindingIterationsPerFrame` 설정 추가)
- [x] ChaseState 추적 최적화 (Time Slicing, 동적 갱신)
- [x] Normal Enemy 무한 추적 구현

---

## [/] Phase 4: 메가봉크 보스 - 진행 중

### ✅ 4.1 기반 구조 (완료)

- [x] `BossController.cs` (EnemyController 상속)
- [x] `BossPhaseManager.cs` (HP% 기반 페이즈 전환)
- [x] `BossDataSO.cs` (보스 전용 데이터)
- [x] `IBossAttack.cs` (공격 인터페이스)

### ✅ 4.2 공격 패턴 (완료)

- [x] `AirborneAttack.cs` - 1페이즈: 에어본 (플레이어 띄우기)
- [x] `GroundSpikeAttack.cs` - 2페이즈: 3갈래 바닥 스파이크
- [x] `RotatingLaserAttack.cs` - 3페이즈: 4갈래 회전 레이저

### ✅ 4.3 UI (완료)

- [x] `BossHealthBar.cs` (화면 상단 체력바 + 페이즈 표시)

### 📋 4.4 남은 작업 (Unity 에디터)

- [ ] 보스 프리팹 생성
- [ ] BossDataSO 에셋 생성
- [ ] 공격 프리팹 생성 (각 공격에 스크립트 연결)
- [ ] 이펙트/파티클 에셋 연결
- [ ] 플레이어 데미지 연동 (PlayerHealth)
- [ ] 보스 Zone 트리거 구현
- [ ] 테스트 및 밸런싱

---

## ✅ Phase 5: 폴더 구조 통합 리팩토링 - 완료!

### 5.1 폴더 구조 변경

- [x] `02_Monster/` → `02_Enemy/Normal/` 이동
- [x] `03_Boss/` → `02_Enemy/Boss/` 이동
- [x] `02_Enemy/StateMachine/` 폴더 생성 (FSM 인프라 이동)
- [x] `02_Enemy/Data/` 폴더 생성 (Normal/Boss/Common 하위 분류)

### 최종 폴더 구조

```
02_Enemy/
├── Boss/          ← 보스 (BossController, Attacks, UI)
├── Data/          ← 모든 DataSO (Normal/Boss/Common)
├── Normal/        ← 일반 적 (EnemyController, States, etc.)
└── StateMachine/  ← FSM 인프라 (IMovementState, CombatFSM, etc.)
```

---

## 📊 진행 상태

| Phase   | 상태       | 완료율 |
| ------- | ---------- | ------ |
| Phase 0 | ✅ 완료    | 100%   |
| Phase 1 | ✅ 완료    | 100%   |
| Phase 2 | ✅ 완료    | 100%   |
| Phase 3 | ✅ 완료    | 100%   |
| Phase 4 | [/] 진행중 | 70%    |
| Phase 5 | ✅ 완료    | 100%   |
