using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    // 카메라가 따라갈 대상 (플레이어)
    public Transform player;

    // 플레이어 위로 얼마나 떨어져 있을지 결정하는 거리
    public float distance = 5f; // 기본값 설정

    // Slerp를 위한 보간 속도. 값이 낮을수록 더 부드럽고 느리게 따라갑니다.
    public float smoothSpeed = 100f;

    private void LateUpdate()
    {
        // 1. 목표 위치 계산
        // 플레이어 위치 + Vector3.up * distance
        Vector3 targetPosition = player.position + Vector3.up * distance;

        // 2. 현재 위치와 목표 위치 사이를 부드럽게 보간 (Slerp 사용)
        // Slerp(시작 위치, 목표 위치, 보간 계수)
        // 보간 계수: Time.deltaTime * smoothSpeed를 사용하여 프레임당 일정한 속도로 움직입니다.
        transform.position = Vector3.Slerp(
            transform.position,
            targetPosition,
            Time.deltaTime * smoothSpeed
        );
    }
}