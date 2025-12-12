using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public void QuitApplication()
    {
        #if UNITY_EDITOR
        // 에디터에서 플레이 모드 중지
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 빌드된 게임 종료
        Application.Quit();
        #endif
    }
}
