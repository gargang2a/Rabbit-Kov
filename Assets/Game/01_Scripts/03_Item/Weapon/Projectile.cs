using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 총알 (Projectile) - 발사 후 직선 이동, 적중 시 데미지 처리
public class Projectile : MonoBehaviour
{
    [Header("Hit Effect")]
    [Tooltip("피격 시 생성될 이펙트 프리팹 (ParticleSystem)")]
    [SerializeField] private GameObject _hitEffectPrefab;
    
    private int _damage;         // 데미지
    private float _speed;        // 이동 속도
    private float _maxRange;     // 최대 사거리
    private float _knockback;    // 넉백 강도
    private Vector3 _startPos;   // 시작 위치
    private Vector3 _lastPos;    // 이전 프레임 위치 (레이캐스트용)

    // 초기화 (RangedWeapon.Fire에서 호출)
    public void Setup(int damage, float speed, float range, float knockback = 0f)
    {
        _damage = damage;
        _speed = speed;
        _maxRange = range;
        _knockback = knockback;
        _startPos = transform.position;
        _lastPos = transform.position;
        Destroy(gameObject, 5f); // 안전장치 (5초 후 자동 삭제)
    }
    
    // 외부에서 히트 이펙트 설정 (RangedWeapon에서 호출)
    public void SetHitEffect(GameObject hitEffectPrefab)
    {
        _hitEffectPrefab = hitEffectPrefab;
    }

    private void Update()
    {
        _lastPos = transform.position; // 이전 위치 저장
        transform.Translate(Vector3.forward * _speed * Time.deltaTime); // 직선 이동

        if (Vector3.Distance(_startPos, transform.position) >= _maxRange) // 사거리 초과
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 적 판정: 태그 OR 레이어 (둘 중 하나)
        if (IsEnemy(other.gameObject))
        {
            // 정확한 피격 위치 계산 (레이캐스트로 표면 찾기)
            Vector3 hitPoint = GetExactHitPoint(other);
            Vector3 hitNormal = GetHitNormal(other, hitPoint);
            
            // 히트 이펙트 생성 (표면 위치 + 법선 방향)
            SpawnHitEffect(hitPoint, hitNormal);
            
            IDamageable target = other.GetComponent<IDamageable>();
            
            // 자신에게 없으면 부모에서 찾기
            if (target == null)
            {
                target = other.GetComponentInParent<IDamageable>();
            }
            
            if (target != null)
            {
                Vector3 attackDir = transform.forward;
                target.TakeDamage(_damage, hitPoint, attackDir, _knockback);
                Debug.Log($"[Projectile] {other.name}에게 {_damage} 데미지!");
            }
            Destroy(gameObject);
        }
        // 벽 충돌 처리
        else if (other.CompareTag("Wall"))
        {
            Vector3 hitPoint = GetExactHitPoint(other);
            Vector3 hitNormal = GetHitNormal(other, hitPoint);
            SpawnHitEffect(hitPoint, hitNormal);
            Destroy(gameObject);
        }
    }
    
    // 정확한 히트 포인트 계산 (레이캐스트)
    private Vector3 GetExactHitPoint(Collider other)
    {
        // 이전 위치에서 현재 위치로 레이캐스트
        Vector3 direction = (transform.position - _lastPos).normalized;
        float distance = Vector3.Distance(_lastPos, transform.position) + 1f; // 약간 여유
        
        RaycastHit hit;
        if (Physics.Raycast(_lastPos, direction, out hit, distance))
        {
            if (hit.collider == other)
            {
                return hit.point; // 정확한 표면 위치
            }
        }
        
        // 레이캐스트 실패 시 ClosestPoint 사용
        return other.ClosestPoint(transform.position);
    }
    
    // 히트 법선 계산
    private Vector3 GetHitNormal(Collider other, Vector3 hitPoint)
    {
        Vector3 direction = (transform.position - _lastPos).normalized;
        float distance = Vector3.Distance(_lastPos, transform.position) + 1f;
        
        RaycastHit hit;
        if (Physics.Raycast(_lastPos, direction, out hit, distance))
        {
            if (hit.collider == other)
            {
                return hit.normal; // 표면 법선
            }
        }
        
        // 실패 시 총알 반대 방향
        return -transform.forward;
    }
    
    // 히트 이펙트 생성
    private void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        if (_hitEffectPrefab == null) return;
        
        // 법선 방향으로 회전 (표면에서 튀는 방향)
        Quaternion rotation = Quaternion.LookRotation(normal);
        
        GameObject effect = Instantiate(_hitEffectPrefab, position, rotation);
        
        // ParticleSystem 자동 삭제
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(effect, duration);
        }
        else
        {
            Destroy(effect, 2f);
        }
    }
    
    // 적 판정: 태그 또는 레이어
    private bool IsEnemy(GameObject obj)
    {
        if (obj.CompareTag("Enemy")) return true;
        if (obj.layer == LayerMask.NameToLayer("Enemy")) return true;
        return false;
    }
}