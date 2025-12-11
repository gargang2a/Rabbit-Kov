using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    // --- 설정 변수 ---
    [Header("Target & Distance")]
    public Transform target; // 플레이어 오브젝트의 Transform
    public float distance = 10f; // 플레이어로부터의 거리

    [Header("Angle Settings")]
    [Range(0f, 360f)]
    public float yAngle = 45f; // 수평 회전 각도 (0도: 정면, 90도: 측면)
    [Range(0f, 90f)]
    public float xAngle = 35f; // 수직 기울기 각도 (0도: 수평, 90도: 정면 탑다운)

    [Header("Smoothing")]
    public float smoothSpeed = 5f; // 카메라 추적 속도 (클수록 빠름)

    // --- 내부 변수 ---
    private Vector3 offset;

    void Start()
    {
        // Target이 설정되지 않았다면 오류 방지
        if (target == null)
        {
            Debug.LogError("카메라의 추적 대상(Target)이 설정되지 않았습니다.");
            enabled = false;
            return;
        }

        // 1. 오프셋 계산 (카메라의 최종 위치)
        CalculateOffset();

        // 카메라의 초기 위치를 Target을 기준으로 설정
        transform.position = target.position + offset;

        // 2. Target을 바라보도록 초기 회전 설정
        transform.LookAt(target);
    }

    // 카메라의 모든 이동 처리는 FixedUpdate에서 실행하는 것이 물리적으로 더 안정적입니다.
    void FixedUpdate()
    {
        if (target == null) return;

        // 1. Target 위치를 기준으로 원하는 카메라의 목표 위치 계산
        Vector3 desiredPosition = target.position + offset;

        // 2. Lerp를 사용하여 현재 위치에서 목표 위치로 부드럽게 이동
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // 3. (선택 사항) Target이 움직일 때마다 카메라가 Target을 바라보도록 회전 업데이트
        transform.LookAt(target);
    }

    /// <summary>
    /// 설정된 각도와 거리를 기반으로 플레이어로부터 떨어진 벡터(오프셋)를 계산합니다.
    /// </summary>
    private void CalculateOffset()
    {
        // 1. 쿼터뷰 각도에 해당하는 회전 쿼터니언 계산
        // Quaternion.Euler(x, y, z)
        // xAngle: 위에서 아래로 기울어지는 각도 (Pitch)
        // yAngle: 플레이어를 중심으로 회전하는 수평 각도 (Yaw)
        Quaternion rotation = Quaternion.Euler(xAngle, yAngle, 0);

        // 2. 계산된 회전을 이용하여 정면(Vector3.back)을 회전시킵니다.
        // Quaternion * Vector3는 해당 벡터를 회전시킨 새로운 벡터를 반환합니다.
        Vector3 direction = rotation * Vector3.back;

        // 3. 계산된 방향에 거리를 곱하여 최종 오프셋 벡터를 얻습니다.
        offset = direction * distance;
    }

    /// <summary>
    /// 스크립트 설정값 변경 시 실시간으로 카메라 오프셋을 업데이트합니다. (에디터 전용)
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && target != null)
        {
            CalculateOffset();
        }
    }
}