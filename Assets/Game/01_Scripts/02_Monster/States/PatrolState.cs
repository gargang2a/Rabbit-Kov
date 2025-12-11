using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 정찰 상태
    // 적이 랜덤 위치나 웨이포인트를 순회하며 이동하는 상태
    // 도착하면 잠시 대기 후 다음 위치로 이동
    // 플레이어 발견 시 추격으로 전환
    
    public class PatrolState : IEnemyState
    {
        // _waitTimer: 도착 후 대기한 시간
        private float _waitTimer = 0f;
        
        // _waitDuration: 도착 후 대기할 시간 (초)
        // 도착하자마자 바로 이동하면 부자연스러워서 잠시 대기
        private float _waitDuration = 1f;
        
        // _isWaiting: 현재 대기 중인지 여부
        // false면 이동 중, true면 도착해서 대기 중
        private bool _isWaiting = false;

        public void Enter(EnemyController enemy)
        {
            if (enemy.Movement == null) return;

            // UseRandomPatrol: true면 랜덤 위치로 이동, false면 웨이포인트 순회
            if (enemy.Movement.UseRandomPatrol)
            {
                // 랜덤 위치로 이동
                enemy.Movement.MoveToRandomPoint();
            }
            else
            {
                // 다음 웨이포인트로 이동
                enemy.Movement.MoveToNextPatrolPoint();
            }

            Debug.Log(enemy.gameObject.name + ": PatrolState 진입");
        }

        public void Execute(EnemyController enemy)
        {
            // 플레이어 발견 체크
            if (enemy.Senses != null && enemy.Senses.ScanForTarget())
            {
                enemy.ChangeState(new ChaseState());
                return;
            }

            if (enemy.Movement == null) return;

            // 대기 중인 경우
            if (_isWaiting)
            {
                _waitTimer = _waitTimer + Time.deltaTime;
                
                // 대기 시간이 지나면 다음 위치로 이동
                if (_waitTimer >= _waitDuration)
                {
                    // 변수 초기화
                    _isWaiting = false;
                    _waitTimer = 0f;

                    // 다음 정찰 지점으로 이동
                    if (enemy.Movement.UseRandomPatrol)
                    {
                        enemy.Movement.MoveToRandomPoint();
                    }
                    else
                    {
                        // 웨이포인트 인덱스를 다음으로 이동
                        enemy.Movement.AdvancePatrolIndex();
                        enemy.Movement.MoveToNextPatrolPoint();
                    }
                }
                return; // 대기 중이면 아래 코드 실행 안 함
            }

            // 이동 중인 경우
            // HasReachedDestination: 목적지에 도착했는지 확인
            if (enemy.Movement.HasReachedDestination)
            {
                // 도착! 대기 시작
                _isWaiting = true;
            }
        }

        public void Exit(EnemyController enemy)
        {
            Debug.Log(enemy.gameObject.name + ": PatrolState 종료");
        }
    }
}
