using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 추격 상태
    // 플레이어를 향해 달려가는 상태
    // 공격 범위에 들어오면 공격 상태로, 시야를 놓치면 조사 상태로 전환
    
    public class ChaseState : IEnemyState
    {
        // _lastKnownPosition: 마지막으로 플레이어를 본 위치
        // 왜 필요? 시야를 놓쳤을 때 InvestigateState에 전달해서 그 위치를 조사하게 함
        private Vector3 _lastKnownPosition;

        public void Enter(EnemyController enemy)
        {
            // 뛰기 속도로 설정 (추격해야 하니까 빨라야 함)
            if (enemy.Movement != null)
            {
                enemy.Movement.SetRunSpeed();
            }

            // 마지막 위치 저장
            if (enemy.CurrentTarget != null)
            {
                _lastKnownPosition = enemy.CurrentTarget.position;
            }

            Debug.Log(enemy.gameObject.name + ": ChaseState 진입");
        }

        public void Execute(EnemyController enemy)
        {
            // 타겟이 없으면 (파괴됨 등) 마지막 위치로 조사하러 감
            if (enemy.CurrentTarget == null)
            {
                // InvestigateState 생성자에 마지막 위치 전달
                enemy.ChangeState(new InvestigateState(_lastKnownPosition));
                return;
            }

            // 매 프레임 마지막 위치 업데이트
            // 왜? 계속 움직이는 플레이어의 최신 위치를 알아야 시야를 놓쳐도 조사 가능
            _lastKnownPosition = enemy.CurrentTarget.position;

            // 거리 계산
            Vector3 myPosition = enemy.transform.position;
            Vector3 targetPosition = enemy.CurrentTarget.position;
            float distance = Vector3.Distance(myPosition, targetPosition);

            // 공격 범위 안에 들어왔는지 확인
            if (enemy.Combat != null && distance <= enemy.Combat.AttackRange)
            {
                // 공격 상태로 전환
                enemy.ChangeState(new AttackState());
                return;
            }

            // 타겟을 향해 이동
            if (enemy.Movement != null)
            {
                enemy.Movement.MoveTo(targetPosition);
            }

            // 시야 체크: ScanForTarget이 false면 시야를 놓친 것
            if (enemy.Senses != null && enemy.Senses.ScanForTarget() == false)
            {
                // 마지막 위치를 조사하러 감
                enemy.ChangeState(new InvestigateState(_lastKnownPosition));
                return;
            }
        }

        public void Exit(EnemyController enemy)
        {
            Debug.Log(enemy.gameObject.name + ": ChaseState 종료");
        }
    }
}
