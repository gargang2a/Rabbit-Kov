using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    public class InvestigateState : IEnemyState
    {
        private Vector3 _investigatePosition;
        private bool _isLookingAround = false;
        private float _lookAroundTimer = 0f;
        private float _lookAroundDuration = 2.5f;

        public void Enter(EnemyController enemy)
        {
            if (enemy.CurrentTarget != null)
            {
                _investigatePosition = enemy.CurrentTarget.position;
            }
            else
            {
                _investigatePosition = enemy.transform.position;
            }

            _isLookingAround = false;
            _lookAroundTimer = 0f;

            if (enemy.Movement != null)
            {
                enemy.Movement.LookAt(_investigatePosition);
                enemy.Movement.SetRunSpeed();
                enemy.Movement.MoveTo(_investigatePosition);
            }

            Debug.Log(enemy.gameObject.name + ": InvestigateState 진입");
        }

        public void Execute(EnemyController enemy)
        {
            if (enemy.Movement == null) return;

            if (!_isLookingAround)
            {
                if (enemy.DetectPlayer())
                {
                    _investigatePosition = enemy.CurrentTarget.position;
                    enemy.Movement.LookAt(_investigatePosition);
                    enemy.Movement.MoveTo(_investigatePosition);
                }

                if (enemy.Movement.HasReachedDestination)
                {
                    enemy.Movement.Stop();
                    _isLookingAround = true;
                    _lookAroundTimer = 0f;
                    Debug.Log(enemy.gameObject.name + ": 도착, 주변 둘러보기 시작");
                }

                return;
            }

            _lookAroundTimer += Time.deltaTime;

            if (enemy.DetectPlayer())
            {
                _investigatePosition = enemy.CurrentTarget.position;
                _isLookingAround = false;
                enemy.Movement.LookAt(_investigatePosition);
                enemy.Movement.SetRunSpeed();
                enemy.Movement.MoveTo(_investigatePosition);
                Debug.Log(enemy.gameObject.name + ": 둘러보는 중 재발견! 추적 재개");
                return;
            }

            if (_lookAroundTimer >= _lookAroundDuration)
            {
                enemy.ClearTarget();
                enemy.ChangeState(new PatrolState());
                Debug.Log(enemy.gameObject.name + ": 둘러보기 완료, Player 찾지 못함. PatrolState로 전환");
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
