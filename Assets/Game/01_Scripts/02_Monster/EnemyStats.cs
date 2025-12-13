using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// EnemyStats - 적 스탯(체력) 관리 시스템
// ============================================================================
// 
// [역할]
// 적의 체력(HP)을 관리하는 컴포넌트입니다.
// 데미지 처리, 힐링, 사망 판정 및 관련 이벤트를 담당합니다.
// 
// [이벤트 시스템]
// - OnHealthChanged: 체력이 변동될 때마다 발생 (UI 업데이트용)
// - OnDeath: 적이 사망했을 때 발생 (EnemyController에서 구독)
// 
// [사용 예시]
// - EnemyController에서 OnDeath 이벤트를 구독하여 사망 처리
// - 체력바 UI에서 OnHealthChanged 이벤트를 구독하여 게이지 업데이트
// 
// [확장 가능성]
// - TakeDamage, Heal은 virtual로 선언되어 상속 클래스에서 오버라이드 가능
// - 예: 보스 몬스터가 특정 체력 이하일 때 패턴 변경
// ============================================================================
public class EnemyStats : MonoBehaviour
{
    // ==================== 체력 설정 ====================
    
    // 최대 체력. 인스펙터에서 적 종류별로 다르게 설정할 수 있습니다.
    // 게임 시작 시 _currentHealth가 이 값으로 초기화됩니다.
    [SerializeField] private float _maxHealth = 100f;
    
    // 현재 체력. 런타임에서 데미지/힐링에 따라 변동됩니다.
    // private이므로 외부에서는 CurrentHealth 프로퍼티로만 읽기 가능합니다.
    private float _currentHealth;

    // ==================== 이벤트 ====================
    // 이벤트(Event): 특정 상황 발생 시 다른 객체에게 알리는 메커니즘
    // Action: 반환값 없는 델리게이트(함수 포인터)의 간편 표현
    
    // 체력 변동 이벤트. 체력이 증가하거나 감소할 때 발생합니다.
    // 용도: 체력바 UI 업데이트, 피격 이펙트 재생 등
    public event Action OnHealthChanged;
    
    // 사망 이벤트. 체력이 0 이하가 되어 사망했을 때 한 번 발생합니다.
    // 용도: 사망 애니메이션, AI 정지, 아이템 드롭, 경험치 지급 등
    public event Action OnDeath;

    // ==================== 프로퍼티 ====================
    
    // 최대 체력을 외부에서 읽을 수 있게 하는 프로퍼티
    // 용도: 체력바 UI에서 게이지 비율 계산 (현재/최대)
    public float MaxHealth { get { return _maxHealth; } }
    
    // 현재 체력을 외부에서 읽을 수 있게 하는 프로퍼티
    // 용도: 체력바 UI 표시, 상태 판단
    public float CurrentHealth { get { return _currentHealth; } }
    
    // 사망 여부를 반환하는 프로퍼티
    // 체력이 0 이하면 true를 반환합니다.
    // 용도: EnemyController.Update()에서 AI 로직 실행 여부 판단
    public bool isDead
    {
        get
        {
            return _currentHealth <= 0;
        }
    }

    // ==================== MonoBehaviour 생명주기 ====================
    
    // Awake: 게임 시작 시 체력 초기화
    private void Awake()
    {
        // 현재 체력을 최대 체력으로 설정
        _currentHealth = _maxHealth;
    }

    // ==================== 공개 메서드 ====================
    
    // 데미지를 받는 함수. 외부에서 적에게 피해를 입힐 때 호출합니다.
    // amt: 입힐 데미지 양 (양수)
    // virtual: 상속 클래스에서 오버라이드 가능 (예: 보스의 특수 피격 처리)
    // 
    // [호출 예시]
    // enemy.GetComponent<EnemyStats>().TakeDamage(25f);
    public virtual void TakeDamage(float amt)
    {
        // 이미 죽었으면 추가 데미지 무시
        if (isDead == true) return;
        
        // 음수 데미지 방지 (음수면 회복이 되어버림)
        // 클리핑(Clipping): 값이 특정 범위를 벗어나지 않도록 제한
        if (amt < 0) amt = 0;

        // 체력 감소
        _currentHealth -= amt;
        
        // 체력이 0 미만으로 떨어지지 않도록 클리핑
        if (_currentHealth < 0) _currentHealth = 0;

        // 체력 변동 이벤트 발생
        // null 체크: 이벤트를 구독한 객체가 없으면 호출하지 않음
        if (OnHealthChanged != null) OnHealthChanged();

        // 사망 체크: 체력이 0이 되면 사망 이벤트 발생
        if (isDead == true)
        {
            // OnDeath 이벤트가 구독되어 있으면 호출
            // EnemyController.HandleDeath()가 이 이벤트를 구독하고 있음
            if (OnDeath != null) OnDeath();
        }
    }

    // 체력을 회복하는 함수. 힐링 아이템이나 스킬 사용 시 호출합니다.
    // amt: 회복할 체력 양 (양수)
    // virtual: 상속 클래스에서 오버라이드 가능
    public virtual void Heal(float amt)
    {
        // 이미 죽었으면 회복 불가 (부활은 별도 함수로 처리)
        if (isDead == true) return;
        
        // 음수 회복 방지 (음수면 데미지가 되어버림)
        if (amt < 0) amt = 0;

        // 체력 증가
        _currentHealth += amt;
        
        // 최대 체력을 초과하지 않도록 클리핑
        if (_currentHealth > _maxHealth) _currentHealth = _maxHealth;

        // 체력 변동 이벤트 발생
        if (OnHealthChanged != null) OnHealthChanged();
    }

    // 체력을 최대로 리셋하는 함수. 리스폰 시 사용합니다.
    // 주의: 이 함수는 사망 상태에서도 호출 가능 (부활 기능)
    public void ResetHealth()
    {
        _currentHealth = _maxHealth;
        
        // 체력 변동 이벤트 발생 (UI 업데이트용)
        if (OnHealthChanged != null) OnHealthChanged();
    }
}
