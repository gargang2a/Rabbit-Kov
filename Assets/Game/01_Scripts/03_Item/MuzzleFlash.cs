using UnityEngine;
using System.Collections;

public class MuzzleFlash : MonoBehaviour
{
    [SerializeField] private Light _flashLight;
    [SerializeField] private ParticleSystem _particles;
    [SerializeField] private float _lightDuration = 0.05f;

    public void Activate()
    {
        // 🔴 [Fix] 코루틴 시작 전에 반드시 오브젝트를 켜야 합니다!
        gameObject.SetActive(true);

        // 파티클 재생
        if (_particles != null)
        {
            _particles.Stop();
            _particles.Play();
        }

        // 조명 깜빡임 코루틴 시작
        StartCoroutine(FlashLightRoutine());
    }

    private IEnumerator FlashLightRoutine()
    {
        // 불 켜기
        if (_flashLight != null) _flashLight.enabled = true;

        // 아주 짧게 대기 (번쩍!)
        yield return new WaitForSeconds(_lightDuration);

        // 불 끄기
        if (_flashLight != null) _flashLight.enabled = false;

        // 🟡 [Optional] 파티클이 다 사라질 때까지 기다렸다가 오브젝트 끄기
        // (파티클 지속시간보다 조금 더 길게 대기)
        yield return new WaitForSeconds(0.2f);

        // 오브젝트 비활성화 (다음 발사를 위해 숨김)
        gameObject.SetActive(false);
    }
}