# Rabbit-Kov 핵심 시스템 3줄 요약

## 1. NavMesh 플레이어 추적

- **감지**: `Physics.OverlapSphere()`로 주변 플레이어 탐색 → 시야각/장애물 체크
- **추적**: 거리에 따라 0.1~0.6초 간격으로 경로 갱신 (동적 스로틀링)
- **이동**: `NavMeshAgent.SetDestination(target)` 호출 → NavMesh가 자동 경로탐색

## 2. ScriptableObject 아이템 관리

- **구조**: `ItemData` → `WeaponData` → `RangedWeaponData` (상속으로 확장)
- **장점**: 코드와 데이터 분리, Inspector에서 수정 가능, 여러 프리팹이 1개 SO 공유
- **사용**: `[SerializeField] RangedWeaponData _data;` → `_data.damage`로 값 참조

## 3. Player-Enemy 상호작용

- **Interface**: `IDamageable` 약속 → Player와 Enemy 모두 `TakeDamage()` 구현
- **공격**: `target.TryGetComponent(out IDamageable d)` → `d.TakeDamage(damage)`
- **장점**: 타입 상관없이 하나의 코드로 데미지 처리 가능
