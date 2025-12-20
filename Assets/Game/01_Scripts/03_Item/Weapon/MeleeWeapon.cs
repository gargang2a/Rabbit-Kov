using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeleeWeapon : Weapon
{
    [Header("Collision")]
    [SerializeField] private Collider _hitBox; // 칼날에 붙은 콜라이더

    // ★ [Audio] 사운드 관련 변수 추가
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _swingClip; // 휘두르는 소리

    private MeleeWeaponData _meleeData;
    private bool _isAttacking = false;
    private HashSet<Collider> _hitEnemies = new HashSet<Collider>(); // 이미 타격한 적 (중복 방지)

    // ★ [Optimization] GC 방지를 위한 코루틴 대기 시간 캐싱
    private WaitForSeconds _waitAttackDelay;    // 공격 판정 전 딜레이 (0.1초)
    private WaitForSeconds _waitAttackDuration; // 공격 판정 지속 시간 (0.2초)
    private WaitForSeconds _waitCoolTime;       // 남은 쿨타임

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint);

        _meleeData = data as MeleeWeaponData;

        if (_hitBox != null)
        {
            _hitBox.enabled = false;  // 평소엔 꺼둠
            _hitBox.isTrigger = true; // 트리거 필수 설정
        }

        // ★ [Audio] 오디오 소스 초기화
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.0f; // 3D 사운드
        }

        // ★ [Optimization] 고정된 시간 캐싱 (메모리 할당 최적화)
        _waitAttackDelay = new WaitForSeconds(0.1f);
        _waitAttackDuration = new WaitForSeconds(0.2f);

        // 쿨타임 계산 (기존 로직: coolTime - 0.3f)
        float calculatedCoolTime = Mathf.Max(0, _baseData.coolTime - 0.3f);
        _waitCoolTime = new WaitForSeconds(calculatedCoolTime);
    }

    public override void Use()
    {
        if (!_isReady || _isAttacking) return;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _isReady = false;
        _hitEnemies.Clear(); // 타격 리스트 초기화

        // ★ [Audio] 공격 시작 시 휘두르는 소리 재생 (랜덤 피치 적용)
        PlaySoundWithRandomPitch(_swingClip, 0.9f, 1.1f);

        // 애니메이션 타이밍에 맞춰 콜라이더 켜기 (예: 0.1초 뒤)
        yield return _waitAttackDelay; // 캐싱된 변수 사용
        if (_hitBox != null) _hitBox.enabled = true;

        // 공격 지속 시간 (예: 0.2초간 판정)
        yield return _waitAttackDuration; // 캐싱된 변수 사용
        if (_hitBox != null) _hitBox.enabled = false;

        // 쿨타임 대기
        yield return _waitCoolTime; // 캐싱된 변수 사용

        _isAttacking = false;
        _isReady = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 공격 중이고 적이면 데미지 처리
        if (_isAttacking && IsEnemy(other.gameObject))
        {
            // 중복 타격 방지
            if (_hitEnemies.Contains(other)) return;
            _hitEnemies.Add(other);

            IDamageable target = other.GetComponent<IDamageable>();

            // 자신에게 없으면 부모에서 찾기
            if (target == null)
            {
                target = other.GetComponentInParent<IDamageable>();
            }

            if (target != null)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 attackDir = (other.transform.position - transform.position).normalized;
                float knockback = _baseData.CalculatedKnockback;

                target.TakeDamage(_baseData.damage, hitPoint, attackDir, knockback);

                // (선택 사항) 여기에 '타격 성공 사운드(Hit Sound)'를 추가할 수도 있습니다.
                // PlaySoundWithRandomPitch(_hitClip); 

                Debug.Log($"[MeleeWeapon] {other.name}에게 {_baseData.damage} 데미지! (넉백: {knockback:F1})");
            }
        }
    }

    // ★ [Helper] 사운드 피치 랜덤 재생 헬퍼
    private void PlaySoundWithRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (_audioSource == null || clip == null) return;

        _audioSource.pitch = Random.Range(minPitch, maxPitch);
        _audioSource.PlayOneShot(clip);
    }

    // 적 판정: 태그 또는 레이어
    private bool IsEnemy(GameObject obj)
    {
        // 태그 체크
        if (obj.CompareTag("Enemy")) return true;

        // 레이어 체크 (레이어 이름: "Enemy")
        if (obj.layer == LayerMask.NameToLayer("Enemy")) return true;

        return false;
    }
}