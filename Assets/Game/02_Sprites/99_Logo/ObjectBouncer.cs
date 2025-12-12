using UnityEngine;
using DG.Tweening; // DOTween 네임스페이스 필수

public class ObjectBouncer : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("애니메이션을 적용할 대상 (비워두면 자기 자신)")]
    [SerializeField] private Transform _targetTransform;

    [Header("Animation Settings")]
    [Tooltip("얼마나 커질지 설정 (1.2 = 1.2배 커짐)")]
    [SerializeField] private float _scaleMultiplier = 1.1f;

    [Tooltip("한 번 커지는 데 걸리는 시간")]
    [SerializeField] private float _duration = 0.8f;

    [Tooltip("애니메이션의 부드러움 정도 (InOutSine이 가장 무난함)")]
    [SerializeField] private Ease _easeType = Ease.InOutSine;

    private void Start()
    {
        InitializeAndStartBounce();
    }

    private void InitializeAndStartBounce()
    {
        if (_targetTransform == null)
        {
            _targetTransform = transform;
        }

        // 핵심 로직: 현재 크기에서 목표 크기까지 갔다가 돌아오기를 무한 반복
        _targetTransform.DOScale(_targetTransform.localScale * _scaleMultiplier, _duration)
            .SetLoops(-1, LoopType.Yoyo) // -1: 무한 반복, Yoyo: 갔다가 되돌아옴
            .SetEase(_easeType)          // 부드러운 움직임 적용
            .SetLink(gameObject);        // 오브젝트가 파괴되면 트윈도 같이 삭제 (메모리 안전)
    }
}