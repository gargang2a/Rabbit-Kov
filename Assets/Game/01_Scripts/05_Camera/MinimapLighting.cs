using UnityEngine;

public class MinimapLighting : MonoBehaviour
{
    // 원래 게임의 조명 설정을 기억해둘 변수들
    private UnityEngine.Rendering.AmbientMode originalAmbientMode;
    private Color originalAmbientColor;
    private bool originalFog;

    // ★ 카메라가 화면을 그리기 "직전"에 실행됨
    void OnPreCull()
    {
        // 1. 현재(밤)의 조명 상태를 저장해둠
        originalAmbientMode = RenderSettings.ambientMode;
        originalAmbientColor = RenderSettings.ambientLight;
        originalFog = RenderSettings.fog;

        // 2. 미니맵을 위해 조명을 "강제로 밝게" 변경
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; // 단색 모드
        RenderSettings.ambientLight = Color.white; // 하얀색 조명 (최대 밝기)
        RenderSettings.fog = false; // 안개도 끔 (선명하게)
    }

    // ★ 카메라가 화면을 다 그린 "직후"에 실행됨
    void OnPostRender()
    {
        // 3. 게임 조명을 원래대로(밤으로) 되돌림
        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.ambientLight = originalAmbientColor;
        RenderSettings.fog = originalFog;
    }
}