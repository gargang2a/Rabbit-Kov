using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake instance;

    private Transform camTransform;
    private Vector3 originalPos; // 흔들리기 직전의 위치 저장용

    private void Awake()
    {
        if (instance == null) instance = this;
        camTransform = GetComponent<Transform>();
    }

    public void Shake(float duration, float magnitude)
    {
        // ★ [수정됨] 흔들기 시작하는 '바로 그 순간'의 위치를 기준점으로 잡음
        originalPos = camTransform.localPosition;

        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // 랜덤한 X, Y 오프셋 생성
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // ★ [수정됨] 원래 위치(originalPos)를 기준으로 x, y만 더함
            // Z축(깊이)은 0을 더해서 줌인/줌아웃 방지
            camTransform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;

            yield return null;
        }

        // 흔들림이 끝나면 원래 있던 자리로 정확히 복구
        camTransform.localPosition = originalPos;
    }
}