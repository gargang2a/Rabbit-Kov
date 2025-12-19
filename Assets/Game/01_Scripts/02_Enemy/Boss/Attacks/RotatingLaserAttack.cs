using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// [역할] 3페이즈 공격 - 회전 레이저 (4갈래, 360도 회전)
public class RotatingLaserAttack : MonoBehaviour, IBossAttack
{
    [Header("공격 설정")]
    [SerializeField] private string _attackName = "회전 레이저";
    [SerializeField] private float _cooldown = 8f;
    
    [Header("레이저 설정")]
    [Tooltip("레이저 갈래 수")]
    [SerializeField] private int _laserCount = 4;
    
    [Tooltip("레이저 길이")]
    [SerializeField] private float _laserLength = 20f;
    
    [Tooltip("회전 속도 (도/초)")]
    [SerializeField] private float _rotationSpeed = 60f;
    
    [Tooltip("총 회전 각도 (360 = 1바퀴)")]
    [SerializeField] private float _totalRotation = 360f;
    
    [Tooltip("레이저 두께 (판정 범위)")]
    [SerializeField] private float _laserWidth = 0.5f;
    
    [Tooltip("데미지 (초당)")]
    [SerializeField] private int _damagePerSecond = 100;
    
    [Tooltip("선딜레이 (초)")]
    [SerializeField] private float _windupTime = 1f;
    
    [Header("프리팹/비주얼")]
    [Tooltip("레이저 빔 프리팹")]
    [SerializeField] private GameObject _laserPrefab;
    
    [Tooltip("경고 이펙트")]
    [SerializeField] private GameObject _warningEffect;
    
    private bool _isExecuting = false;
    private Coroutine _attackCoroutine;
    private List<GameObject> _activeLasers = new List<GameObject>();
    private float _currentRotation = 0f;
    
    public string AttackName => _attackName;
    public float Cooldown => _cooldown;
    public bool IsExecuting => _isExecuting;

    public void Execute(BossController boss, Transform target)
    {
        if (_isExecuting) return;
        
        _attackCoroutine = StartCoroutine(ExecuteAttackRoutine(boss));
    }

    public void Cancel()
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }
        
        foreach (var laser in _activeLasers)
        {
            if (laser != null) Destroy(laser);
        }
        _activeLasers.Clear();
        
        _isExecuting = false;
    }

    private IEnumerator ExecuteAttackRoutine(BossController boss)
    {
        _isExecuting = true;
        Transform bossTransform = boss.transform;
        
        // 1. 선딜레이
        if (_warningEffect != null)
        {
            GameObject warning = Instantiate(_warningEffect, bossTransform.position, Quaternion.identity);
            Destroy(warning, _windupTime);
        }
        
        yield return new WaitForSeconds(_windupTime);

        // 2. 레이저 생성
        float angleStep = 360f / _laserCount;
        for (int i = 0; i < _laserCount; i++)
        {
            float angle = i * angleStep;
            GameObject laser = CreateLaser(bossTransform, angle);
            _activeLasers.Add(laser);
        }

        // 3. 회전
        _currentRotation = 0f;
        float damageTimer = 0f;
        
        while (_currentRotation < _totalRotation)
        {
            float rotateThisFrame = _rotationSpeed * Time.deltaTime;
            _currentRotation += rotateThisFrame;
            
            foreach (var laser in _activeLasers)
            {
                if (laser != null)
                {
                    laser.transform.RotateAround(bossTransform.position, Vector3.up, rotateThisFrame);
                }
            }
            
            // 데미지 판정 (0.1초마다)
            damageTimer += Time.deltaTime;
            if (damageTimer >= 0.1f)
            {
                damageTimer = 0f;
                CheckLaserDamage(bossTransform.position);
            }
            
            yield return null;
        }

        // 4. 레이저 정리
        foreach (var laser in _activeLasers)
        {
            if (laser != null) Destroy(laser);
        }
        _activeLasers.Clear();

        _isExecuting = false;
    }

    private GameObject CreateLaser(Transform boss, float angle)
    {
        Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        Vector3 endPos = boss.position + direction * _laserLength;
        
        if (_laserPrefab != null)
        {
            GameObject laser = Instantiate(_laserPrefab, boss.position, Quaternion.LookRotation(direction));
            laser.transform.localScale = new Vector3(_laserWidth, _laserWidth, _laserLength);
            return laser;
        }
        else // 프리팹 없으면 큐브로 대체
        {
            GameObject laser = GameObject.CreatePrimitive(PrimitiveType.Cube);
            laser.transform.position = boss.position + direction * (_laserLength / 2f);
            laser.transform.rotation = Quaternion.LookRotation(direction);
            laser.transform.localScale = new Vector3(_laserWidth, _laserWidth, _laserLength);
            
            laser.GetComponent<Collider>().isTrigger = true;
            laser.GetComponent<Renderer>().material.color = Color.red;
            
            return laser;
        }
    }

    private void CheckLaserDamage(Vector3 bossPos)
    {
        foreach (var laser in _activeLasers)
        {
            if (laser == null) continue;
            
            Vector3 direction = laser.transform.forward;
            RaycastHit[] hits = Physics.SphereCastAll(bossPos, _laserWidth, direction, _laserLength);
            
            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag("Player"))
                {
                    int damage = Mathf.RoundToInt(_damagePerSecond * 0.1f);
                    Debug.Log($"[RotatingLaser] 플레이어 적중! (Damage: {damage})");
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        
        float angleStep = 360f / _laserCount;
        for (int i = 0; i < _laserCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Gizmos.DrawLine(transform.position, transform.position + direction * _laserLength);
        }
    }
}
