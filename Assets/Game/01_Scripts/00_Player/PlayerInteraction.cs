using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _interactRange = 3f;
    [SerializeField] private LayerMask _interactLayer;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _promptText;

    // 카메라 대신 플레이어 자신의 위치를 사용하기 위해 transform 사용

    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        CheckInteraction();
    }

    private void CheckInteraction()
    {
        // 1. 레이저 시작점: 플레이어의 가슴 높이 (발밑 + 1.0f)
        Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;

        // 2. 레이저 방향: 플레이어가 바라보는 앞쪽
        Vector3 rayDirection = transform.forward;

        RaycastHit hit;

        // 디버그: 씬 뷰에서 빨간 선 확인용
        Debug.DrawRay(rayOrigin, rayDirection * _interactRange, Color.red);

        // ★ [수정됨] rayDirection을 두 번째 인자로 추가했습니다.
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, _interactRange, _interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (_promptText != null)
                {
                    _promptText.text = interactable.GetInteractPrompt() + " [F]";
                    _promptText.gameObject.SetActive(true);
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    interactable.Interact(_player);
                }
                return;
            }
        }

        if (_promptText != null) _promptText.gameObject.SetActive(false);
    }
}