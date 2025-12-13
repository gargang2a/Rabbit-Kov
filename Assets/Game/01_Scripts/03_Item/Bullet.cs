using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _fadeDuration = 2.0f; // 사라지는 데 걸리는 시간

    // 외부 접근 프로퍼티
    public int Damage => _damage;

    // 내부 캐싱 변수
    private MeshRenderer _meshRenderer;
    private Collider _collider;
    private Rigidbody _rb;
    private bool _isFading = false; // 중복 실행 방지

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<Collider>();
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 이미 페이드 아웃 중이라면 로직 무시
        if (_isFading) return;

        if (collision.gameObject.CompareTag("Floor"))
        {
            // 바닥에 닿으면 페이드 아웃 코루틴 시작
            StartCoroutine(FadeAndDestroyRoutine());
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            // 벽에 닿으면 즉시 파괴 (혹은 필요시 여기도 페이드 적용 가능)
            Destroy(gameObject);
        }
    }

    private IEnumerator FadeAndDestroyRoutine()
    {
        _isFading = true;

        // 1. 물리 연산 제거 (바닥에 굴러다니는 총알에 플레이어가 걸리지 않게 함)
        if (_collider != null) _collider.enabled = false;
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.isKinematic = true; // 물리 영향 받지 않게 고정
        }

        // 2. 투명해지는 로직
        if (_meshRenderer != null)
        {
            Material mat = _meshRenderer.material;
            Color initialColor = mat.color;
            float timer = 0f;

            while (timer < _fadeDuration)
            {
                timer += Time.deltaTime;
                // Alpha 값을 1(불투명)에서 0(투명)으로 보간
                float newAlpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);

                mat.color = new Color(initialColor.r, initialColor.g, initialColor.b, newAlpha);

                yield return null; // 다음 프레임까지 대기
            }
        }
        else
        {
            // 렌더러가 없다면 그냥 대기 후 삭제
            yield return new WaitForSeconds(_fadeDuration);
        }

        // 3. 완전히 투명해지면 오브젝트 파괴
        Destroy(gameObject);
    }
}