using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    [RequireComponent(typeof(EnemyStats))]
    [RequireComponent(typeof(EnemyMovement))]

    public class EnemyController : MonoBehaviour
    {
        private EnemyStats _stats;
        private EnemyMovement _movement;

        private EnemyStateMachine _stateMachine;

        private Transform _currentTarget;

        public EnemyStats Stats { get { return _stats; } }
        public EnemyMovement Movement { get { return _movement; } }
        public Transform CurrentTarget { get { return _currentTarget; } }

        public string CurrentStateName
        {
            get
            {
                if (_stateMachine != null)
                {
                    return _stateMachine.CurrentStateName;
                }
                else
                {
                    return "Not Initialized";
                }
            }
        }

        private void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            _stats = GetComponent<EnemyStats>();
            _movement = GetComponent<EnemyMovement>();
            _stateMachine = new EnemyStateMachine();

            if (_stats != null)
            {
                _stats.OnDeath += HandleDeath;
            }

            _stateMachine.ChangeState(new IdleState(), this);
        }

        private void Update()
        {
            if (_stats != null && _stats.isDead) return;

            if (_stateMachine != null)
            {
                _stateMachine.Update(this);
            }
        }

        private void OnDestroy()
        {
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

        public void SetTarget(Transform target)
        {
            _currentTarget = target;
        }

        public void ClearTarget()
        {
            _currentTarget = null;
        }

        public bool HasTarget()
        {
            if (_currentTarget != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual void HandleDeath()
        {
            if (_movement != null)
            {
                _movement.Stop();
            }
            Debug.Log(gameObject.name + " »ç¸Á!");
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 labelPosition = transform.position + Vector3.up * 2.5f;
            UnityEditor.Handles.Label(labelPosition, "State: " + CurrentStateName);

            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Vector3 form = transform.position + Vector3.up;
                Vector3 to = _currentTarget.position + Vector3.up;
                Gizmos.DrawLine(form, to);
            }
        }
#endif
    }
}