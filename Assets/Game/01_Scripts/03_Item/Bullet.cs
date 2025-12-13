using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _fadeDuration = 1.0f; // 페이드 시간 단축 권장
    [SerializeField] private float _bounceForce = 3.0f;  // 튕기는 힘의 세기
    [SerializeField] private float _bounceDelay = 0.5f;  // 튕긴 후 사라지기 시작할 대기 시간

    // 외부 접근 프로퍼티
    public int Damage => _damage;

    // 내부 캐싱 변수
    private MeshRenderer _meshRenderer;
    private Collider _collider;
    private Rigidbody _rb;
    private bool _isFading = false;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<Collider>();
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isFading) return;

        if (collision.gameObject.CompareTag("Floor"))
        {
            // 🔴 [Logic Added] 바닥에 닿으면 튕겨오르는 힘 적용
            Ricochet();

            // 튕기는 물리 연산을 보여준 뒤 페이드 아웃 시작
            StartCoroutine(FadeAndDestroyRoutine());
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }

    // 도탄(튕김) 효과 처리 메서드
    private void Ricochet()
    {
        if (_rb != null)
        {
            // 기존 속도를 초기화하여 너무 높게 튀거나 이상하게 튀는 것 방지
            //_rb.velocity = Vector3.zero;

            // 위쪽 방향 + 약간의 무작위 방향으로 힘을 가함
            Vector3 bounceDir = Vector3.up + Random.insideUnitSphere * 0.2f;
            _rb.AddForce(bounceDir.normalized * _bounceForce, ForceMode.Impulse);

            // 회전력(Torque)을 주어 더 자연스럽게 뒹굴게 함
            _rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }
    }

    private IEnumerator FadeAndDestroyRoutine()
    {
        _isFading = true;

        // 🔴 [Critical Fix] 물리 효과가 보일 시간을 줌 (즉시 멈추지 않음)
        yield return new WaitForSeconds(_bounceDelay);

        // 1. 물리 연산 제거 (이제 멈춤)
        //if (_collider != null) _collider.enabled = false;
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            //_rb.isKinematic = true;
        }

        // 2. 투명해지는 로직
        if (_meshRenderer != null)
        {
            Material mat = _meshRenderer.material;

            // 쉐이더 모드 변경이 필요할 수 있음 (Standard Shader 기준 Transparent 설정 필요)
            // 단순히 Color Alpha만 줄인다고 투명해지지 않는 경우가 많으니 주의

            Color initialColor = mat.color;
            float timer = 0f;

            while (timer < _fadeDuration)
            {
                timer += Time.deltaTime;
                float newAlpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);
                mat.color = new Color(initialColor.r, initialColor.g, initialColor.b, newAlpha);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(_fadeDuration);
        }

        // 3. 파괴
        Destroy(gameObject);
    }
}