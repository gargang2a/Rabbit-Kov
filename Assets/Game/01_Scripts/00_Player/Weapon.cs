using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public enum Type { Melee, Range } // 근접, 원거리 구분
    public Type type;
    public int damage;
    public float attackDelay; // 공격 판정이 생기는 딜레이 (애니메이션에 맞춤)
    public BoxCollider meleeArea; // 공격 범위를 담당할 콜라이더
    public TrailRenderer trailEffect; // (선택) 칼 휘두르는 이펙트

    // 공격할 때 호출되는 함수
    public void Use()
    {
        if (type == Type.Melee)
        {
            StopCoroutine("Swing");
            StartCoroutine("Swing");
        }
    }

    IEnumerator Swing()
    {
        // 1. 이펙트 켜기
        if (trailEffect != null) trailEffect.enabled = true;

        // 2. 선딜레이 (칼을 들어올리는 시간 등) - 필요 없으면 0
        yield return new WaitForSeconds(0.1f);

        // 3. 콜라이더 활성화 (이제 닿으면 데미지)
        meleeArea.enabled = true;

        // 4. 휘두르는 시간 (공격 판정 유지 시간)
        yield return new WaitForSeconds(attackDelay);

        // 5. 콜라이더 비활성화 및 정리
        meleeArea.enabled = false;
        if (trailEffect != null) trailEffect.enabled = false;
    }
}