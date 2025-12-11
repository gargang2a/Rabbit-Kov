using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 조사 상태
    // 마지막으로 플레이어를 본 위치로 이동해서 주변을 둘러보는 상태
    // 플레이어를 다시 발견하면 추격, 일정 시간 못 찾으면 정찰로 복귀
    
    public class InvestigateState : IEnemyState
    {
        // _investigatePosition: 조사할 위치 (마지막으로 본 곳)
        private Vector3 _investigatePosition;
        
        // _hasCustomPosition: 외부에서 위치를 받았는지 여부
        // 생성자에서 위치를 받으면 true
        // 왜 필요? 기본 생성자로 만들 수도 있고, 위치를 전달해서 만들 수도 있어서
        private bool _hasCustomPosition = false;
        
        // _isLookingAround: 도착해서 주변을 둘러보는 중인지
        // false면 이동 중, true면 도착해서 둘러보는 중
        private bool _isLookingAround = false;
        
        // _lookAroundTimer: 둘러본 시간
        private float _lookAroundTimer = 0f;
        
        // _lookAroundDuration: 둘러볼 총 시간 (초)
        // 이 시간 동안 플레이어를 못 찾으면 정찰로 복귀
        private float _lookAroundDuration = 2.5f;

        // 기본 생성자: 위치를 전달받지 않을 때 사용
        // new InvestigateState() 형태로 호출
        public InvestigateState()
        {
            _hasCustomPosition = false;
        }

        // 위치를 받는 생성자: ChaseState에서 마지막 위치를 전달할 때 사용
        // new InvestigateState(lastPosition) 형태로 호출
        // 왜 생성자에서 받는가? Enter에서 받으면 너무 늦어서 ChangeState 시점에 알아야 함
        public InvestigateState(Vector3 lastKnownPosition)
        {
            _investigatePosition = lastKnownPosition;
            _hasCustomPosition = true;
        }

        public void Enter(EnemyController enemy)
        {
            // 위치가 전달되지 않았으면 타겟 위치나 현재 위치 사용
            if (_hasCustomPosition == false)
            {
                if (enemy.CurrentTarget != null)
                {
                    _investigatePosition = enemy.CurrentTarget.position;
                }
                else
                {
                    // 타겟도 없으면 현재 위치 (제자리 조사)
                    _investigatePosition = enemy.transform.position;
                }
            }

            // 변수 초기화
            _isLookingAround = false;
            _lookAroundTimer = 0f;

            // 뛰기 속도로 마지막 위치로 이동
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

            // 이동 중인 경우
            if (_isLookingAround == false)
            {
                // 이동 중에도 플레이어 감지 체크
                if (enemy.Senses != null && enemy.Senses.ScanForTarget())
                {
                    enemy.ChangeState(new ChaseState());
                    return;
                }

                // 도착 확인
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

            // 플레이어 발견 체크
            if (enemy.Senses != null && enemy.Senses.ScanForTarget())
            {
                enemy.ChangeState(new ChaseState());
                return;
            }

            // 시간이 지나면 정찰로 복귀
            if (_lookAroundTimer >= _lookAroundDuration)
            {
                // 타겟 해제 (추적 종료)
                enemy.ClearTarget();
                // 정찰 상태로 전환
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
