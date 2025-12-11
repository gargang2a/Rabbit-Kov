using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 적의 전투 시스템 - 공격 범위, 쿨타임 관리
    public class EnemyCombat : MonoBehaviour
    {
        [Header("공격 설정")]
        [SerializeField] private float _attackRange = 2f;      // 공격 가능 거리
        [SerializeField] private float _attackCooldown = 1.5f; // 공격 쿨타임 (초)

        private EnemyController _controller;
        private float _lastAttackTime;  // 마지막 공격 시간 (Time.time 저장)

        public float AttackRange { get { return _attackRange; } }

        private void Awake()
        {
            _controller = GetComponent<EnemyController>();
        }

        // 공격 시도 - 범위 안 + 쿨타임 끝나면 공격
        public void TryAttack()
        {
            if (_controller == null) return;
            if (_controller.CurrentTarget == null) return;

            // 거리 계산
            Vector3 myPosition = transform.position;
            Vector3 targetPosition = _controller.CurrentTarget.position;
            float distance = Vector3.Distance(myPosition, targetPosition);

            if (distance > _attackRange) return;  // 범위 밖이면 무시

            // 쿨타임 체크
            float currentTime = Time.time;
            float nextAttackTime = _lastAttackTime + _attackCooldown;

            if (currentTime < nextAttackTime) return;  // 아직 쿨타임 중

            // 공격! (나중에 데미지 로직, 애니메이션, 효과음 추가)
            Debug.Log(gameObject.name + " attacks " + _controller.CurrentTarget.name);
            
            _lastAttackTime = currentTime;  // 쿨타임 리셋
        }

        // 미구현
        public void SetTarget(Transform target)
        {
        }
    }
}