using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform player; // 플레이어 트랜스폼

    void LateUpdate()
    {
        if (player != null)
        {
            // 플레이어의 X, Z 위치만 따라가고, 높이(Y)는 현재 카메라 높이 유지
            Vector3 newPosition = player.position;
            newPosition.y = transform.position.y;

            transform.position = newPosition;

            // 회전은 고정 (X:90도로 바닥 보기)
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}