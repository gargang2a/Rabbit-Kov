using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // ★ [핵심] 외부에서 접근할 수 있게 하는 싱글톤 인스턴스
    public static CameraShake Instance;

    private void Awake()
    {
        // 게임 시작 시 자기 자신을 Instance에 등록
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 만약 두 개 이상 있으면 나중 건 파괴 (안전장치)
            Destroy(gameObject);
        }
    }

    // 외부에서 호출하는 함수
    public void Shake(float duration, float magnitude)
    {
        StopAllCoroutines(); // 기존 흔들림이 있다면 멈추고 새로 시작
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // 랜덤한 위치로 흔들기
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 제자리로 복귀
        transform.localPosition = originalPos;
    }
}