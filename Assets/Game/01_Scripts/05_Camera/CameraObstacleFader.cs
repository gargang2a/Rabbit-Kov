using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraObstacleHider : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private LayerMask _obstacleLayer;

    // 이번 프레임에 감지된 렌더러들
    private HashSet<Renderer> _currentHitRenderers = new HashSet<Renderer>();

    // 이전에 감지되어 꺼져있는 렌더러들
    private Dictionary<Renderer, Coroutine> _disabledRenderers = new Dictionary<Renderer, Coroutine>();

    void Update()
    {
        if (_playerTransform == null) return;

        Vector3 dir = _playerTransform.position - transform.position;
        float dist = dir.magnitude;

        // 레이캐스트로 가리는 물체 찾기
        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir, dist, _obstacleLayer);
        _currentHitRenderers.Clear();

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                _currentHitRenderers.Add(rend);
                HideRenderer(rend);
            }
        }

        // 더 이상 가리지 않는 물체는 다시 켜기
        List<Renderer> toRemove = new List<Renderer>();
        foreach (var kvp in _disabledRenderers)
        {
            if (!_currentHitRenderers.Contains(kvp.Key))
            {
                ShowRenderer(kvp.Key);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var rend in toRemove)
        {
            _disabledRenderers.Remove(rend);
        }
    }

    private void HideRenderer(Renderer rend)
    {
        if (!_disabledRenderers.ContainsKey(rend))
        {
            // 렌더러 끄기 (안 보이게 됨)
            rend.enabled = false;
            _disabledRenderers.Add(rend, null);
        }
    }

    private void ShowRenderer(Renderer rend)
    {
        if (rend != null)
        {
            // 렌더러 켜기 (다시 보임)
            rend.enabled = true;
        }
    }
}