using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RabbitKov.Enemy
{
    // 적의 이동을 담당하는 스크립트
    // NavMeshAgent를 사용해서 길찾기와 이동을 처리함
    // NavMeshAgent는 Unity에서 AI 캐릭터가 자동으로 길을 찾아 이동하게 해주는 컴포넌트
    
    // RequireComponent: 이 스크립트가 동작하려면 NavMeshAgent가 필요하다고 Unity에 알려줌
    // 이 스크립트를 오브젝트에 붙이면 NavMeshAgent가 자동으로 추가됨
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        [Header("이동 속도")]
        // _walkSpeed: 정찰할 때 걷는 속도 (단위: m/s)
        // 왜 SerializeField? 디자이너가 Inspector에서 속도를 조절할 수 있게 하려고
        [SerializeField] private float _walkSpeed = 2f;
        
        // _runSpeed: 추격할 때 뛰는 속도 (단위: m/s)
        // 걷기보다 빨라야 플레이어를 따라잡을 수 있음
        [SerializeField] private float _runSpeed = 5f;

        [Header("정찰 설정")]
        // _patrolPoints: 정찰할 위치들을 저장하는 배열
        // Transform[]: 여러 개의 Transform을 저장할 수 있는 배열
        // Scene에 빈 오브젝트들을 만들고 여기에 연결하면 정찰 경로가 됨
        [SerializeField] private Transform[] _patrolPoints;
        
        // _randomPatrolRadius: 랜덤 정찰할 때 이동할 범위 (단위: 미터)
        // 현재 위치에서 이 범위 안의 랜덤한 위치로 이동함
        [SerializeField] private float _randomPatrolRadius = 10f;
        
        // _useRandomPatrol: true면 랜덤 정찰, false면 웨이포인트 정찰
        // 웨이포인트를 설정하기 귀찮을 때 랜덤으로 돌아다니게 할 수 있음
        [SerializeField] private bool _useRandomPatrol = true;

        [Header("회전 설정")]
        // _rotateSpeed: 초당 회전할 수 있는 최대 각도 (단위: 도/초)
        // 120이면 1초에 최대 120도 회전 가능
        [SerializeField] private float _rotateSpeed = 120f;

        // _currentPatrolIndex: 현재 몇 번째 웨이포인트로 이동 중인지 저장
        // 0부터 시작하고, 마지막 웨이포인트에 도착하면 다시 0으로 돌아감
        private int _currentPatrolIndex = 0;
        
        // _agent: NavMeshAgent 컴포넌트를 저장해두는 변수
        // 왜 저장? GetComponent를 매번 호출하면 느려서 한 번만 호출하고 저장해둠 (캐싱)
        private NavMeshAgent _agent;

        // 프로퍼티: 변수 값을 외부에서 읽을 수 있게 해주는 기능
        // get만 있고 set이 없으면 읽기 전용 (외부에서 값을 바꿀 수 없음)
        public float WalkSpeed { get { return _walkSpeed; } }
        public float RunSpeed { get { return _runSpeed; } }
        public float RotationSpeed { get { return _rotateSpeed; } }
        public bool UseRandomPatrol { get { return _useRandomPatrol; } }

        // HasReachedDestination: 목적지에 도착했는지 확인하는 프로퍼티
        // 왜 프로퍼티? 단순히 변수 값을 반환하는 게 아니라 계산이 필요해서
        public bool HasReachedDestination
        {
            get
            {
                // agent가 없으면 도착 여부를 알 수 없음
                if (_agent == null) return false;
                
                // pathPending: 아직 경로 계산 중이면 true
                // 경로 계산 중에는 도착 여부를 알 수 없음
                if (_agent.pathPending) return false;
                
                // hasPath: 현재 이동할 경로가 있으면 true
                // 경로가 없다는 건 이미 도착했거나 갈 수 없는 곳이라는 뜻
                if (_agent.hasPath == false)
                {
                    // velocity.sqrMagnitude: 속도 벡터의 길이의 제곱
                    // 왜 제곱? 제곱근 계산 안 해서 빠름 (최적화)
                    // 속도가 거의 0이면 멈춰있는 것 = 도착
                    return _agent.velocity.sqrMagnitude < 0.01f;
                }
                
                // remainingDistance: 목적지까지 남은 거리
                // stoppingDistance: 목적지에서 이 거리만큼 떨어진 곳에서 멈춤
                return _agent.remainingDistance <= _agent.stoppingDistance;
            }
        }

        // isMoving: 현재 이동 중인지 확인
        public bool isMoving
        {
            get
            {
                // 속도의 제곱이 0.01보다 크면 움직이는 중
                float speedSquared = _agent.velocity.sqrMagnitude;
                return speedSquared > 0.01f;
            }
        }

        // Awake: 오브젝트가 생성될 때 가장 먼저 1번만 호출됨
        // Start보다 먼저 호출되므로 컴포넌트 캐싱에 적합
        private void Awake()
        {
            // GetComponent<타입>(): 이 오브젝트에 붙어있는 해당 타입의 컴포넌트를 가져옴
            // 여기서 한 번만 호출하고 _agent에 저장해둠 (캐싱)
            _agent = GetComponent<NavMeshAgent>();
        }

        // MoveTo: 목적지로 이동하는 함수
        // 매개변수 destination: 이동할 위치 (Vector3 = x, y, z 좌표)
        public void MoveTo(Vector3 destination)
        {
            if (_agent == null) return;
            
            // isOnNavMesh: NavMesh 위에 있는지 확인
            // NavMesh 밖에 있으면 길찾기가 안 되므로 이동 불가
            if (_agent.isOnNavMesh == false) return;

            // isStopped = false: 멈춤 상태 해제하고 이동 시작
            _agent.isStopped = false;
            
            // SetDestination: NavMeshAgent에게 목적지를 알려줌
            // NavMeshAgent가 알아서 경로를 찾아서 이동함
            _agent.SetDestination(destination);
        }

        // MoveTo 오버로드: Transform을 받아서 position으로 변환
        // 오버로드: 같은 이름의 함수인데 매개변수 타입이 다른 것
        // 왜 오버로드? enemy.Movement.MoveTo(player) 형태로 편하게 쓰려고
        public void MoveTo(Transform target)
        {
            if (target == null) return;
            MoveTo(target.position);
        }

        // Stop: 이동을 멈추는 함수
        public void Stop()
        {
            if (_agent == null) return;
            if (_agent.isOnNavMesh == false) return;

            // isStopped = true: 이동 멈춤
            _agent.isStopped = true;
            
            // velocity = Vector3.zero: 속도를 0으로 해서 즉시 멈춤
            // isStopped만 하면 천천히 멈출 수 있음
            _agent.velocity = Vector3.zero;
        }

        // SetSpeed: 이동 속도를 설정하는 함수
        public void SetSpeed(float speed)
        {
            if (_agent != null) _agent.speed = speed;
        }

        // SetWalkSpeed: 걷기 속도로 설정 (정찰용)
        public void SetWalkSpeed()
        {
            SetSpeed(_walkSpeed);
        }

        // SetRunSpeed: 뛰기 속도로 설정 (추격용)
        public void SetRunSpeed()
        {
            SetSpeed(_runSpeed);
        }

        // LookAt: 지정한 위치를 바라보게 회전하는 함수
        // 반환값: 완전히 바라보면 true, 아직 회전 중이면 false
        public bool LookAt(Vector3 target)
        {
            // 바라볼 방향 계산
            // (목표 위치 - 내 위치) = 나에서 목표를 향하는 벡터
            // normalized: 벡터의 길이를 1로 만듦 (방향만 남김)
            Vector3 direction = (target - transform.position).normalized;
            
            // y를 0으로 해서 수평 회전만 함
            // 안 하면 위아래로 기울어질 수 있음
            direction.y = 0;

            // 방향이 zero면 같은 위치 = 이미 바라보고 있음
            if (direction == Vector3.zero) return true;

            // Quaternion: 3D에서 회전을 표현하는 방법
            // Euler 각도(x, y, z)보다 안정적이고 회전 계산에 좋음
            
            // Quaternion.LookRotation(방향):
            // - 해당 방향을 바라보는 회전값을 만들어줌
            // - 매개변수: 바라볼 방향 (Vector3)
            // - 반환: 그 방향을 바라보는 Quaternion
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            // Quaternion.RotateTowards(현재, 목표, 최대각도):
            // - 현재 회전에서 목표 회전으로 천천히 회전
            // - 매개변수1: 현재 회전 (transform.rotation)
            // - 매개변수2: 목표 회전 (targetRotation)
            // - 매개변수3: 이번 프레임에 회전할 최대 각도
            //   _rotateSpeed(초당 각도) * Time.deltaTime(프레임 시간) = 이번 프레임 회전량
            // - 반환: 조금 회전된 Quaternion
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotateSpeed * Time.deltaTime);

            // Quaternion.Angle: 두 회전 사이의 각도 차이
            // 1도 미만이면 거의 다 회전함
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            return angleDifference < 1f;
        }

        // LookAt 오버로드: Transform 버전
        public bool LookAt(Transform target)
        {
            if (target == null) return true;
            return LookAt(target.position);
        }

        // HasPatrolPoint: 웨이포인트가 설정되어 있는지 확인
        public bool HasPatrolPoint()
        {
            if (_patrolPoints == null) return false;
            if (_patrolPoints.Length > 0) return true;
            return false;
        }

        // CanPatrol: 정찰이 가능한지 확인
        // 랜덤 정찰이거나 웨이포인트가 있으면 true
        public bool CanPatrol()
        {
            if (_useRandomPatrol == true) return true;
            return HasPatrolPoint();
        }

        // MoveToNextPatrolPoint: 현재 인덱스의 웨이포인트로 이동
        public bool MoveToNextPatrolPoint()
        {
            if (_patrolPoints == null) return false;
            if (_patrolPoints.Length == 0) return false;

            // 배열[인덱스]: 배열에서 해당 번호의 요소를 가져옴
            Transform targetPoint = _patrolPoints[_currentPatrolIndex];

            if (targetPoint != null)
            {
                SetWalkSpeed();
                MoveTo(targetPoint.position);
            }

            return true;
        }

        // AdvancePatrolIndex: 다음 웨이포인트 인덱스로 이동
        // 마지막에 도달하면 처음으로 돌아감 (순환)
        public void AdvancePatrolIndex()
        {
            if (_patrolPoints == null) return;
            if (_patrolPoints.Length == 0) return;

            _currentPatrolIndex = _currentPatrolIndex + 1;

            // 배열 끝에 도달하면 처음으로
            if (_currentPatrolIndex >= _patrolPoints.Length)
            {
                _currentPatrolIndex = 0;
            }
        }

        // MoveToRandomPoint: 랜덤한 위치로 이동
        // 현재 위치에서 _randomPatrolRadius 범위 내의 랜덤 위치를 찾아서 이동
        public bool MoveToRandomPoint()
        {
            if (_agent == null) return false;
            if (_agent.isOnNavMesh == false) return false;

            // Random.insideUnitSphere: 반지름 1인 구 안의 랜덤한 점
            // _randomPatrolRadius를 곱해서 범위를 확장
            Vector3 randomDirection = Random.insideUnitSphere * _randomPatrolRadius;
            
            // 현재 위치를 기준으로 이동
            randomDirection = randomDirection + transform.position;
            
            // 높이는 현재 위치와 같게 (땅바닥 높이 유지)
            randomDirection.y = transform.position.y;

            // NavMesh.SamplePosition: 해당 위치 근처에서 NavMesh 위의 유효한 점을 찾음
            // 왜 필요? 랜덤 위치가 벽 안이나 NavMesh 밖일 수 있어서
            // 매개변수: 찾을 위치, 결과 저장 변수, 검색 범위, NavMesh 종류
            NavMeshHit hit;
            bool foundPosition = NavMesh.SamplePosition(randomDirection, out hit, 2f, NavMesh.AllAreas);

            // 유효한 위치를 못 찾으면 실패
            if (foundPosition == false) return false;

            SetWalkSpeed();
            MoveTo(hit.position);

            return true;
        }

        // #if UNITY_EDITOR: 에디터에서만 실행, 실제 게임에는 포함 안 됨
        // Gizmos는 에디터 전용 기능이라서 이렇게 감싸줌
#if UNITY_EDITOR
        // OnDrawGizmos: Scene 뷰에서 항상 보이는 시각화
        // 에디터에서 확인할 때 유용함
        private void OnDrawGizmos()
        {
            DrawRandomPatrolGizmos();
            DrawPatrolPointGizmos();
            DrawPathGizmos();
        }

        // 랜덤 정찰 범위 시각화 (마젠타색 원)
        private void DrawRandomPatrolGizmos()
        {
            if (_useRandomPatrol == false) return;
            
            // new Color(R, G, B, A): RGBA 색상 (0~1 범위)
            // A = 0.3이면 30% 불투명 (반투명)
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _randomPatrolRadius);
        }

        // 웨이포인트 경로 시각화 (시안색 점과 선)
        private void DrawPatrolPointGizmos()
        {
            if (_patrolPoints == null) return;
            if (_patrolPoints.Length < 2) return;

            Gizmos.color = Color.cyan;

            for (int i = 0; i < _patrolPoints.Length; i++)
            {
                if (_patrolPoints[i] == null) continue;

                // 각 웨이포인트에 구 그리기
                Gizmos.DrawWireSphere(_patrolPoints[i].position, 0.5f);

                // 다음 웨이포인트까지 선 그리기
                int nextIndex = i + 1;
                if (nextIndex >= _patrolPoints.Length) nextIndex = 0;

                if (_patrolPoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(_patrolPoints[i].position, _patrolPoints[nextIndex].position);
                }
            }
        }

        // 현재 이동 경로 시각화
        private void DrawPathGizmos()
        {
            if (_agent == null) return;
            if (_agent.hasPath == false) return;

            // 경로 (초록색)
            Gizmos.color = Color.green;
            
            // path.corners: NavMesh가 계산한 경로의 꺾이는 점들
            Vector3[] corners = _agent.path.corners;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }

            // 목적지 (노란색)
            if (corners.Length > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(corners[corners.Length - 1], 0.5f);
            }

            // 속도 벡터 (파란색)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + _agent.velocity);
        }
#endif
    }
}