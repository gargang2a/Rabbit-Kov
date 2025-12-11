using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 조사 상태 - 마지막으로 플레이어 본 위치로 가서 둘러봄
    public class InvestigateState : IEnemyState
    {
        private Vector3 _investigatePosition;      // 조사할 위치
        private bool _hasCustomPosition = false;   // 외부에서 위치 받았는지
        private bool _isLookingAround = false;     // 도착해서 둘러보는 중?
        private float _lookAroundTimer = 0f;       // 둘러본 시간
        private float _lookAroundDuration = 2.5f;  // 둘러볼 총 시간(초)

        // 기본 생성자
        public InvestigateState()
        {
            _hasCustomPosition = false;
        }

        // 위치 받는 생성자 (ChaseState에서 마지막 위치 전달용)
        public InvestigateState(Vector3 lastKnownPosition)
        {
            _investigatePosition = lastKnownPosition;
            _hasCustomPosition = true;
        }

        public void Enter(EnemyController enemy)
        {
            // 위치 안 받았으면 타겟 위치나 현재 위치 사용
            if (_hasCustomPosition == false)
            {
                if (enemy.CurrentTarget != null)
                {
                    _investigatePosition = enemy.CurrentTarget.position;
                }
                else
                {
                    _investigatePosition = enemy.transform.position;
                }
            }

            _isLookingAround = false;
            _lookAroundTimer = 0f;

            // 조사 위치로 이동
            if (enemy.Movement != null)
            {
                enemy.Movement.SetRunSpeed();
                enemy.Movement.MoveTo(_investigatePosition);
            }

            Debug.Log(enemy.gameObject.name + ": InvestigateState 진입");
        }

        public void Execute(EnemyController enemy)
        {
            if (enemy.Movement == null) return;

            // 이동 중
            if (_isLookingAround == false)
            {
                // 이동 중 플레이어 발견하면 추격
                if (enemy.Senses != null && enemy.Senses.ScanForTarget())
                {
                    enemy.ChangeState(new ChaseState());
                    return;
                }

                // 도착하면 둘러보기 시작
                if (enemy.Movement.HasReachedDestination)
                {
                    enemy.Movement.Stop();
                    _isLookingAround = true;
                    _lookAroundTimer = 0f;
                }
                return;
            }

            // 둘러보는 중
            _lookAroundTimer = _lookAroundTimer + Time.deltaTime;

            // 플레이어 발견하면 추격
            if (enemy.Senses != null && enemy.Senses.ScanForTarget())
            {
                enemy.ChangeState(new ChaseState());
                return;
            }

            // 시간 지나면 정찰로 복귀
            if (_lookAroundTimer >= _lookAroundDuration)
            {
                enemy.ClearTarget();
                enemy.ChangeState(new PatrolState());
                return;
            }
        }

        public void Exit(EnemyController enemy)
        {
            _isLookingAround = false;
            _lookAroundTimer = 0f;
            Debug.Log(enemy.gameObject.name + ": InvestigateState 종료");
        }
    }
}
