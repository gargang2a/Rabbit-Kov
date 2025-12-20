# 🔫 Weapon 시스템 문서 (초보자용)

> **이 문서의 목표**: Weapon 시스템을 이해하고 다른 시스템과 연동하는 방법을 배웁니다
> **대상**: Unity 1개월차 개발자

---

## 📁 파일 한눈에 보기

```
01_Scripts/03_Item/
│
├── 📂 Data/                       ← 무기/아이템 설정 (ScriptableObject)
│   ├── 🔵 ItemData.cs             ← 모든 아이템의 기반
│   ├── 🟢 WeaponData.cs           ← 무기 기본 데이터 (추상)
│   ├── 🟢 RangedWeaponData.cs     ← 원거리 무기 데이터
│   ├── 🟢 MeleeWeaponData.cs      ← 근접 무기 데이터
│   ├── 🟢 ThrowableWeaponData.cs  ← 투척 무기 데이터
│   └── 🔵 ConsumableData.cs       ← 소모품 데이터
│
├── 📂 Weapon/                     ← 무기 동작 코드 (7개)
│   ├── 🟢 Weapon.cs               ← 무기 기본 클래스 (추상)
│   ├── 🟢 RangedWeapon.cs         ← 총 (발사체 발사)
│   ├── 🟢 MeleeWeapon.cs          ← 검 (히트박스 활성화)
│   ├── 🟢 Projectile.cs           ← 총알 (날아가서 hit)
│   ├── 🟢 ThrowableWeapon.cs      ← 수류탄 던지기
│   └── 🟢 GrenadeProjectile.cs    ← 수류탄 폭발
│
├── 📂 Field Item/                 ← 필드 아이템 (4개)
│   ├── ExpOrb.cs, HealthOrb.cs, StaminaOrb.cs, MagnetItem.cs
│
├── 🟢 ItemPickup.cs               ← 아이템 줍기
├── 🔵 ItemDropper.cs              ← 아이템 드롭
└── 🔵 AccessoryPickup.cs          ← 장신구 획득

🟢 = 핵심 파일 (22개 스크립트)
🔵 = 부가 파일
```

---

## 🔗 시스템 연결도

```mermaid
graph TB
    subgraph "Weapon 시스템"
        WD[WeaponData\n설계도]
        W[Weapon\n추상 클래스]
        RW[RangedWeapon\n총]
        MW[MeleeWeapon\n검]
        TW[ThrowableWeapon\n수류탄]
        PROJ[Projectile\n총알]
        GP[GrenadeProjectile\n폭발]
    end

    subgraph "외부 시스템"
        PWC[PlayerWeaponController\n무기 장착]
        ES[EnemyStats\nIDamageable]
        P[Player\nIDamageable]
    end

    WD -->|Initialize| W
    W --> RW
    W --> MW
    W --> TW

    RW -->|Fire| PROJ
    TW -->|Throw| GP
    PROJ -->|TakeDamage| ES
    PROJ -->|TakeDamage| P
    MW -->|TakeDamage| ES
    GP -->|Explode| ES

    PWC -->|EquipWeapon| WD

    style RW fill:#2196F3,color:#fff
    style MW fill:#FF9800,color:#fff
    style TW fill:#4CAF50,color:#fff
    style PROJ fill:#E91E63,color:#fff
```

### 연결 요약표

| 나는                      | 하고 싶은 것     | 어떻게?                                          |
| ------------------------- | ---------------- | ------------------------------------------------ |
| **Player/Inventory 담당** | 무기 장착        | `playerWeaponController.EquipWeapon(weaponData)` |
| **Enemy 담당**            | 무기로 피격 받기 | `IDamageable` 구현만 하면 자동!                  |
| **UI 담당**               | 탄알 수 표시     | `rangedWeapon.CurrentAmmo`                       |
| **새 무기 추가**          | 새로운 총 만들기 | WeaponData 생성 → 프리팹 만들기                  |

---

## 📊 Mermaid 다이어그램

### 클래스 상속 다이어그램

```mermaid
classDiagram
    class ItemData {
        <<abstract>>
        +string itemName
        +Sprite icon
        +string description
    }

    class WeaponData {
        <<abstract>>
        +GameObject weaponPrefab
        +int damage
        +float coolTime
    }

    class RangedWeaponData {
        +int maxAmmo
        +float reloadTime
        +float maxRange
        +float bulletSpeed
        +GameObject bulletPrefab
        +GameObject casingPrefab
    }

    class MeleeWeaponData {
        +float attackRange
        +float knockbackForce
    }

    class Weapon {
        <<abstract>>
        #WeaponData _baseData
        #bool _isReady
        +bool IsReady
        +Initialize(WeaponData, Transform)
        +Use()*
        +Reload()
    }

    class RangedWeapon {
        -int _currentAmmo
        -bool _isReloading
        +bool HasAmmo
        +int CurrentAmmo
        +Fire()
        +Reload()
    }

    class MeleeWeapon {
        -Collider _hitBox
        -bool _isAttacking
        +AttackRoutine()
    }

    class Projectile {
        -int _damage
        -float _speed
        -float _maxRange
        +Setup(int, float, float)
    }

    class ThrowableWeaponData {
        +float explosionRadius
        +float explosionDelay
        +float throwForce
        +GameObject explosionEffect
    }

    class ThrowableWeapon {
        -ThrowableWeaponData _throwableData
        +ThrowGrenade()
    }

    class GrenadeProjectile {
        -int _damage
        -float _explosionRadius
        +Setup(ThrowableWeaponData, Collider)
        +Explode()
    }

    ItemData <|-- WeaponData
    WeaponData <|-- RangedWeaponData
    WeaponData <|-- MeleeWeaponData
    WeaponData <|-- ThrowableWeaponData
    Weapon <|-- RangedWeapon
    Weapon <|-- MeleeWeapon
    Weapon <|-- ThrowableWeapon
    RangedWeapon ..> Projectile : 생성
    ThrowableWeapon ..> GrenadeProjectile : 생성
```

### 스크립트 연동 다이어그램

```mermaid
graph TB
    subgraph "Player GameObject"
        PWC[PlayerWeaponController]
    end

    subgraph "Weapon 시스템"
        WD[WeaponData<br/>ScriptableObject]
        W[Weapon<br/>추상 클래스]
        RW[RangedWeapon]
        MW[MeleeWeapon]
        PROJ[Projectile]
    end

    subgraph "대상"
        ES[EnemyStats<br/>IDamageable]
        P[Player<br/>IDamageable]
    end

    PWC -->|EquipWeapon| WD
    PWC -->|Instantiate| W
    W -->|Initialize| WD

    RW -->|Fire| PROJ
    PROJ -->|OnTriggerEnter| ES
    PROJ -->|OnTriggerEnter| P

    MW -->|OnTriggerEnter| ES
    MW -->|OnTriggerEnter| P

    style PWC fill:#4CAF50,color:#fff
    style RW fill:#2196F3,color:#fff
    style MW fill:#FF9800,color:#fff
    style PROJ fill:#E91E63,color:#fff
```

### 발사 플로우차트 (RangedWeapon)

```mermaid
flowchart TD
    START[Use 호출] --> A{IsReady?}
    A -->|No| END[종료]
    A -->|Yes| B{IsReloading?}
    B -->|Yes| END
    B -->|No| C{CurrentAmmo > 0?}
    C -->|No| D[빈 탄창 사운드]
    D --> E[자동 재장전]
    E --> END
    C -->|Yes| F[Fire 실행]
    F --> G[탄알 감소]
    G --> H[Projectile 생성]
    H --> I[Setup 초기화]
    I --> J[MuzzleFlash 재생]
    J --> K[발사 사운드]
    K --> L[CooldownRoutine]
    L --> END
```

### 발사체 라이프사이클

```mermaid
sequenceDiagram
    participant RW as RangedWeapon
    participant P as Projectile
    participant E as Enemy/Player
    participant ID as IDamageable

    RW->>P: Instantiate(bulletPrefab)
    RW->>P: Setup(damage, speed, range)
    P->>P: Destroy(5f) 안전장치 등록

    loop 매 프레임
        P->>P: Translate(forward * speed)
        P->>P: 거리 체크
        alt 사거리 초과
            P->>P: Destroy()
        end
    end

    P->>E: OnTriggerEnter(Collider)
    P->>ID: GetComponent<IDamageable>()
    alt target != null
        P->>ID: TakeDamage(damage, hitPoint, direction)
    end
    P->>P: Destroy()
```

### 근접 무기 공격 타임라인

```mermaid
gantt
    title MeleeWeapon AttackRoutine 타임라인
    dateFormat X
    axisFormat %L ms

    section 공격 사이클
    선딜 대기           :a1, 0, 100
    히트박스 ON         :active, a2, 100, 200
    공격 판정           :a3, 100, 300
    히트박스 OFF        :a4, 300, 300
    후딜 쿨타임         :a5, 300, 500
    다시 Ready          :milestone, a6, 500, 0
```

### 무기 장착 시퀀스

```mermaid
sequenceDiagram
    participant INV as Inventory
    participant PWC as PlayerWeaponController
    participant OLD as 기존 Weapon
    participant NEW as 새 Weapon
    participant WD as WeaponData

    INV->>PWC: EquipWeapon(weaponData)

    alt _isSwapping == true
        PWC-->>INV: return (무시)
    end

    PWC->>PWC: _isSwapping = true
    PWC->>OLD: Destroy()
    PWC->>WD: weaponPrefab 참조
    PWC->>NEW: Instantiate(prefab)
    PWC->>NEW: SetParent(weaponHolder)
    PWC->>NEW: Initialize(weaponData)
    NEW->>NEW: _baseData = data
    NEW->>NEW: _isReady = true
    PWC->>PWC: _currentWeaponInstance = NEW
    PWC->>PWC: _isSwapping = false
```

---

## 🏗️ 클래스 상속 구조 (쉽게 이해하기)

### 비유: 레고 블록처럼 쌓아 올리기

```
                    ┌─────────────┐
                    │  ItemData   │   ← 모든 아이템 (이름, 아이콘)
                    │  [추상]     │
                    └──────┬──────┘
                           │
                    ┌──────┴──────┐
                    │ WeaponData  │   ← 무기 전용 (데미지, 쿨타임)
                    │   [추상]    │
                    └──────┬──────┘
                           │
          ┌────────────────┼────────────────┐
          ▼                                  ▼
┌─────────────────┐              ┌─────────────────┐
│ RangedWeaponData│              │ MeleeWeaponData │
│   (총 데이터)    │              │   (검 데이터)   │
│                 │              │                 │
│ + 탄약          │              │ + 공격 범위     │
│ + 재장전 시간   │              │ + 넉백 강도     │
│ + 탄속          │              │                 │
└─────────────────┘              └─────────────────┘
```

**같은 구조로 동작 클래스도**:

```
Weapon (추상) → RangedWeapon, MeleeWeapon
```

---

## 📜 Weapon.cs 완전 분석 (추상 클래스)

### 추상 클래스가 뭐예요?

**비유**: 추상 클래스는 **설계도**입니다.

- 직접 만들 수는 없지만
- "모든 무기는 이런 식으로 만들어야 해" 라고 규칙을 정함

```csharp
public abstract class Weapon : MonoBehaviour
//      ↑
//      "나를 직접 쓰지 마. 상속받아서 써!"
```

### 코드 전체 분석

```csharp
public abstract class Weapon : MonoBehaviour
{
    // ═══════════════════════════════════════════════
    // 변수 (자식 클래스에서 사용)
    // ═══════════════════════════════════════════════

    protected WeaponData _baseData;  // 무기 데이터 (데미지, 쿨타임 등)
    protected bool _isReady = true;  // 공격 가능 상태

    // ═══════════════════════════════════════════════
    // 프로퍼티 (외부에서 읽기 전용)
    // ═══════════════════════════════════════════════

    public bool IsReady => _isReady;       // 쿨타임 끝났나?
    public WeaponData BaseData => _baseData; // 데이터 접근

    // ═══════════════════════════════════════════════
    // 초기화 (PlayerWeaponController에서 호출)
    // ═══════════════════════════════════════════════

    public virtual void Initialize(WeaponData data, Transform ownerFirePoint = null)
    {
        _baseData = data;
    }

    // ═══════════════════════════════════════════════
    // 공격 (자식 클래스에서 반드시 구현)
    // ═══════════════════════════════════════════════

    public abstract void Use();  // ← "나를 상속받는 애들아, 이거 만들어!"

    // ═══════════════════════════════════════════════
    // 재장전 (필요한 무기만 오버라이드)
    // ═══════════════════════════════════════════════

    public virtual void Reload() { }  // 기본은 아무것도 안 함
}
```

### 함수 목록 (전체)

|     접근자      | 함수명                              | 설명                      |
| :-------------: | ----------------------------------- | ------------------------- |
|     public      | `IsReady` (프로퍼티)                | 공격 가능 상태 확인       |
|     public      | `BaseData` (프로퍼티)               | 무기 데이터 접근          |
| public virtual  | `Initialize(WeaponData, Transform)` | 무기 초기화               |
| public abstract | `Use()`                             | 공격 실행 (자식에서 구현) |
| public virtual  | `Reload()`                          | 재장전 (기본은 빈 함수)   |

---

## 📜 RangedWeapon.cs 완전 분석 (원거리 무기)

### 이 스크립트의 역할

> 총알을 발사하고, 탄약과 재장전을 관리합니다.

### Inspector에서 설정할 것들

```csharp
[Header("Points - 위치 설정")]
[SerializeField] private Transform _firePoint;      // 총구 (총알 나가는 곳)
[SerializeField] private Transform _ejectionPort;   // 탄피 배출구

[Header("Visual & Audio - 이펙트/사운드")]
[SerializeField] private ParticleSystem _muzzleFlash; // 총구 화염
[SerializeField] private AudioSource _audioSource;    // 오디오
[SerializeField] private AudioClip _fireClip;         // 발사음
[SerializeField] private AudioClip _reloadClip;       // 재장전음
[SerializeField] private AudioClip _emptyClip;        // 탄창 비었을 때
```

### 핵심 프로퍼티 (UI에서 사용)

```csharp
public bool HasAmmo => _currentAmmo > 0;    // 탄알 있나?
public int CurrentAmmo => _currentAmmo;      // 현재 탄알
public int MaxAmmo => _gunData.maxAmmo;     // 최대 탄알
public bool IsReloading => _isReloading;     // 재장전 중?
```

**UI 담당자 사용 예시**:

```csharp
// AmmoUI.cs
void Update()
{
    RangedWeapon weapon = player.GetComponent<PlayerWeaponController>()
                                .CurrentWeapon as RangedWeapon;
    if (weapon != null)
    {
        ammoText.text = $"{weapon.CurrentAmmo} / {weapon.MaxAmmo}";
    }
}
```

### 함수 목록 (전체)

|     접근자      | 함수명                              | 설명               |
| :-------------: | ----------------------------------- | ------------------ |
|     public      | `HasAmmo` (프로퍼티)                | 탄알 있는지 확인   |
|     public      | `CurrentAmmo` (프로퍼티)            | 현재 탄알 수       |
|     public      | `MaxAmmo` (프로퍼티)                | 최대 탄알 수       |
|     public      | `IsReloading` (프로퍼티)            | 재장전 중인지 확인 |
|     public      | `myMuzzlePoint` (프로퍼티)          | 총구 Transform     |
|     private     | `OnEnable()`                        | 상태 초기화        |
| public override | `Initialize(WeaponData, Transform)` | 무기 데이터 설정   |
| public override | `Use()`                             | 발사 또는 재장전   |
| public override | `Reload()`                          | 수동 재장전 시작   |
|     private     | `Fire()`                            | 총알 생성 + 이펙트 |
|     private     | `PlaySoundWithRandomPitch(...)`     | 랜덤 피치 사운드   |
|     private     | `CoolTimeRoutine()`                 | 쿨타임 코루틴      |
|     private     | `ReloadRoutine()`                   | 재장전 코루틴      |
|     public      | `UpgradeMagazine(int)`              | 탄창 확장          |
|     public      | `UpgradeGrip(float)`                | 탄퍼짐 감소        |

### Use() 함수 분석

```csharp
public override void Use()
{
    // 1. 쿨타임 or 재장전 중이면 리턴
    if (!_isReady || _isReloading) return;

    // 2. 탄알 있으면 발사
    if (_currentAmmo > 0)
    {
        Fire();
    }
    // 3. 탄알 없으면 빈 탄창 사운드 + 자동 재장전
    else
    {
        _audioSource.PlayOneShot(_emptyClip);
        StartCoroutine(ReloadRoutine());
    }
}
```

### Fire() 함수 분석

```csharp
private void Fire()
{
    // 1. 탄알 소모
    _currentAmmo--;

    // 2. 총알 생성
    GameObject bullet = Instantiate(
        _gunData.bulletPrefab,  // 총알 프리팹
        _firePoint.position,     // 총구 위치
        _firePoint.rotation      // 총구 방향
    );

    // 3. 총알 초기화
    Projectile proj = bullet.GetComponent<Projectile>();
    proj.Setup(
        _baseData.damage,       // 데미지
        _gunData.bulletSpeed,   // 탄속
        _gunData.maxRange       // 사거리
    );

    // 4. 이펙트 & 사운드
    _muzzleFlash?.Play();
    _audioSource.PlayOneShot(_fireClip);

    // 5. 쿨타임 시작
    StartCoroutine(CooldownRoutine());
}
```

---

## 📜 MeleeWeapon.cs 완전 분석 (근접 무기)

### 이 스크립트의 역할

> 히트박스를 활성화해서 범위 내 적에게 데미지를 줍니다.

### 히트박스가 뭐예요?

**비유**: 히트박스는 **투명한 칼날**입니다.

- 평소에는 꺼져있다가 (안 맞음)
- 휘두를 때만 켜짐 (맞음!)

```
휘두르기 전:    휘두르는 중:

   ╭──╮           ╭──╮
   │검│           │검│═══════╗  ← 히트박스 ON
   ╰──╯           ╰──╯       ║
                              ║
                        (이 영역에 닿으면 데미지)
```

### 함수 목록 (전체)

|     접근자      | 함수명                              | 설명                   |
| :-------------: | ----------------------------------- | ---------------------- |
| public override | `Initialize(WeaponData, Transform)` | 근접 무기 초기화       |
| public override | `Use()`                             | 공격 코루틴 시작       |
|     private     | `AttackRoutine()`                   | 히트박스 활성화 코루틴 |
|     private     | `OnTriggerEnter(Collider)`          | 적 타격 시 데미지 처리 |
|     private     | `PlaySoundWithRandomPitch(...)`     | 랜덤 피치 사운드       |
|     private     | `IsEnemy(GameObject)`               | 태그/레이어로 적 판정  |

### AttackRoutine 분석

```csharp
private IEnumerator AttackRoutine()
{
    _isAttacking = true;
    _isReady = false;

    // ───────────────────────────────────
    // 1단계: 선딜 (공격 준비 모션)
    // ───────────────────────────────────
    yield return new WaitForSeconds(0.1f);

    // ───────────────────────────────────
    // 2단계: 히트박스 켜기 (데미지 판정 시작)
    // ───────────────────────────────────
    _hitBox.enabled = true;

    // ───────────────────────────────────
    // 3단계: 공격 지속 (0.2초간 판정)
    // ───────────────────────────────────
    yield return new WaitForSeconds(0.2f);

    // ───────────────────────────────────
    // 4단계: 히트박스 끄기 (데미지 판정 종료)
    // ───────────────────────────────────
    _hitBox.enabled = false;

    // ───────────────────────────────────
    // 5단계: 후딜 (쿨타임)
    // ───────────────────────────────────
    float waitTime = _baseData.coolTime - 0.3f;
    if (waitTime > 0) yield return new WaitForSeconds(waitTime);

    _isAttacking = false;
    _isReady = true;  // 다시 공격 가능
}
```

### OnTriggerEnter (충돌 감지)

```csharp
private void OnTriggerEnter(Collider other)
{
    // 공격 중이고, 적 태그면
    if (_isAttacking && other.CompareTag("Enemy"))
    {
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            Vector3 dir = (other.transform.position - transform.position).normalized;
            target.TakeDamage(_baseData.damage, other.transform.position, dir);
        }
    }
}
```

---

## 📜 Projectile.cs 완전 분석

### 이 스크립트의 역할

> 총알이 날아가다가 적에 맞으면 **데미지 + 넉백 + 히트 이펙트**를 적용합니다.

### 핵심 변수

```csharp
private int _damage;        // 데미지
private float _speed;       // 이동 속도
private float _maxRange;    // 최대 사거리
private float _knockback;   // ★ 넉백 강도 (신규)
private GameObject _hitEffectPrefab;  // ★ 히트 이펙트 (신규)
```

### 코드 분석

```csharp
public class Projectile : MonoBehaviour
{
    // ═══════════════════════════════════════════════
    // 초기화 (RangedWeapon.Fire()에서 호출)
    // ═══════════════════════════════════════════════
    public void Setup(int damage, float speed, float range, float knockback = 0f)
    {
        _damage = damage;
        _speed = speed;
        _maxRange = range;
        _knockback = knockback;  // ★ 넉백 강도 저장
        _startPos = transform.position;

        Destroy(gameObject, 5f);  // 안전장치
    }

    // ★ 히트 이펙트 설정 (RangedWeapon에서 호출)
    public void SetHitEffect(GameObject hitEffectPrefab)
    {
        _hitEffectPrefab = hitEffectPrefab;
    }

    // ═══════════════════════════════════════════════
    // 적중 처리 (넉백 + 히트 이펙트 포함)
    // ═══════════════════════════════════════════════
    void OnTriggerEnter(Collider other)
    {
        if (IsEnemy(other.gameObject))
        {
            // 1. 정확한 피격 위치 계산
            Vector3 hitPoint = GetExactHitPoint(other);
            Vector3 hitNormal = GetHitNormal(other, hitPoint);

            // 2. 히트 이펙트 생성
            SpawnHitEffect(hitPoint, hitNormal);

            // 3. 데미지 + 넉백 적용
            IDamageable target = other.GetComponent<IDamageable>();
            target?.TakeDamage(_damage, hitPoint, transform.forward, _knockback);

            Destroy(gameObject);
        }
    }

    // 히트 이펙트 생성
    private void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        if (_hitEffectPrefab == null) return;

        Quaternion rotation = Quaternion.LookRotation(normal);
        GameObject effect = Instantiate(_hitEffectPrefab, position, rotation);
        Destroy(effect, 2f);
    }
}
```

### 넉백 + 히트 이펙트 흐름

```mermaid
flowchart LR
    A[RangedWeapon.Fire] -->|Setup + SetHitEffect| B[Projectile]
    B --> C[OnTriggerEnter]
    C --> D[GetExactHitPoint]
    D --> E[SpawnHitEffect]
    E --> F[TakeDamage + knockback]
```

---

## 🛠️ 새 무기 만들기 (단계별 가이드)

### Step 1: WeaponData 만들기 (ScriptableObject)

```
1. Project 창에서 우클릭
2. Create → Duckov → Item → Weapon → Ranged (또는 Melee)
3. 이름 지정 (예: "AK47_Data")
4. Inspector에서 설정:
```

| 필드           | 설명                     | 예시 값  |
| -------------- | ------------------------ | -------- |
| `damage`       | 총알 하나당 데미지       | 25       |
| `coolTime`     | 연사 간격 (초)           | 0.1      |
| `maxAmmo`      | 탄창 용량                | 30       |
| `reloadTime`   | 재장전 시간              | 2.5      |
| `bulletSpeed`  | 탄속                     | 50       |
| `maxRange`     | 사거리                   | 100      |
| `weaponPrefab` | 무기 프리팹 (손에 들 것) | (드래그) |
| `bulletPrefab` | 총알 프리팹              | (드래그) |

### Step 2: 무기 프리팹 만들기

```
1. 무기 모델(메쉬) 임포트
2. 빈 GameObject 생성 → 이름: "AK47_Prefab"
3. 모델을 자식으로 넣기
4. RangedWeapon 컴포넌트 추가
5. Inspector 설정:
   - Fire Point: 총구 위치 만들어서 연결
   - Muzzle Flash: 이펙트 연결
   - Audio Source: 추가하고 연결
```

### Step 3: 총알 프리팹 만들기

```
1. 작은 Sphere 또는 Capsule 생성
2. Rigidbody 추가:
   - Is Kinematic: ✅ (체크)
   - Use Gravity: ❌ (해제)
3. Collider 설정:
   - Is Trigger: ✅ (체크)
4. Projectile 스크립트 추가
5. Prefabs 폴더에 드래그해서 저장
```

### Step 4: 테스트

```csharp
// 테스트용 코드
public WeaponData testWeapon;  // Inspector에서 연결

void Start()
{
    PlayerWeaponController pwc = FindObjectOfType<PlayerWeaponController>();
    pwc.EquipWeapon(testWeapon);
}
```

---

## 💣 투척 무기 시스템 (신규)

### 이 시스템의 역할

> **수류탄/화염병** 등 던져서 폭발하는 무기를 관리합니다.

### 클래스 구조

```
Weapon
   │
   ├── RangedWeapon     ← 총 (총알 발사)
   ├── MeleeWeapon      ← 검 (히트박스)
   └── ThrowableWeapon  ← 수류탄 (투척 → 폭발) ★ 신규
```

### ThrowableWeaponData (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "Duckov/Item/Weapon/Throwable")]
public class ThrowableWeaponData : WeaponData
{
    [Header("Explosion Stats")]
    public float explosionRadius = 5f;  // 폭발 반경
    public float explosionDelay = 3f;   // 폭발 지연 시간
    public float explosionForce = 700f; // 물리력

    [Header("Throw Stats")]
    public float throwForce = 15f;      // 던지는 힘
    public float throwUpwardForce = 2f; // 위로 던지는 힘 (포물선)

    [Header("Effects")]
    public GameObject explosionEffect;  // 폭발 이펙트
    public AudioClip explosionSound;    // 폭발 소리
}
```

### ThrowableWeapon.cs 핵심 로직

```mermaid
sequenceDiagram
    participant P as Player
    participant TW as ThrowableWeapon
    participant GP as GrenadeProjectile
    participant E as Enemy

    P->>TW: Use()
    TW->>TW: ThrowGrenade()
    TW->>GP: Instantiate(prefab)
    TW->>GP: Setup(data, playerCollider)
    GP->>GP: StartCoroutine(ExplodeRoutine)

    note over GP: explosionDelay 초 대기

    GP->>GP: Explode()
    GP->>E: Physics.OverlapSphere
    GP->>E: TakeDamage + 넝백
    GP->>GP: Destroy()
```

### GrenadeProjectile.cs 폭발 로직

```csharp
private void Explode()
{
    if (_hasExploded) return;
    _hasExploded = true;

    // 1. 이펙트 생성
    Instantiate(_explosionEffect, transform.position, Quaternion.identity);

    // 2. 폭발 반경 내 적 찾기
    Collider[] hits = Physics.OverlapSphere(transform.position, _explosionRadius);
    foreach (var hit in hits)
    {
        // 데미지
        if (hit.TryGetComponent(out IDamageable target))
        {
            target.TakeDamage(_damage, hit.transform.position, Vector3.zero);
        }

        // 물리 넝백
        Rigidbody rb = hit.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddExplosionForce(_explosionForce, transform.position, _explosionRadius);
        }
    }

    Destroy(gameObject);
}
```

### 투척 무기 만들기 가이드

| 단계 | 설명                                            |
| ---- | ----------------------------------------------- |
| 1    | `ThrowableWeaponData` ScriptableObject 생성     |
| 2    | 수류탄 모델 프리팫 생성 (Rigidbody + Collider)  |
| 3    | `GrenadeProjectile` 스크립트 붙이기             |
| 4    | `ThrowableWeapon` 스크립트로 들려있는 무기 생성 |
| 5    | weaponPrefab에 수류탄 연결                      |

---

## ❓ 자주 묻는 질문

### Q: `abstract`가 뭐예요?

**A**: "이 클래스 자체로는 못 쓰고, 상속받아서 써야 해" 라는 뜻입니다.

```csharp
// ❌ 안 됨
Weapon w = new Weapon();

// ✅ 됨 (상속받은 클래스)
RangedWeapon gun = new RangedWeapon();
```

### Q: `virtual`과 `override`가 뭐예요?

**A**: 부모의 함수를 자식이 **다시 정의**할 수 있게 해줍니다.

```csharp
// 부모 클래스
public virtual void Reload() { }  // "자식이 바꿔도 돼"

// 자식 클래스
public override void Reload()     // "내 방식대로 할게"
{
    StartCoroutine(ReloadRoutine());
}
```

### Q: Coroutine이 뭐예요?

**A**: 시간을 두고 천천히 실행되는 함수입니다.

```csharp
// 일반 함수: 한 번에 쭉 실행됨
void NomalFunction()
{
    DoA();
    DoB();  // DoA 직후 바로 실행
}

// 코루틴: 기다릴 수 있음
IEnumerator MyCoroutine()
{
    DoA();
    yield return new WaitForSeconds(1f);  // 1초 대기
    DoB();  // 1초 후 실행
}

// 코루틴 시작 방법
StartCoroutine(MyCoroutine());
```

### Q: 총알이 안 나가요

**A**: 체크리스트:

1. ✅ `bulletPrefab`이 WeaponData에 연결됐나요?
2. ✅ `_firePoint` Transform이 연결됐나요?
3. ✅ Projectile 스크립트가 총알 프리팹에 있나요?
4. ✅ 총알 Collider의 `Is Trigger`가 켜져있나요?

---

> 📖 이전 문서: [PLAYER_SYSTEM.md](./PLAYER_SYSTEM.md), [ENEMY_SYSTEM.md](./ENEMY_SYSTEM.md)
