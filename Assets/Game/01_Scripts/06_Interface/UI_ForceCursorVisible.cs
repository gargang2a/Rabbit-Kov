using UnityEngine;

public class UI_ForceCursorVisible : MonoBehaviour
{
    // 이 UI(패널)가 켜져 있을 때 계속 실행됨
    void Update()
    {
        // 마우스 커서 잠금 해제
        Cursor.lockState = CursorLockMode.None;

        // 마우스 커서 보이게 하기
        Cursor.visible = true;
    }
}