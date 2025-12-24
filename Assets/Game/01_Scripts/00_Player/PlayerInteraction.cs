using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask _interactLayer;
    [Tooltip("상호작용 가능한 최대 거리 (반경)")]
    [SerializeField] private float _interactionRadius = 2.5f;
    [Tooltip("스캔 빈도 (초 단위, 0이면 매 프레임). 성능 최적화용.")]
    [SerializeField] private float _scanInterval = 0.1f;

    [Header("UI References")]
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private TextMeshProUGUI _promptText;
    [SerializeField] private float _uiHeightOffset = 2.0f;

    private Player _player;
    private Camera _mainCam;
    private IInteractable _currentInteractable;

    // 성능 최적화를 위한 변수들
    private Collider[] _hitColliders = new Collider[10]; // 최대 10개 아이템까지 감지
    private float _lastScanTime;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _mainCam = Camera.main;

        if (_uiPanel != null) _uiPanel.SetActive(false);
    }

    private void Update()
    {
        // 1. 주기적으로 주변 스캔 (능동 감지)
        if (Time.time - _lastScanTime >= _scanInterval)
        {
            ScanForInteractables();
            _lastScanTime = Time.time;
        }

        // 2. 상호작용 키 입력
        if (Input.GetKeyDown(KeyCode.F) && _currentInteractable != null)
        {
            // 인터페이스인지 확인 후 실행
            _currentInteractable.Interact(_player);

            // 상호작용 직후 UI 갱신을 위해 즉시 재스캔
            ScanForInteractables();
        }

        // 3. 패널 위치 업데이트 (타겟이 있을 때만)
        if (_currentInteractable != null && _uiPanel.activeSelf)
        {
            UpdatePromptPosition();
        }
    }

    // ★ [핵심] Trigger 이벤트 대신 OverlapSphere로 직접 검사
    private void ScanForInteractables()
    {
        // 1. 내 주변 반경 내의 모든 콜라이더를 가져옴 (NonAlloc으로 가비지 생성 방지)
        int numFound = Physics.OverlapSphereNonAlloc(transform.position, _interactionRadius, _hitColliders, _interactLayer);

        IInteractable closestItem = null;
        float closestDistSqr = float.MaxValue;
        Vector3 playerPos = transform.position;

        // 2. 감지된 것들 중 가장 가까운 것 찾기
        for (int i = 0; i < numFound; i++)
        {
            Collider col = _hitColliders[i];

            // 내 손에 들린 무기(자식)는 무시
            if (col.transform.IsChildOf(transform)) continue;

            IInteractable interactable = col.GetComponent<IInteractable>();
            if (interactable != null)
            {
                float distSqr = (col.transform.position - playerPos).sqrMagnitude;
                if (distSqr < closestDistSqr)
                {
                    closestDistSqr = distSqr;
                    closestItem = interactable;
                }
            }
        }

        // 3. 타겟 변경 여부 확인 및 UI 갱신
        if (closestItem != _currentInteractable)
        {
            _currentInteractable = closestItem;
            UpdateUIState();
        }
        // 타겟은 같은데 UI가 꺼져있다면 켜기 (예외 처리)
        else if (_currentInteractable != null && !_uiPanel.activeSelf)
        {
            UpdateUIState();
        }
        // 아무것도 못 찾았는데 UI가 켜져있다면 끄기
        else if (_currentInteractable == null && _uiPanel.activeSelf)
        {
            UpdateUIState();
        }
    }

    private void UpdateUIState()
    {
        if (_currentInteractable != null)
        {
            if (_promptText != null) _promptText.text = _currentInteractable.GetInteractPrompt() + " [F]";
            if (_uiPanel != null) _uiPanel.SetActive(true);
        }
        else
        {
            if (_uiPanel != null) _uiPanel.SetActive(false);
        }
    }

    private void UpdatePromptPosition()
    {
        MonoBehaviour itemMono = _currentInteractable as MonoBehaviour;

        // 아이템이 파괴되었거나(null) 사라졌으면 UI 끄기
        if (itemMono == null)
        {
            _currentInteractable = null;
            if (_uiPanel != null) _uiPanel.SetActive(false);
            return;
        }

        Vector3 worldPos = itemMono.transform.position + Vector3.up * _uiHeightOffset;
        Vector3 screenPos = _mainCam.WorldToScreenPoint(worldPos);

        // 화면 뒤쪽으로 넘어갔을 때 UI 숨김
        if (screenPos.z < 0)
        {
            _uiPanel.SetActive(false);
        }
        else
        {
            if (!_uiPanel.activeSelf) _uiPanel.SetActive(true);
            _uiPanel.transform.position = screenPos;
        }
    }

    // 디버깅용: 씬 뷰에서 감지 범위 그리기
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactionRadius);
    }
}