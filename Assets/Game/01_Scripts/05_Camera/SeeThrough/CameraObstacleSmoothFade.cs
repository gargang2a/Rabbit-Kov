using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraObstacleSmoothFade : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _fadeSpeed = 5f;
    [SerializeField] private float _targetAlpha = 0.2f;

    // 관리 중인 렌더러와 현재 알파값
    private Dictionary<Renderer, float> _fadingObjects = new Dictionary<Renderer, float>();

    // 성능 최적화를 위한 MaterialPropertyBlock (머티리얼 인스턴스 생성 방지)
    private MaterialPropertyBlock _propBlock;
    private int _alphaScaleID;

    // Raycast 최적화를 위한 버퍼 (NonAlloc 사용 권장)
    private RaycastHit[] _hitBuffer = new RaycastHit[20];

    void Awake()
    {
        _alphaScaleID = Shader.PropertyToID("_AlphaScale");
        _propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (_playerTransform == null) return;

        HandleObstacleFading();
    }

    private void HandleObstacleFading()
    {
        Vector3 direction = _playerTransform.position - transform.position;
        float distance = direction.magnitude;

        // NonAlloc을 사용하여 가비지 컬렉션(GC) 할당 최소화
        int hitCount = Physics.RaycastNonAlloc(transform.position, direction, _hitBuffer, distance, _obstacleLayer);

        HashSet<Renderer> currentHits = new HashSet<Renderer>();

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hitBuffer[i];

            // [수정 포인트 1] Collider가 있는 객체뿐만 아니라 그 자식 객체들의 Renderer도 모두 가져옵니다.
            // 복합적인 구조의 프리팹(부모: Collider, 자식: Mesh)을 대응하기 위함입니다.
            Renderer[] renderers = hit.collider.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;

                currentHits.Add(renderer);

                if (!_fadingObjects.ContainsKey(renderer))
                {
                    // 처음 등록 시 현재 알파값으로 초기화 (보통 1.0)
                    _fadingObjects.Add(renderer, 1.0f);
                }
            }
        }

        ProcessFading(currentHits);
    }

    private void ProcessFading(HashSet<Renderer> currentHits)
    {
        // 딕셔너리 변경 중 오류를 막기 위해 제거할 목록 별도 관리
        List<Renderer> renderersToRemove = new List<Renderer>();

        // 딕셔너리의 키를 복사하여 순회 (GC 발생 가능성 있으나 로직 안전성 우선)
        List<Renderer> activeRenderers = new List<Renderer>(_fadingObjects.Keys);

        foreach (Renderer renderer in activeRenderers)
        {
            if (renderer == null)
            {
                renderersToRemove.Add(renderer);
                continue;
            }

            float targetAlpha = currentHits.Contains(renderer) ? _targetAlpha : 1.0f;
            float currentAlpha = _fadingObjects[renderer];

            // 값이 이미 목표치에 도달했다면 연산 건너뛰기 (최적화)
            if (Mathf.Approximately(currentAlpha, targetAlpha))
            {
                if (!currentHits.Contains(renderer) && Mathf.Approximately(targetAlpha, 1.0f))
                {
                    renderersToRemove.Add(renderer);
                }
                continue;
            }

            float nextAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, _fadeSpeed * Time.deltaTime);
            _fadingObjects[renderer] = nextAlpha;

            // [수정 포인트 2] MaterialPropertyBlock을 사용하여 머티리얼 원본을 훼손하지 않고 값 변경
            renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_alphaScaleID, nextAlpha);
            renderer.SetPropertyBlock(_propBlock);

            // 완전히 불투명해졌고 더 이상 가리지 않는다면 목록에서 제거
            if (!currentHits.Contains(renderer) && Mathf.Approximately(nextAlpha, 1.0f))
            {
                renderersToRemove.Add(renderer);
            }
        }

        foreach (Renderer renderer in renderersToRemove)
        {
            _fadingObjects.Remove(renderer);
        }
    }
}