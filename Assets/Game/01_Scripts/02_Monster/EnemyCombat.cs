using UnityEngine;

// 적 전투 시스템 - 공격 사거리 및 쿨다운 관리
public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private float _attackRange = 1.5f;   // 공격 사거리 (m)
    [SerializeField] private float _attackCooldown = 1f;  // 공격 쿨다운 (초)

    private EnemyController _controller;  // 컨트롤러 참조
    private float _lastAttackTime;        // 마지막 공격 시간

    // 프로퍼티, 외부에서 읽기 전용
    public float AttackRange => _attackRange;

    // 컴포넌트 캐싱
    private void Awake()
    {
        _controller = GetComponent<EnemyController>();
    }

    // 공격 시도, 조건 충족 시 공격 실행
    public void TryAttack()
    {
        if (_controller == null || _controller.CurrentTarget == null) return; // 타겟 없으면 종료

        Vector3 targetPosition = _controller.CurrentTarget.position; // 타겟 위치
        float distance = Vector3.Distance(transform.position, targetPosition); // 타겟과의 거리

        if (distance > _attackRange) return; // 사거리 밖이면 종료

        float currentTime = Time.time;
        if (currentTime < _lastAttackTime + _attackCooldown) return; // 쿨다운 중이면 종료

        // TODO: 실제 데미지 처리 구현
        Debug.Log(gameObject.name + " attacks " + _controller.CurrentTarget.name);
        _lastAttackTime = currentTime; // 공격 시간 기록
    }

    // 타겟 설정 (외부 호출용, 현재 미사용)
    public void SetTarget(Transform target) { }

#if UNITY_EDITOR
    // 공격 사거리 시각화 (에디터 전용)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red; // 공격 범위 색상
        Gizmos.DrawWireSphere(transform.position, _attackRange); // 공격 사거리 원
    }
#endif
}
