using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// EnemyCombat - 적 전투(공격) 시스템
// ============================================================================
// 
// [역할]
// 적의 공격을 담당하는 컴포넌트입니다.
// 공격 사거리, 쿨다운(재사용 대기시간), 실제 공격 실행을 관리합니다.
// 
// [AttackState와의 관계]
// - AttackState.Execute()에서 TryAttack()을 매 프레임 호출
// - TryAttack()은 내부적으로 쿨다운을 체크하여 공격 실행 여부 결정
// 
// [공격 흐름]
// 1. AttackState에서 TryAttack() 호출
// 2. 타겟 유효성 체크
// 3. 사거리 체크
// 4. 쿨다운 체크 (마지막 공격 이후 _attackCooldown 초 경과했는지)
// 5. 조건 충족 시 공격 실행 (현재는 로그만 출력, 추후 데미지 처리 추가)
// ============================================================================
public class EnemyCombat : MonoBehaviour
{
    // ==================== 공격 설정 ====================
    
    // 공격 사거리 (미터 단위). 타겟이 이 거리 이내에 있어야 공격 가능합니다.
    // ChaseState에서 이 값을 참조하여 공격 범위에 들어왔는지 체크합니다.
    // 인스펙터에서 조절 가능: 값이 크면 원거리 공격, 작으면 근거리 공격
    [SerializeField] private float _attackRange = 10f;
    
    // 공격 쿨다운 (초 단위). 연속 공격 사이의 최소 간격입니다.
    // 이 시간이 지나야 다음 공격이 가능합니다.
    // 예: 1.0f면 1초마다 한 번 공격 가능, 0.5f면 초당 2회 공격 가능
    [SerializeField] private float _attackCooldown = 1f;

    // ==================== 컴포넌트 참조 ====================
    
    // EnemyController 참조. CurrentTarget 정보를 가져오기 위해 필요합니다.
    // Awake()에서 GetComponent로 캐싱합니다.
    private EnemyController _controller;
    
    // 마지막으로 공격을 실행한 시간 (Time.time 기준)
    // TryAttack()에서 쿨다운 체크에 사용됩니다.
    // 현재 시간 - _lastAttackTime >= _attackCooldown 이면 공격 가능
    private float _lastAttackTime;

    // ==================== 프로퍼티 ====================
    
    // 공격 사거리를 외부에서 읽을 수 있게 하는 프로퍼티
    // ChaseState, AttackState에서 사거리 체크에 사용합니다.
    public float AttackRange
    {
        get { return _attackRange; }
    }

    // ==================== MonoBehaviour 생명주기 ====================
    
    // Awake: 컴포넌트 초기화. GetComponent는 비용이 크므로 여기서 한 번만 호출
    private void Awake()
    {
        _controller = GetComponent<EnemyController>();
    }

    // ==================== 공개 메서드 ====================
    
    // 공격을 시도하는 함수. AttackState에서 매 프레임 호출됩니다.
    // 모든 조건(타겟, 사거리, 쿨다운)이 충족되면 공격을 실행합니다.
    // 조건이 충족되지 않으면 아무것도 하지 않고 반환합니다.
    public void TryAttack()
    {
        // ===== 1. 컨트롤러 및 타겟 유효성 체크 =====
        // _controller가 없으면 타겟 정보를 알 수 없음
        if (_controller == null) return;
        // 타겟이 없으면 공격할 대상이 없음
        if (_controller.CurrentTarget == null) return;

        // ===== 2. 사거리 체크 =====
        Vector3 myPosition = transform.position;
        Vector3 targetPosition = _controller.CurrentTarget.position;
        float distance = Vector3.Distance(myPosition, targetPosition);

        // 타겟이 공격 사거리를 벗어났으면 공격하지 않음
        if (distance > _attackRange) return;

        // ===== 3. 쿨다운 체크 =====
        // Time.time: 게임 시작 후 경과한 총 시간 (초)
        float currentTime = Time.time;
        // 다음 공격이 가능한 시간 = 마지막 공격 시간 + 쿨다운
        float nextAttackTime = _lastAttackTime + _attackCooldown;

        // 아직 쿨다운 중이면 공격하지 않음
        if (currentTime < nextAttackTime) return;

        // ===== 4. 공격 실행 =====
        // 현재는 디버그 로그만 출력
        // TODO: 실제 데미지 처리 로직 구현 필요
        // 예: _controller.CurrentTarget.GetComponent<PlayerHealth>().TakeDamage(_attackDamage);
        Debug.Log(gameObject.name + " attacks " + _controller.CurrentTarget.name);

        // 공격 시간 기록: 다음 쿨다운 계산에 사용
        _lastAttackTime = currentTime;
    }

    // 타겟을 설정하는 함수 (현재 미사용, EnemyController.SetTarget 사용)
    // 추후 Combat 전용 타겟 관리가 필요할 때 구현
    public void SetTarget(Transform target)
    {
        // 현재 비어있음 - 필요시 구현
    }

// ==================== 에디터 전용: 시각적 디버깅 ====================
#if UNITY_EDITOR
    // OnDrawGizmos: Scene 뷰에서 공격 사거리 시각화
    // 빨간색 와이어 구로 공격 범위를 표시합니다.
    private void OnDrawGizmos()
    {
        // 빨간색: 공격/위험 영역을 나타내는 전통적인 색상
        Gizmos.color = Color.red;
        // 현재 위치를 중심으로 공격 사거리만큼의 원 그리기
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
#endif
}

