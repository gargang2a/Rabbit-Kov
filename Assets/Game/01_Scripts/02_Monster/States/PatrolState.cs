using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    public class PatrolState : IEnemyState
    {
        private float _waitTimer = 0f;
        private float _waitDurationMin = 1f;
        private float _waitDurationMax = 3f;
        private float _currentWaitDuration;
        private bool _isWaiting = false;
        
        public void Enter(EnemyController enemy)
        {
            if (enemy.Movement != null)
            {
                enemy.Movement.SetWalkSpeed();
            }
            
            bool success = StartPatrolMovement(enemy);
            
            if (success == false)
            {
                enemy.ChangeState(new IdleState());
                return;
            }
            
            _waitTimer = 0f;
            _isWaiting = false;
            _currentWaitDuration = Random.Range(_waitDurationMin, _waitDurationMax);
            
            Debug.Log(enemy.gameObject.name + ": PatrolState 진입");
        }
        
        public void Execute(EnemyController enemy)
        {
            if (enemy.Movement == null) return;
            
            if (enemy.DetectPlayer())
            {
                enemy.ChangeState(new InvestigateState());
                return;
            }

            if (_isWaiting)
            {
                _waitTimer += Time.deltaTime;
                
                if (_waitTimer >= _currentWaitDuration)
                {
                    _isWaiting = false;
                    _waitTimer = 0f;
                    
                    if (enemy.Movement.UseRandomPatrol)
                    {
                        enemy.Movement.MoveToRandomPoint();
                    }
                    else
                    {
                        enemy.Movement.AdvancePatrolIndex();
                        enemy.Movement.MoveToNextPatrolPoint();
                    }
                    
                    _currentWaitDuration = Random.Range(_waitDurationMin, _waitDurationMax);
                }
                
                return;
            }
            
            if (enemy.Movement.HasReachedDestination)
            {
                enemy.Movement.Stop();
                _isWaiting = true;
                _waitTimer = 0f;
            }
        }
        
        public void Exit(EnemyController enemy)
        {
            Debug.Log(enemy.gameObject.name + ": PatrolState 종료");
        }
        
        private bool StartPatrolMovement(EnemyController enemy)
        {
            if (enemy.Movement == null) return false;
            
            if (enemy.Movement.UseRandomPatrol)
            {
                return enemy.Movement.MoveToRandomPoint();
            }
            else
            {
                return enemy.Movement.MoveToNextPatrolPoint();
            }
        }
    }
}
