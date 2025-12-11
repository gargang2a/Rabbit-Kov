using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 대기 상태
    // 적이 가만히 서서 주변을 살피다가 일정 시간 후 정찰을 시작하는 상태
    // 플레이어를 발견하면 즉시 추격 상태로 전환됨
    
    public class IdleState : IEnemyState
    {
        // _idleTimer: 대기한 시간을 측정하는 변수
        // 왜 필요? 일정 시간이 지나면 정찰을 시작해야 해서
        private float _idleTimer = 0f;
        
        // _idleDuration: 대기할 총 시간 (초)
        // 이 시간이 지나면 PatrolState로 전환됨
        private float _idleDuration = 2f;

        // Enter: 이 상태에 들어올 때 1번 호출됨
        // 초기화 작업을 여기서 함
        public void Enter(EnemyController enemy)
        {
            // 이동 멈춤
            if (enemy.Movement != null)
            {
                enemy.Movement.Stop();
            }
            
            // 타이머 초기화
            _idleTimer = 0f;
            
            Debug.Log(enemy.gameObject.name + ": IdleState 진입");
        }

        // Execute: 이 상태에 있는 동안 매 프레임 호출됨
        // 상태의 메인 로직이 여기에 들어감
        public void Execute(EnemyController enemy)
        {
            // 플레이어 감지 체크
            // &&: 앞 조건이 true일 때만 뒤 조건을 확인 (단락 평가)
            // 만약 앞이 false면 뒤는 확인 안 하고 바로 false
            if (enemy.Senses != null && enemy.Senses.ScanForTarget())
            {
                // 플레이어 발견! 추격 상태로 전환
                // new ChaseState(): 새 ChaseState 인스턴스 생성
                enemy.ChangeState(new ChaseState());
                return; // 상태가 바뀌었으니 더 이상 실행할 필요 없음
            }

            // Time.deltaTime: 이전 프레임부터 지금까지 경과한 시간 (초)
            // 60fps면 약 0.016초, 30fps면 약 0.033초
            // 이걸 더해야 프레임 속도와 관계없이 같은 속도로 시간이 흐름
            _idleTimer = _idleTimer + Time.deltaTime;

            // 대기 시간이 지났는지 확인
            if (_idleTimer >= _idleDuration)
            {
                // 정찰 가능한지 확인 (랜덤 정찰이거나 웨이포인트가 있는지)
                if (enemy.Movement != null && enemy.Movement.CanPatrol())
                {
                    enemy.ChangeState(new PatrolState());
                    return;
                }
            }
        }

        // Exit: 이 상태에서 나갈 때 1번 호출됨
        // 정리 작업을 여기서 함 (예: 애니메이션 정지, 변수 리셋)
        public void Exit(EnemyController enemy)
        {
            Debug.Log(enemy.gameObject.name + ": IdleState 종료");
        }
    }
}
