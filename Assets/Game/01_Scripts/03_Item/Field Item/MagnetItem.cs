using UnityEngine;

public class MagnetItem : MonoBehaviour
{
    [Header("Effect Settings")]
    public float rotationSpeed = 100f; // 아이템 회전 속도

    void Update()
    {
        // 아이템이 맵에서 뱅글뱅글 돌게 함
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    } // <--- ★ 여기 이 닫는 괄호 '}'가 빠져있었을 확률이 높습니다!

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 자석을 먹었을 때
        if (other.CompareTag("Player"))
        {
            ActivateAllOrbs();

            // 자석 아이템 자체는 파괴
            Destroy(gameObject);
        }
    }

    void ActivateAllOrbs()
    {
        // 1. 씬에 있는 모든 ExpOrb 스크립트를 가진 오브젝트를 배열로 가져옴
        // (구버전 유니티 호환을 위해 FindObjectsOfType 사용)
        ExpOrb[] allOrbs = FindObjectsOfType<ExpOrb>();

        Debug.Log($"{allOrbs.Length}개의 경험치 구슬을 끌어당깁니다!");

        // 2. 모든 구슬의 자석 모드 활성화
        foreach (ExpOrb orb in allOrbs)
        {
            orb.ActivateMagnet();
        }
    }
}