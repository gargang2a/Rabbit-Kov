using UnityEngine;

/// <summary>
/// 아이템의 시각적 강조를 위해 3축 회전 및 부유 효과를 부여하는 클래스입니다.
/// Escape from Duckov 프로젝트의 파밍 아이템(Loot)에 부착하여 사용합니다.
/// </summary>
public class ItemHighlighter : MonoBehaviour
{
    [Header("Motion Settings")]
    [Tooltip("X, Y, Z 축별 초당 회전 속도 (도/초). 기본값은 (0, 0, 0)입니다.")]
    [SerializeField] private Vector3 _rotationVelocity = Vector3.zero; // 요청하신 대로 기본값 0

    [Tooltip("위아래로 움직이는 최대 높이 (Amplitude)")]
    [SerializeField] private float _floatAmplitude = 0.25f;

    [Tooltip("위아래 움직임의 빈도/속도 (Frequency)")]
    [SerializeField] private float _floatFrequency = 2.0f;

    // 시작 위치를 저장하여 기준점으로 삼습니다.
    private Vector3 _initialPosition;
    // 랜덤한 시작 오프셋
    private float _timeOffset;

    private void Start()
    {
        _initialPosition = transform.position;
        _timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    private void Update()
    {
        AnimateItem();
    }

    private void AnimateItem()
    {
        // 1. Rotation (3축 자유 회전)
        // Vector3를 사용하여 X, Y, Z 축 각각의 속도로 회전시킵니다.
        // Space.Self를 사용하여 로컬 축 기준으로 회전합니다 (아이템이 기울어져 있어도 자연스럽게 돔).
        transform.Rotate(_rotationVelocity * Time.deltaTime, Space.Self);

        // 2. Floating (Sin 파동)
        // 부유 효과는 Y축 위치만 변경합니다.
        float newY = _initialPosition.y + Mathf.Sin((Time.time + _timeOffset) * _floatFrequency) * _floatAmplitude;

        transform.position = new Vector3(_initialPosition.x, newY, _initialPosition.z);
    }

    /// <summary>
    /// 물리 충돌 등으로 위치가 어긋났을 때 기준점을 재설정합니다.
    /// </summary>
    public void ResetInitialPosition()
    {
        _initialPosition = transform.position;
    }
}