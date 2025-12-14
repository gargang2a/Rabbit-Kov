using UnityEngine;

/// <summary>
/// 3D 모델을 UI로 사용할 때, 체력 상태에 따라 모델의 색상이나 애니메이션을 제어하는 클래스입니다.
/// Render Texture를 비추는 모델에 부착합니다.
/// </summary>
public class HealthModelPresenter : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Renderer _targetRenderer; // 색상을 바꿀 모델의 렌더러
    [SerializeField] private Animator _targetAnimator; // (선택) 피격/사망 애니메이션용

    [Header("Visual Settings")]
    [SerializeField] private Color _healthyColor = Color.green;
    [SerializeField] private Color _criticalColor = Color.red;
    [SerializeField] private float _flashDuration = 0.2f;

    // 최적화를 위한 PropertyBlock (매번 머티리얼 인스턴스를 생성하지 않음)
    private MaterialPropertyBlock _propBlock;
    private int _colorPropertyId;

    // 상태 관리 변수
    private float _currentFlashTime;
    private bool _isFlashing;

    private void Awake()
    {
        // 초기화: PropertyBlock 및 Shader ID 캐싱
        _propBlock = new MaterialPropertyBlock();
        _colorPropertyId = Shader.PropertyToID("_Color"); // 쉐이더의 Color 프로퍼티 이름 확인 필요 (URP라면 _BaseColor 일 수 있음)
    }

    /// <summary>
    /// 외부(PlayerHealth)에서 체력이 변경될 때 호출합니다.
    /// </summary>
    /// <param name="currentHealth">현재 체력</param>
    /// <param name="maxHealth">최대 체력</param>
    public void OnHealthChanged(float currentHealth, float maxHealth)
    {
        float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);

        // 1. 체력 비율에 따른 색상 보간 (Lerp)
        Color targetColor = Color.Lerp(_criticalColor, _healthyColor, healthRatio);
        UpdateModelColor(targetColor);

        // 2. 애니메이션 파라미터 업데이트 (예: 체력이 낮으면 헐떡거림)
        if (_targetAnimator != null)
        {
            _targetAnimator.SetFloat("HealthRatio", healthRatio);
        }
    }

    /// <summary>
    /// 데미지를 입었을 때 호출하여 피격 효과를 줍니다.
    /// </summary>
    public void OnDamageTaken()
    {
        if (_targetAnimator != null)
        {
            _targetAnimator.SetTrigger("OnHit");
        }

        // 피격 시 잠시 흰색으로 깜빡이는 코루틴 등을 여기서 실행 가능
        // (간단한 예시로 생략)
    }

    /// <summary>
    /// 밤(Night)이 되었을 때 Ghost 등장에 맞춰 UI 분위기를 바꿉니다.
    /// </summary>
    public void OnNightStart()
    {
        // 예: 모델의 눈이 빛나게 하거나, 조명을 어둡게 변경
        if (_targetAnimator != null)
        {
            _targetAnimator.SetBool("IsNight", true);
        }
    }

    private void UpdateModelColor(Color color)
    {
        if (_targetRenderer == null) return;

        // MaterialPropertyBlock을 사용하여 GPU에 색상 전달 (Draw Call Batching 유지)
        _targetRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(_colorPropertyId, color);
        _targetRenderer.SetPropertyBlock(_propBlock);
    }
}