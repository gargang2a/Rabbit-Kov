using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 적의 전투 시스템
    // 공격 범위와 쿨타임을 관리하고 공격을 실행함
    
    public class EnemyCombat : MonoBehaviour
    {
        [Header("공격 설정")]
        // _attackRange: 공격 가능한 거리 (단위: 미터)
        // 타겟이 이 거리 안에 들어와야 공격할 수 있음
        [SerializeField] private float _attackRange = 2f;
        
        // _attackCooldown: 공격 쿨타임 (단위: 초)
        // 공격 후 다음 공격까지 기다려야 하는 시간
        // 너무 짧으면 공격이 너무 세고, 너무 길면 약함
        [SerializeField] private float _attackCooldown = 1.5f;

        private EnemyController _controller;
        
        // _lastAttackTime: 마지막으로 공격한 시간
        // Time.time 값을 저장해서 쿨타임 계산에 사용
        // Time.time: 게임 시작 후 경과한 시간 (초)
        private float _lastAttackTime;

        // 공격 범위를 외부에서 읽을 수 있게 함
        // ChaseState에서 공격 범위에 들어왔는지 확인할 때 사용
        public float AttackRange { get { return _attackRange; } }

        private void Awake()
        {
            _controller = GetComponent<EnemyController>();
        }

        // TryAttack: 공격 시도
        // 범위 안에 있고 쿨타임이 끝났으면 공격함
        // AttackState에서 매 프레임 호출됨
        public void TryAttack()
        {
            if (_controller == null) return;
            if (_controller.CurrentTarget == null) return;

            // 거리 계산
            Vector3 myPosition = transform.position;
            Vector3 targetPosition = _controller.CurrentTarget.position;
            float distance = Vector3.Distance(myPosition, targetPosition);

            // 공격 범위 밖이면 공격 안 함
            if (distance > _attackRange) return;

            // 쿨타임 체크
            // Time.time: 게임 시작 후 경과 시간 (초)
            // 예: 게임 시작 10초 후면 Time.time = 10
            float currentTime = Time.time;
            
            // 다음 공격 가능 시간 = 마지막 공격 시간 + 쿨타임
            // 예: 8초에 공격하고 쿨타임이 1.5초면, 9.5초부터 공격 가능
            float nextAttackTime = _lastAttackTime + _attackCooldown;

            // 아직 쿨타임이 안 끝났으면 공격 안 함
            if (currentTime < nextAttackTime) return;

            // 공격 실행
            // 여기에 실제 데미지 로직 추가 가능:
            // - target.GetComponent<PlayerHealth>().TakeDamage(10);
            // - 애니메이션 재생
            // - 효과음 재생
            Debug.Log(gameObject.name + " attacks " + _controller.CurrentTarget.name);
            
            // 쿨타임 리셋: 현재 시간을 마지막 공격 시간으로 저장
            _lastAttackTime = currentTime;
        }

        // 나중에 구현할 수 있는 함수 (현재는 비어있음)
        public void SetTarget(Transform target)
        {
        }
    }
}