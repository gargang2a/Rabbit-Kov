using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{

    // 총알 케이스 // 총알 탄두 구분

    [Header("Settings")]
    [SerializeField] private int _damage = 10;

    [SerializeField] private float _fadeDuration = 0.5f; // 페이드 시간 (조절됨)
    [SerializeField] private float _bounceForce = 3.0f;  // 튕기는 힘
    [SerializeField] private float _bounceDelay = 0.5f;  // 튕긴 후 사라지기까지 대기 시간

    // 내부 상태 변수
    private MeshRenderer _meshRenderer;
    private Rigidbody _rb;
    private bool _isFading = false;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 이미 사라지는 중이라면 추가 충돌 무시
        if (_isFading) return;

        // 1. 적(IDamageable)과 충돌했는지 확인 (가장 우선순위 높음)
        // TryGetComponent를 사용하여 성능 최적화 (GetComponent + Null Check를 한 번에 수행)
        if (collision.gameObject.TryGetComponent(out IDamageable target))
        {
            // 공격 방향 계산 (총알의 진행 방향)
            Vector3 attackDirection = transform.forward;

            // 정확한 타격 지점 (충돌 지점)
            Vector3 hitPoint = collision.contacts[0].point;

            // 인터페이스를 통해 데미지 전달
            target.TakeDamage(_damage, hitPoint, attackDirection);

            // 적에게 맞았으므로 총알 즉시 삭제 (관통 효과가 필요 없다면)
            Destroy(gameObject);
            return;
        }

        // 2. 바닥(Floor)과 충돌 시 도탄 효과
        if (collision.gameObject.CompareTag("Floor"))
        {
            Ricochet();
            StartCoroutine(FadeAndDestroyRoutine());
        }
        // 3. 벽(Wall)과 충돌 시 즉시 삭제
        else if (collision.gameObject.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
        // 4. 그 외(Default 등) 충돌 시 삭제 (필요에 따라 조정 가능)
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 바닥에 닿았을 때 튕겨 나가는 물리 효과를 적용합니다.
    /// </summary>
    private void Ricochet()
    {
        if (_rb != null)
        {
            // 위쪽 방향 + 약간의 무작위 방향으로 힘을 가함
            Vector3 bounceDir = Vector3.up + Random.insideUnitSphere * 0.2f;
            _rb.AddForce(bounceDir.normalized * _bounceForce, ForceMode.Impulse);

            // 회전력(Torque)을 주어 자연스럽게 뒹굴게 함
            _rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// 일정 시간 대기 후 투명해지며 사라지는 코루틴입니다.
    /// </summary>
    private IEnumerator FadeAndDestroyRoutine()
    {
        _isFading = true;

        // 물리 효과가 보일 시간을 줌 (즉시 멈추지 않음)
        yield return new WaitForSeconds(_bounceDelay);

        // 물리 연산 정지 (제자리에서 멈춤)
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true; // 더 이상 물리 영향을 받지 않음
        }

        // 투명하게 페이드 아웃
        if (_meshRenderer != null)
        {
            Material mat = _meshRenderer.material;
            Color initialColor = mat.color;
            float timer = 0f;

            while (timer < _fadeDuration)
            {
                timer += Time.deltaTime;
                float newAlpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);

                // 주의: Material의 Rendering Mode가 Transparent 또는 Fade여야 알파값이 적용됨
                mat.color = new Color(initialColor.r, initialColor.g, initialColor.b, newAlpha);

                yield return null;
            }
        }
        else
        {
            // 렌더러가 없으면 그냥 대기
            yield return new WaitForSeconds(_fadeDuration);
        }

        // 오브젝트 파괴
        Destroy(gameObject);
    }
}