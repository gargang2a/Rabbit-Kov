using UnityEngine;
using System.Collections;

// [역할] 적 피격 시 빨간색 플래시 이펙트
// - EnemyStats의 OnHit 이벤트 구독
// - 모든 Renderer의 머티리얼 색상을 잠시 빨갛게 변경
[RequireComponent(typeof(EnemyStats))]
public class EnemyHitFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [Tooltip("피격 시 플래시 색상")]
    [SerializeField] private Color _flashColor = new Color(1f, 0.3f, 0.3f, 1f); // 밝은 빨강
    
    [Tooltip("플래시 지속 시간 (초)")]
    [SerializeField] private float _flashDuration = 0.1f;
    
    // 캐싱
    private EnemyStats _stats;
    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    private Color[] _originalColors;
    private Coroutine _flashCoroutine;
    
    // Shader 프로퍼티 ID 캐싱 (성능 최적화)
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor"); // URP용
    
    private void Awake()
    {
        _stats = GetComponent<EnemyStats>();
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        
        // 원래 색상 저장
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i].material.HasProperty(ColorProperty))
            {
                _originalColors[i] = _renderers[i].material.GetColor(ColorProperty);
            }
            else if (_renderers[i].material.HasProperty(BaseColorProperty))
            {
                _originalColors[i] = _renderers[i].material.GetColor(BaseColorProperty);
            }
            else
            {
                _originalColors[i] = Color.white;
            }
        }
    }
    
    private void OnEnable()
    {
        if (_stats != null)
        {
            _stats.OnHit += OnHitReceived;
        }
    }
    
    private void OnDisable()
    {
        if (_stats != null)
        {
            _stats.OnHit -= OnHitReceived;
        }
        
        // 플래시 중단 시 원래 색상 복원
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            RestoreColors();
        }
    }
    
    // 피격 이벤트 핸들러
    private void OnHitReceived(Vector3 attackDirection)
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }
    
    // 플래시 코루틴
    private IEnumerator FlashRoutine()
    {
        // 빨간색으로 변경
        SetFlashColor(_flashColor);
        
        yield return new WaitForSeconds(_flashDuration);
        
        // 원래 색상으로 복원
        RestoreColors();
        _flashCoroutine = null;
    }
    
    // 모든 Renderer에 플래시 색상 적용
    private void SetFlashColor(Color color)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            
            // MaterialPropertyBlock 사용 (인스턴스 복제 방지)
            _renderers[i].GetPropertyBlock(_propBlock);
            
            if (_renderers[i].sharedMaterial.HasProperty(ColorProperty))
            {
                _propBlock.SetColor(ColorProperty, color);
            }
            if (_renderers[i].sharedMaterial.HasProperty(BaseColorProperty))
            {
                _propBlock.SetColor(BaseColorProperty, color);
            }
            
            _renderers[i].SetPropertyBlock(_propBlock);
        }
    }
    
    // 원래 색상 복원
    private void RestoreColors()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            
            _renderers[i].GetPropertyBlock(_propBlock);
            
            if (_renderers[i].sharedMaterial.HasProperty(ColorProperty))
            {
                _propBlock.SetColor(ColorProperty, _originalColors[i]);
            }
            if (_renderers[i].sharedMaterial.HasProperty(BaseColorProperty))
            {
                _propBlock.SetColor(BaseColorProperty, _originalColors[i]);
            }
            
            _renderers[i].SetPropertyBlock(_propBlock);
        }
    }
    
    // [Debug] 에디터에서 테스트
    [ContextMenu("Test Flash")]
    private void TestFlash()
    {
        OnHitReceived(Vector3.forward);
    }
}
