using UnityEngine;

// [Escape from Duckov] FirePoint Rotation Logic
public class FirePointSlopeAdjuster : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _rayDistance = 2.0f;
    [SerializeField] private float _rayOffset = 0.5f; // 발밑보다 약간 위에서 레이 시작

    [Header("References")]
    [SerializeField] private Transform _ownerTransform; // 플레이어 본체 Transform

    private void Awake()
    {
        // 인스펙터에서 할당 안 했을 경우 최상위 부모(플레이어)를 자동으로 찾음
        if (_ownerTransform == null)
        {
            _ownerTransform = transform.root;
        }
    }

    // Update보다는 모든 이동/회전이 끝난 LateUpdate에서 각도를 맞추는 것이 정확함
    private void LateUpdate()
    {
        AdjustRotation();
    }

    private void AdjustRotation()
    {
        if (_ownerTransform == null) return;

        // 1. 플레이어 위치에서 바닥으로 레이를 쏨
        Vector3 rayStartPos = _ownerTransform.position + Vector3.up * _rayOffset;
        Ray ray = new Ray(rayStartPos, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _groundLayer))
        {
            // 2. 플레이어의 현재 정면 방향(Y축 회전 포함)을 가져옴
            Vector3 playerForward = _ownerTransform.forward;

            // 3. [핵심] 플레이어의 정면 벡터를 바닥의 기울기(Normal) 평면에 투영
            // 이 결과값은 바닥의 경사를 따라 흐르는 방향 벡터가 됨
            Vector3 slopeForward = Vector3.ProjectOnPlane(playerForward, hit.normal).normalized;

            // 4. FirePoint의 회전값을 해당 방향으로 업데이트
            // 이제 FirePoint.forward는 지면과 완벽하게 평행함
            transform.rotation = Quaternion.LookRotation(slopeForward);

            // 디버깅용: 씬 뷰에서 발사 궤적 확인 (녹색 선)
            Debug.DrawRay(transform.position, slopeForward * 2f, Color.green);
        }
        else
        {
            // 바닥이 감지되지 않을 경우(공중 등) 플레이어의 기본 회전과 동기화
            transform.rotation = _ownerTransform.rotation;
        }
    }
}