using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 적 AI의 중앙 컨트롤러
    // 모든 컴포넌트를 연결하고 상태 기계를 관리함
    // 다른 스크립트는 이 컨트롤러를 통해 적의 기능에 접근함
    
    // RequireComponent: 이 스크립트에 필요한 컴포넌트들을 명시
    // 이 스크립트를 오브젝트에 붙이면 나열된 컴포넌트들이 자동으로 추가됨
    [RequireComponent(typeof(EnemyStats))]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemySenses))]
    [RequireComponent(typeof(EnemyCombat))]
    [RequireComponent(typeof(EnemyInventory))]

    public class EnemyController : MonoBehaviour
    {
        // 각 컴포넌트에 대한 참조를 저장하는 변수들
        // 왜 저장해두는가?
        // - GetComponent는 느린 함수라서 매번 호출하면 성능이 떨어짐
        // - Awake에서 한 번만 호출하고 저장해두면 빠름 (캐싱)
        
        private EnemyStats _stats;          // 체력 관리
        private EnemyMovement _movement;    // 이동 관리
        private EnemySenses _senses;        // 감지 관리
        private EnemyCombat _combat;        // 전투 관리
        private EnemyInventory _inventory;  // 인벤토리 관리
        
        // _stateMachine: 상태 기계 인스턴스
        // MonoBehaviour가 아니라 일반 클래스라서 new로 생성
        private EnemyStateMachine _stateMachine;
        
        // _currentTarget: 현재 추적 중인 타겟 (보통 플레이어)
        // Transform: 위치, 회전, 크기 정보를 가진 컴포넌트
        private Transform _currentTarget;

        // 프로퍼티: 컴포넌트들을 외부에서 접근할 수 있게 함
        // 왜 프로퍼티? 변수를 직접 public으로 하면 외부에서 바꿀 수 있어서 위험
        // 프로퍼티로 하면 읽기만 가능하게 할 수 있음 (캡슐화)
        public EnemyStats Stats { get { return _stats; } }
        public EnemyMovement Movement { get { return _movement; } }
        public EnemySenses Senses { get { return _senses; } }
        public EnemyCombat Combat { get { return _combat; } }
        public EnemyInventory Inventory { get { return _inventory; } }
        public Transform CurrentTarget { get { return _currentTarget; } }

        // 현재 상태 이름 (디버깅용)
        public string CurrentStateName
        {
            get
            {
                if (_stateMachine != null) return _stateMachine.CurrentStateName;
                return "Not Initialized";
            }
        }

        // Awake: 오브젝트가 생성될 때 호출
        // Start보다 먼저 호출되므로 초기화에 적합
        private void Awake()
        {
            Initialize();
        }

        // Initialize: 초기화 함수
        // protected: 이 클래스와 자식 클래스에서만 접근 가능
        // virtual: 자식 클래스에서 재정의(override) 가능
        // 왜 Awake에서 직접 안 하고 함수로 분리? 자식 클래스에서 초기화를 수정하기 쉽게
        protected virtual void Initialize()
        {
            // GetComponent<타입>(): 이 오브젝트에 붙어있는 해당 타입의 컴포넌트를 가져옴
            _stats = GetComponent<EnemyStats>();
            _movement = GetComponent<EnemyMovement>();
            _senses = GetComponent<EnemySenses>();
            _combat = GetComponent<EnemyCombat>();
            _inventory = GetComponent<EnemyInventory>();

            // new EnemyStateMachine(): 상태 기계 인스턴스 생성
            _stateMachine = new EnemyStateMachine();

            // 이벤트 구독: 사망 시 HandleDeath 함수가 호출됨
            // += : 이벤트에 함수를 연결
            if (_stats != null)
            {
                _stats.OnDeath += HandleDeath;
            }

            // 시작 상태를 IdleState로 설정
            // new IdleState(): IdleState 인스턴스 생성
            _stateMachine.ChangeState(new IdleState(), this);
        }

        // Update: 매 프레임 호출됨 (약 60번/초)
        private void Update()
        {
            // 죽었으면 아무것도 안 함
            // && : 둘 다 true여야 전체가 true (AND 연산)
            if (_stats != null && _stats.isDead == true) return;

            // 상태 기계 업데이트: 현재 상태의 Execute 호출
            if (_stateMachine != null)
            {
                _stateMachine.Update(this);
            }
        }

        // OnDestroy: 오브젝트가 파괴될 때 호출
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            // -= : 이벤트에서 함수 연결 해제
            // 왜 해제해야 하는가?
            // - 해제 안 하면 오브젝트가 파괴되어도 이벤트가 함수를 참조하고 있음
            // - 메모리 누수와 에러의 원인이 됨
            if (_stats != null)
            {
                _stats.OnDeath -= HandleDeath;
            }
        }

        // ChangeState: 상태 변경 함수
        // 다른 스크립트(상태 클래스)에서 상태를 바꿀 때 사용
        public void ChangeState(IEnemyState newState)
        {
            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(newState, this);
            }
        }

        // SetTarget: 타겟 설정
        // EnemySenses에서 플레이어를 감지했을 때 호출
        public void SetTarget(Transform target)
        {
            _currentTarget = target;
        }

        // ClearTarget: 타겟 해제
        // 시야를 놓쳤거나 조사가 끝났을 때 호출
        public void ClearTarget()
        {
            _currentTarget = null;
        }

        // HasTarget: 타겟이 있는지 확인
        public bool HasTarget()
        {
            if (_currentTarget != null) return true;
            return false;
        }

        // HandleDeath: 사망 처리
        // OnDeath 이벤트에 의해 호출됨
        protected virtual void HandleDeath()
        {
            // 이동 멈춤
            if (_movement != null)
            {
                _movement.Stop();
            }
            Debug.Log(gameObject.name + " 사망!");
        }
    }
}
