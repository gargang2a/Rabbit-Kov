using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    void Start()
    {
        // 시스템 커서 숨기기 및 고정
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}