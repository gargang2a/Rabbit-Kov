using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitKov.Enemy
{
    // 적의 체력을 관리하는 스크립트
    // 데미지를 받으면 체력이 깎이고, 0이 되면 사망 처리됨
    
    public class EnemyStats : MonoBehaviour
    {
        [Header("체력 설정")]
        // _maxHealth: 최대 체력
        // 왜 SerializeField? Inspector에서 적마다 다른 체력을 설정할 수 있게
        [SerializeField] private float _maxHealth = 100f;
        
        // _currentHealth: 현재 체력
        // 왜 private? 다른 스크립트에서 직접 체력을 바꾸면 이벤트가 발생 안 하니까
        // TakeDamage나 Heal 함수를 통해서만 변경되어야 함
        private float _currentHealth;

        // event: 특정 일이 발생했을 때 등록된 함수들을 호출하는 기능
        // Action: 매개변수 없이 호출되는 함수 타입 (System 네임스페이스에 있음)
        // 
        // 사용 예시:
        // 등록: enemy.Stats.OnDeath += 내함수;  ← OnDeath 발생 시 내함수도 호출됨
        // 해제: enemy.Stats.OnDeath -= 내함수;  ← 더 이상 호출 안 됨
        // 
        // 왜 이벤트를 쓰는가?
        // - EnemyStats는 죽었다는 것만 알림
        // - 사망 처리(애니메이션, 아이템 드롭 등)는 다른 스크립트가 알아서 함
        // - 스크립트 간 의존성이 줄어듦 (느슨한 결합)
        
        // OnHealthChanged: 체력이 변경될 때마다 호출됨
        // 체력바 UI 업데이트에 사용할 수 있음
        public event Action OnHealthChanged;
        
        // OnDeath: 사망할 때 호출됨
        // 사망 애니메이션, 아이템 드롭 등에 사용
        public event Action OnDeath;

        // 프로퍼티: 변수를 외부에서 읽을 수 있게 해줌
        // get만 있으면 읽기 전용
        public float MaxHealth { get { return _maxHealth; } }
        public float CurrentHealth { get { return _currentHealth; } }

        // isDead: 사망 여부를 확인하는 프로퍼티
        // 왜 함수가 아니라 프로퍼티? enemy.Stats.isDead 형태로 자연스럽게 읽히니까
        public bool isDead
        {
            get
            {
                if (_currentHealth <= 0) return true;
                return false;
            }
        }

        // HealthRatio: 체력 비율 (0.0 ~ 1.0)
        // 왜 필요? 체력바 UI에서 fillAmount에 바로 넣을 수 있음
        // 예: 체력 50/100 이면 0.5 반환
        public float HealthRatio
        {
            get
            {
                if (_maxHealth > 0) return _currentHealth / _maxHealth;
                return 0f;
            }
        }

        // Awake: 오브젝트가 생성될 때 호출됨
        private void Awake()
        {
            // 게임 시작 시 현재 체력을 최대 체력으로 설정
            _currentHealth = _maxHealth;
        }

        // TakeDamage: 데미지를 받는 함수
        // damageAmount: 받을 데미지 양
        // virtual: 자식 클래스에서 이 함수를 재정의(override) 할 수 있게 해줌
        // 왜 virtual? 보스몬스터는 데미지를 다르게 처리하고 싶을 수 있음
        public virtual void TakeDamage(float damageAmount)
        {
            // 이미 죽었으면 데미지 안 받음
            if (isDead == true) return;
            
            // 음수 데미지 방지
            // 왜? 음수면 오히려 체력이 회복되어 버림
            if (damageAmount < 0) damageAmount = 0;

            // 체력 감소
            _currentHealth = _currentHealth - damageAmount;
            
            // 체력이 0 미만이면 0으로 고정
            // 왜? -10 같은 값이 되면 UI에서 이상하게 보일 수 있음
            if (_currentHealth < 0) _currentHealth = 0;

            // 체력 변경 이벤트 발생
            // 조건 체크 이유: 아무도 구독 안 했는데 호출하면 에러남
            if (OnHealthChanged != null) OnHealthChanged();

            // 사망 체크
            if (isDead == true)
            {
                if (OnDeath != null) OnDeath();
            }
        }

        // Heal: 체력을 회복하는 함수
        public virtual void Heal(float healAmount)
        {
            if (isDead == true) return;
            if (healAmount < 0) healAmount = 0;

            _currentHealth = _currentHealth + healAmount;
            
            // 최대 체력 초과 방지
            if (_currentHealth > _maxHealth) _currentHealth = _maxHealth;

            if (OnHealthChanged != null) OnHealthChanged();
        }

        // ResetHealth: 체력을 완전히 회복
        // 리스폰할 때 사용
        public void ResetHealth()
        {
            _currentHealth = _maxHealth;
            if (OnHealthChanged != null) OnHealthChanged();
        }
    }
}