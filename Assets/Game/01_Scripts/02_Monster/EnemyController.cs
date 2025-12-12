using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 적 AI 중앙 컨트롤러 - 모든 컴포넌트 연결, FSM 관리
    [RequireComponent(typeof(EnemyStats))]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemySenses))]
    [RequireComponent(typeof(EnemyCombat))]
    [RequireComponent(typeof(EnemyInventory))]
    public class EnemyController : MonoBehaviour
    {
        private EnemyStats _stats;          // 체력
        private EnemyMovement _movement;    // 이동
        private EnemySenses _senses;        // 감지
        private EnemyCombat _combat;        // 전투
        private EnemyInventory _inventory;  // 인벤토리
        private EnemyStateMachine _stateMachine;  // FSM
        private Transform _currentTarget;  // 현재 타겟 (보통 플레이어)

        // 외부 접근용 프로퍼티 (읽기 전용)
        public EnemyStats Stats { get { return _stats; } }
        public EnemyMovement Movement { get { return _movement; } }
        public EnemySenses Senses { get { return _senses; } }
        public EnemyCombat Combat { get { return _combat; } }
        public EnemyInventory Inventory { get { return _inventory; } }
        public Transform CurrentTarget { get { return _currentTarget; } }

        public string CurrentStateName
        {
            get
            {
                if (_stateMachine != null) return _stateMachine.CurrentStateName;
                return "Not Initialized";
            }
        }

        private void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            // 컴포넌트 캐싱
            _stats = GetComponent<EnemyStats>();
            _movement = GetComponent<EnemyMovement>();
            _senses = GetComponent<EnemySenses>();
            _combat = GetComponent<EnemyCombat>();
            _inventory = GetComponent<EnemyInventory>();

            _stateMachine = new EnemyStateMachine();

            // 사망 이벤트 구독
            if (_stats != null)
            {
                _stats.OnDeath += HandleDeath;
            }

            // 시작 상태: Idle
            _stateMachine.ChangeState(new IdleState(), this);
        }

        private void Update()
        {
            if (_stats != null && _stats.isDead == true) return;

            if (_stateMachine != null)
            {
                _stateMachine.Update(this);
            }
        }

        private void OnDestroy()
        {
            // 이벤트 해제 (메모리 누수 방지)
            if (_stats != null)
            {
                _stats.OnDeath -= HandleDeath;
            }
        }

        public void ChangeState(IEnemyState newState)
        {
            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(newState, this);
            }
        }

        public void SetTarget(Transform target) { _currentTarget = target; }
        public void ClearTarget() { _currentTarget = null; }
        public bool HasTarget() { return _currentTarget != null; }

        protected virtual void HandleDeath()
        {
            if (_movement != null)
            {
                _movement.Stop();
            }
            Debug.Log(gameObject.name + " 사망!");
        }
    }
}
