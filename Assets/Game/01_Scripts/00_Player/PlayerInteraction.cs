using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask _interactLayer;

    [Header("UI References")]
    [SerializeField] private GameObject _uiPanel;       // ★ [추가] 검은색 배경 패널 (부모)
    [SerializeField] private TextMeshProUGUI _promptText; // 글자 (자식)
    [SerializeField] private float _uiHeightOffset = 2.0f;

    private Player _player;
    private IInteractable _currentInteractable;
    private Camera _mainCam;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _mainCam = Camera.main;

        // 시작할 때 패널 끄기
        if (_uiPanel != null) _uiPanel.SetActive(false);
    }

    private void Update()
    {
        // 1. 상호작용 키 입력
        if (Input.GetKeyDown(KeyCode.F) && _currentInteractable != null)
        {
            _currentInteractable.Interact(_player);
            ClearInteractable();
        }

        // 2. 패널 위치 업데이트 (패널이 켜져 있을 때만)
        if (_currentInteractable != null && _uiPanel.activeSelf)
        {
            UpdatePromptPosition();
        }
    }

    private void UpdatePromptPosition()
    {
        MonoBehaviour itemMono = _currentInteractable as MonoBehaviour;

        if (itemMono != null)
        {
            Vector3 worldPos = itemMono.transform.position + Vector3.up * _uiHeightOffset;
            Vector3 screenPos = _mainCam.WorldToScreenPoint(worldPos);

            // ★ 텍스트가 아니라 패널(부모)을 이동시킴
            _uiPanel.transform.position = screenPos;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ★ [추가] 감지된 물체가 내 몸(Transform)의 자식이라면 무시한다.
        // (즉, 내가 손에 들고 있는 무기라면 상호작용 띄우지 않음)
        if (other.transform.IsChildOf(transform)) return;

        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && CheckLayerMask(other.gameObject.layer))
        {
            // 텍스트 내용 바꾸고, 패널을 켠다
            if (_promptText != null) _promptText.text = interactable.GetInteractPrompt() + " [F]";
            if (_uiPanel != null) _uiPanel.SetActive(true);

            _currentInteractable = interactable;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && interactable == _currentInteractable)
        {
            ClearInteractable();
        }
    }

    private void ClearInteractable()
    {
        // ★ 패널을 끈다
        if (_uiPanel != null) _uiPanel.SetActive(false);
        _currentInteractable = null;
    }

    private bool CheckLayerMask(int layer)
    {
        return (_interactLayer.value & (1 << layer)) != 0;
    }
}