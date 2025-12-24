using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestItemActive : MonoBehaviour
{
    public bool isDialogueFinish = false;
    public GameObject[] questItems = new GameObject[2];
    void Start()
    {
        // 자식 오브젝트 찾기
        Transform axe = transform.Find("Axe");
        Transform pistol = transform.Find("Pistol");

        if (axe != null) questItems[0] = axe.gameObject;
        if (pistol != null) questItems[1] = pistol.gameObject;

        // 시작 시점 상태 적용 (기본적으로 꺼져있음)
        UpdateItemState();
    }

    // ★ [추가] NPC가 대화가 끝났을 때 호출할 함수
    public void ActivateItems()
    {
        isDialogueFinish = true;
        UpdateItemState();
        Debug.Log("QuestItemActive: 아이템이 활성화되었습니다.");
    }

    // 아이템 켜고 끄는 로직 분리
    private void UpdateItemState()
    {
        if (questItems[0] != null) questItems[0].SetActive(isDialogueFinish);
        if (questItems[1] != null) questItems[1].SetActive(isDialogueFinish);
    }
}