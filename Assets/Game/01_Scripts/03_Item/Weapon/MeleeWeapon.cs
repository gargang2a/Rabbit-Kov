using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeleeWeapon : Weapon
{
    [Header("Collision")]
    [SerializeField] private Collider _hitBox;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _swingClip;

    private MeleeWeaponData _meleeData;
    // private bool _isAttacking = false; // ★ [삭제] 부모 변수 사용
    private HashSet<Collider> _hitEnemies = new HashSet<Collider>();

    private WaitForSeconds _waitAttackDelay;
    private WaitForSeconds _waitAttackDuration;
    private WaitForSeconds _waitCoolTime;

    public override void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        base.Initialize(data, ownerFirePoint);

        _meleeData = data as MeleeWeaponData;

        if (_hitBox != null)
        {
            _hitBox.enabled = false;
            _hitBox.isTrigger = true;
        }

        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.0f;
        }

        _waitAttackDelay = new WaitForSeconds(0.1f);
        _waitAttackDuration = new WaitForSeconds(0.2f);

        float calculatedCoolTime = Mathf.Max(0, _baseData.coolTime - 0.3f);
        _waitCoolTime = new WaitForSeconds(calculatedCoolTime);
    }

    public override void Use()
    {
        // 부모의 _isAttacking 변수 사용
        if (!_isReady || _isAttacking) return;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true; // 부모 변수
        _isReady = false;
        _hitEnemies.Clear();

        PlaySoundWithRandomPitch(_swingClip, 0.9f, 1.1f);

        yield return _waitAttackDelay;
        if (_hitBox != null) _hitBox.enabled = true;

        yield return _waitAttackDuration;
        if (_hitBox != null) _hitBox.enabled = false;

        yield return _waitCoolTime;

        _isAttacking = false; // 부모 변수
        _isReady = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isAttacking && IsEnemy(other.gameObject))
        {
            if (_hitEnemies.Contains(other)) return;
            _hitEnemies.Add(other);

            IDamageable target = other.GetComponent<IDamageable>();
            if (target == null) target = other.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 attackDir = (other.transform.position - transform.position).normalized;
                float knockback = _baseData.CalculatedKnockback;

                target.TakeDamage(_baseData.damage, hitPoint, attackDir, knockback);
                Debug.Log($"[MeleeWeapon] {other.name}에게 {_baseData.damage} 데미지!");
            }
        }
    }

    private void PlaySoundWithRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (_audioSource == null || clip == null) return;
        _audioSource.pitch = Random.Range(minPitch, maxPitch);
        _audioSource.PlayOneShot(clip);
    }

    private bool IsEnemy(GameObject obj)
    {
        if (obj.CompareTag("Enemy")) return true;
        if (obj.layer == LayerMask.NameToLayer("Enemy")) return true;
        return false;
    }
}