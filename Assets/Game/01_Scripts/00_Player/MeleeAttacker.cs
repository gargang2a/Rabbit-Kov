using System.Collections;
using UnityEngine;

public class MeleeAttacker : MonoBehaviour
{
    [Header("Attack Settings")]
    public float damage = 10f;          // 공격 데미지
    public float attackRange = 1.5f;    // 공격 사거리 (반지름)
    public float attackDelay = 0.2f;    // 공격 키 누른 후 판정이 생길 때까지의 시간 (선딜레이)
    public float attackCooldown = 0.5f; // 다음 공격까지 걸리는 시간 (후딜레이)
    public LayerMask enemyLayer;        // 적 레이어 (Enemy로 설정된 레이어만 공격)
    public Transform attackPoint;       // 공격 판정의 중심점 (주로 무기 앞이나 캐릭터 앞)

    [Header("References")]
    public Animator animator;           // 애니메이터 컴포넌트

    private bool isAttacking = false;   // 현재 공격 중인지 확인하는 플래그
    


    void Update()
    {
        // 공격 중이 아니고, 공격 키(여기선 마우스 좌클릭)를 눌렀을 때
        if (Input.GetButtonDown("Fire1") && !isAttacking)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // 1. 공격 애니메이션 실행
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // 2. 공격 판정 타이밍까지 대기 (칼을 휘두르는 모션에 맞춤)
        yield return new WaitForSeconds(attackDelay);

        //// 3. 범위 내의 적 감지 및 데미지 처리
        //DetectAndDamageEnemies();

        // 4. 공격 후 딜레이 (쿨타임) 대기
        yield return new WaitForSeconds(attackCooldown);

        // 5. 공격 상태 해제
        isAttacking = false;
    }

    //void DetectAndDamageEnemies()
    //{
    //    // attackPoint 위치를 기준으로 attackRange 반경 내의 enemyLayer를 가진 콜라이더들을 찾음
    //    Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);

    //    foreach (Collider enemy in hitEnemies)
    //    {
    //        Debug.Log(enemy.name + "을(를) 공격했습니다!");

    //        // 적에게 데미지를 주는 함수 호출 (적 스크립트에 TakeDamage 함수가 있다고 가정)
    //        // enemy.GetComponent<EnemyHealth>()?.TakeDamage(damage);
    //    }
    //}

    // 에디터에서 공격 범위를 눈으로 확인하기 위한 기즈모
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}