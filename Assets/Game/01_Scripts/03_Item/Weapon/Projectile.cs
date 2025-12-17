using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ==========================================
// 4. 총알 (Projectile)
// ==========================================

public class Projectile : MonoBehaviour
{
    private int _damage;
    private float _speed;
    private float _maxRange;
    private Vector3 _startPos;

    public void Setup(int damage, float speed, float range)
    {
        _damage = damage;
        _speed = speed;
        _maxRange = range;
        _startPos = transform.position;
        Destroy(gameObject, 5f); // 안전장치
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);

        if (Vector3.Distance(_startPos, transform.position) >= _maxRange)
        {
            Destroy(gameObject); // 사거리 도달 시 삭제
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log($"적중! 데미지: {_damage}");
            // other.GetComponent<IDamageable>()?.TakeDamage(_damage);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}