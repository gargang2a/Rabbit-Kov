using UnityEngine;
using UnityEngine.EventSystems; // 필수

public class UICursorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // 마우스가 UI 영역 안으로 들어왔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DynamicCrosshair.Instance != null)
        {
            // "나 UI 위에 있어! 커스텀 커서 보여줘!"
            DynamicCrosshair.Instance.SetUIHoverState(true);
        }
    }

    // 마우스가 UI 영역 밖으로 나갔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        if (DynamicCrosshair.Instance != null)
        {
            // "나 이제 나갔어. 다시 크로스헤어 보여줘."
            DynamicCrosshair.Instance.SetUIHoverState(false);
        }
    }

    // UI 창이 꺼질 때 (F키로 닫거나 할 때) 강제로 상태 복구
    private void OnDisable()
    {
        if (DynamicCrosshair.Instance != null)
        {
            DynamicCrosshair.Instance.SetUIHoverState(false);
        }
    }
}