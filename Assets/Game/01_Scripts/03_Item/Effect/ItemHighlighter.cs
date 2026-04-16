using UnityEngine;
using System.Collections;

public class ItemHighlighter : MonoBehaviour
{
    [Header("Motion Settings")]
    [SerializeField] private Vector3 _rotationVelocity = Vector3.zero;
    [SerializeField] private float _floatAmplitude = 0.25f;
    [SerializeField] private float _floatFrequency = 2.0f;

    [Space]
    [Header("Life Cycle Settings")]
    [SerializeField] private bool _enableAutoDestroy = true;
    [SerializeField] private float _lifeTime = 20.0f;
    [SerializeField] private float _blinkDuration = 3.0f;

    private Vector3 _initialPosition;
    private float _timeOffset;
    private Renderer[] _renderers;
    private Coroutine _destroyCoroutine; // 코루틴 제어용 변수

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnEnable()
    {
        // 활성화될 때마다 초기화 (다시 버렸을 때를 대비)
        _initialPosition = transform.position;
        _timeOffset = Random.Range(0f, 2f * Mathf.PI);

        // 렌더러가 꺼져있을 수 있으므로 다시 켜줌
        ToggleRenderers(true);

        if (_enableAutoDestroy)
        {
            _destroyCoroutine = StartCoroutine(AutoDestroyRoutine());
        }
    }

    private void OnDisable()
    {
        // ★ [핵심] 스크립트가 꺼지면(장착/인벤토리행) 파괴 타이머도 즉시 중단
        if (_destroyCoroutine != null)
        {
            StopCoroutine(_destroyCoroutine);
            _destroyCoroutine = null;
        }

        // 렌더러가 깜빡이다가 꺼진 상태로 끝날 수 있으므로 복구
        ToggleRenderers(true);
    }

    private void Update()
    {
        AnimateItem();
    }

    private void AnimateItem()
    {
        transform.Rotate(_rotationVelocity * Time.deltaTime, Space.Self);
        float newY = _initialPosition.y + Mathf.Sin((Time.time + _timeOffset) * _floatFrequency) * _floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    // ★ [New] 외부에서 호출: 아이템을 획득했을 때 호출하세요.
    public void NotifyPickedUp()
    {
        // 1. 타이머 중단 및 컴포넌트 비활성화
        this.enabled = false;

        // 2. (선택사항) 회전/부유로 틀어진 로컬 좌표를 0으로 초기화
        // 손에 쥐었을 때 이상하게 기울어져 있지 않도록 함
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private IEnumerator AutoDestroyRoutine()
    {
        float waitTime = Mathf.Max(0, _lifeTime - _blinkDuration);
        yield return new WaitForSeconds(waitTime);

        float blinkTimer = 0f;
        float blinkInterval = 0.2f;

        while (blinkTimer < _blinkDuration)
        {
            ToggleRenderers(false);
            yield return new WaitForSeconds(blinkInterval);

            ToggleRenderers(true);
            yield return new WaitForSeconds(blinkInterval);

            blinkTimer += (blinkInterval * 2);
            blinkInterval = Mathf.Max(0.05f, blinkInterval * 0.9f);
        }

        Destroy(gameObject);
    }

    private void ToggleRenderers(bool isActive)
    {
        if (_renderers == null) return;
        foreach (var renderer in _renderers)
        {
            if (renderer != null) renderer.enabled = isActive;
        }
    }

    public void ResetInitialPosition()
    {
        _initialPosition = transform.position;
    }
}