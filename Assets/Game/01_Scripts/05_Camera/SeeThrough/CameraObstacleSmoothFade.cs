using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraObstacleSmoothFade : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _fadeSpeed = 5f; // 변하는 속도
    [SerializeField] private float _targetAlpha = 0.2f; // 가려졌을 때의 투명도 (0.2 정도가 적당)

    // 관리 중인 렌더러와 현재 알파값
    private Dictionary<Renderer, float> _fadingObjects = new Dictionary<Renderer, float>();

    // 쉐이더 프로퍼티 ID 캐싱
    private int _alphaScaleID;

    void Awake()
    {
        // 쉐이더의 "_AlphaScale" 변수 ID 가져오기
        _alphaScaleID = Shader.PropertyToID("_AlphaScale");
    }

    void Update()
    {
        if (_playerTransform == null) return;

        Vector3 dir = _playerTransform.position - transform.position;
        float dist = dir.magnitude;

        // 1. 레이캐스트
        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir, dist, _obstacleLayer);

        HashSet<Renderer> currentHits = new HashSet<Renderer>();

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                currentHits.Add(rend);

                if (!_fadingObjects.ContainsKey(rend))
                {
                    // 처음 등록될 때 현재 알파값(보통 1)으로 시작
                    _fadingObjects.Add(rend, 1.0f);
                }
            }
        }

        // 2. 투명도 업데이트 (Lerp)
        ProcessFading(currentHits);
    }

    private void ProcessFading(HashSet<Renderer> currentHits)
    {
        List<Renderer> toRemove = new List<Renderer>();
        List<Renderer> keys = new List<Renderer>(_fadingObjects.Keys);

        foreach (Renderer rend in keys)
        {
            if (rend == null)
            {
                toRemove.Add(rend);
                continue;
            }

            // 목표값 설정: 가리고 있으면 _targetAlpha, 아니면 1.0f
            float target = currentHits.Contains(rend) ? _targetAlpha : 1.0f;
            float current = _fadingObjects[rend];

            // 부드럽게 값 변경
            float nextAlpha = Mathf.MoveTowards(current, target, _fadeSpeed * Time.deltaTime);

            _fadingObjects[rend] = nextAlpha;

            // 머티리얼에 적용
            rend.material.SetFloat(_alphaScaleID, nextAlpha);

            // 완전히 불투명해졌고(1.0), 더 이상 가리지 않는다면 리스트에서 제거
            if (!currentHits.Contains(rend) && Mathf.Approximately(nextAlpha, 1.0f))
            {
                toRemove.Add(rend);
            }
        }

        foreach (var rend in toRemove)
        {
            _fadingObjects.Remove(rend);
        }
    }
}