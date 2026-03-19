# SOLID Coding Standard for Unity C#

> AI 코딩 어시스턴트를 위한 Unity C# 코딩 표준 가이드라인  
> 이 문서는 코드 생성 시 반드시 준수해야 할 원칙을 정의합니다.

---

## 🚨 필수 준수 사항 (Mandatory Rules)

### 1️⃣ 만능 클래스 (God Object) 금지

**규칙:** 하나의 클래스가 2 개 이상의 책임을 가지면 안 됩니다.

**위반 코드 예시:**

```csharp
// ❌ 절대 작성 금지
public class Player : MonoBehaviour
{
    // 이동, 공격, 인벤토리, UI, 오디오, 저장... 모든 것을 처리
}

public class GameManager : MonoBehaviour
{
    // 게임 루프, UI, 적 스폰, 사운드, 네트워크... 모든 것을 처리
}
```

**적용 가이드라인:**

| 기준 | 제한 | 위반 시 액션 |
|------|------|-------------|
| 클래스 행 수 | 최대 300 행 | 기능별 클래스 분리 |
| 메서드 수 | 최대 10 개 | 하위 클래스로 분할 |
| 주어진 책임 | 단일 목적 | 컴포넌트 분리 |
| Unity 컴포넌트 | 단일 기능 | 시스템으로 분리 |

**올바른 접근:**

```csharp
// ✅ Player 는 조립만 담당
public class Player : MonoBehaviour
{
    [Inject] private PlayerMovement _movement;
    [Inject] private PlayerCombat _combat;
    [Inject] private PlayerInventory _inventory;
    
    // 각 시스템은 별도 클래스
}

// ✅ 각자 한 가지 일만 함
public class PlayerMovement : MonoBehaviour { /* 이동만 */ }
public class PlayerCombat : MonoBehaviour { /* 전투만 */ }
public class PlayerInventory : MonoBehaviour { /* 인벤토리만 */ }
```

---

### 2️⃣ If-Else / Switch 대신 추상화 (OCP 강력 적용)

**규칙:** 새로운 타입 추가 시 기존 코드를 수정해야 한다면 설계 오류입니다.

**위반 코드 예시:**

```csharp
// ❌ 절대 작성 금지 - OCP 위반
public void CreateEnemy(string type)
{
    switch (type)
    {
        case "Goblin": return Instantiate(goblinPrefab);
        case "Orc": return Instantiate(orcPrefab);
        case "Dragon": return Instantiate(dragonPrefab);
        // 새 적 추가 시 이 코드를 수정해야 함 = 위반
    }
}

// ❌ 절대 작성 금지 - 타입 체크
public void ProcessDamage(Character target)
{
    if (target is Player player) { /* 플레이어 처리 */ }
    else if (target is Enemy enemy) { /* 적 처리 */ }
    else if (target is Boss boss) { /* 보스 처리 */ }
    // 새 타입 추가 시 이 코드를 수정해야 함 = 위반
}
```

**올바른 접근:**

```csharp
// ✅ 인터페이스 + 다형성 사용
public interface IEnemy
{
    void Spawn(Vector3 position);
    void TakeDamage(float damage);
}

public class Goblin : MonoBehaviour, IEnemy { /* 구현 */ }
public class Orc : MonoBehaviour, IEnemy { /* 구현 */ }
public class Dragon : MonoBehaviour, IEnemy { /* 구현 */ }

// ✅ 팩토리 패턴 - 새 적 추가 시 기존 코드 수정 불필요
public interface IEnemyFactory
{
    IEnemy CreateEnemy();
}

public class EnemySpawner : MonoBehaviour
{
    [Inject] private Dictionary<string, IEnemyFactory> _factories;
    
    public IEnemy SpawnEnemy(string type)
    {
        // switch 없음 - 딕셔너리 조회만
        return _factories[type].CreateEnemy();
    }
}

// ✅ 전략 패턴 - 타입 체크 없음
public class DamageHandler : MonoBehaviour
{
    public void ProcessDamage(IDamageable target, float damage)
    {
        // 타입 관계없이 일관된 호출
        target.TakeDamage(damage);
    }
}
```

**판단 기준:**

| 상황 | 위반 | 올바른 해결 |
|------|------|-------------|
| 새 캐릭터 추가 | switch-case 수정 | 새 클래스 추가만 |
| 새 아이템 추가 | if-else 수정 | 인터페이스 구현만 |
| 새 스킬 추가 | 매니저 수정 | 스킬 클래스 추가만 |
| 타입 분기 필요 | `is`, `as` 사용 | 다형성 사용 |

---

### 3️⃣ Unity Inspector 와 DIP 조화

**규칙:** DIP 를 지키면서도 Unity Inspector 를 통해 기획자가 값을 수정할 수 있어야 합니다.

**해결책 1: Data Object 분리**

```csharp
// ✅ 인터페이스 + ScriptableObject 조합
public interface IWeapon
{
    string WeaponName { get; }
    void Attack(Character target);
}

// 데이터는 ScriptableObject 로 (Inspector 편집 가능)
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public float damage = 10f;
    public float attackSpeed = 1f;
    public GameObject projectilePrefab;
}

// 로직은 클래스로 (DI 주입)
public class Sword : MonoBehaviour, IWeapon
{
    [field: SerializeField] public string WeaponName { get; private set; }
    
    [Inject] private WeaponData _data;
    [Inject] private IEffectPool _effectPool;
    
    public void Attack(Character target)
    {
        // _data.damage 사용 (기획자가 Inspector 에서 수정)
        target.TakeDamage(_data.damage);
        _effectPool.Spawn("Slash", transform.position);
    }
}
```

**해결책 2: 설정 클래스 분리**

```csharp
// ✅ 설정 데이터는 직렬화, 로직은 인터페이스
[System.Serializable]
public class EnemySettings
{
    public float maxHealth = 100f;
    public float moveSpeed = 5f;
    public float damage = 20f;
    public GameObject deathEffect;
}

public interface IEnemy
{
    EnemySettings Settings { get; }
    void Spawn();
    void TakeDamage(float damage);
}

public class Goblin : MonoBehaviour, IEnemy
{
    [SerializeField] private EnemySettings _settings;
    public EnemySettings Settings => _settings;
    
    [Inject] private IEnemySpawner _spawner;
    
    // _settings 는 Inspector 에서 수정 가능
    // _spawner 는 DI 에서 주입
}
```

**해결책 3: Hybrid 주입**

```csharp
// ✅ MonoBehaviour 는 Inspector, 로직은 DI
public class Player : MonoBehaviour
{
    // Inspector: 기획자가 수정
    [Header("Stats")]
    [SerializeField] private float _baseHealth = 100f;
    [SerializeField] private float _moveSpeed = 5f;
    
    // DI: 코드에서 주입
    [Inject] private IInputManager _input;
    [Inject] private IAudioManager _audio;
    [Inject] private IEventBus _eventBus;
    
    private void Awake()
    {
        // Inspector 값과 DI 주입 모두 사용
    }
}
```

**해결책 4: ScriptableObject 를 DI 에 등록**

```csharp
// ✅ ScriptableObject 도 DI 로 주입
[CreateAssetMenu(fileName = "GameData", menuName = "Game/GameData")]
public class GameData : ScriptableObject
{
    public float gravity = -9.81f;
    public int maxPlayers = 4;
    public string gameTitle = "My Game";
}

public class GameInstaller : GameInstaller
{
    [SerializeField] private GameData _gameData;  // Inspector 에서 설정
    
    public override void Install(ServiceCollection services)
    {
        // ScriptableObject 도 DI 에 등록
        services.Add<GameData>().ToInstance(_gameData);
        
        services.Add<IPhysicsConfig>()
            .ToFactory(r => new PhysicsConfig(r.Resolve<GameData>().gravity));
    }
}

public class PhysicsService
{
    [Inject] private GameData _data;  // DI 로 주입받음
    
    public void ApplyGravity(Rigidbody rb)
    {
        rb.AddForce(Vector2.up * _data.gravity);
    }
}
```

---

## 📌 AI 코딩 전 필수 점검 (Mandatory Checklist)

**코드를 생성하기 전에 반드시 다음을 확인합니다:**

```
□ [SRP] 이 클래스가 2 개 이상의 책임을 가지는가?
   → Player 가 이동 + 공격 + 인벤토리를 모두 처리하는가?
   
□ [OCP] 새 타입 추가 시 switch/if 를 수정해야 하는가?
   → 새 캐릭터/아이템/스킬 추가 시 기존 코드를 고치는가?
   
□ [DIP] Unity Inspector 와 충돌하는가?
   → 기획자가 값을 수정할 수 있는가?
   
□ [LSP] 자식 클래스에서 예외 (throw) 가 발생하는가?
   → 부모 메서드를 오버라이드 시 예외를 던지는가?
   
□ [ISP] 인터페이스에 5 개 이상 메서드가 있는가?
   → 사용하지 않는 메서드를 구현해야 하는가?
```

**하나라도 "Yes" 면 설계를 다시 검토합니다.**

---

## 📌 원칙별 핵심 요약

### 1. SRP (Single Responsibility Principle) - 단일 책임 원칙

**핵심:** 클래스는 단 하나의 책임만 가져야 한다. **만능 클래스 금지.**

**판단 기준:**
- 클래스 설명에 "그리고"가 2 개 이상 들어가면 분리 신호
- 메서드 수가 10 개를 초과하면 분리 고려
- 파일 길이가 300 행을 넘으면 분리 **(기존 500 행 → 300 행으로 강화)**

**위반 신호 (절대 금지):**
```csharp
// ❌ Player 가 이것저것 모두 처리
public class Player : MonoBehaviour
{
    void HandleMovement() { }
    void HandleCombat() { }
    void HandleInventory() { }
    void HandleUI() { }
    void HandleAudio() { }
    void HandleSaveLoad() { }
}
```

**올바른 접근:**
```csharp
// ✅ Player 는 조립만, 각 시스템은 별도 클래스
public class Player : MonoBehaviour
{
    [Inject] private PlayerMovement _movement;  // 이동만
    [Inject] private PlayerCombat _combat;      // 전투만
    [Inject] private PlayerInventory _inventory; // 인벤토리만
    [Inject] private PlayerUI _ui;              // UI 표현만
}
```

#### ❌ Bad Example

```csharp
public class Player : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _health = 100f;
    [SerializeField] private float _speed = 5f;
    
    [Header("Combat")]
    [SerializeField] private Weapon _weapon;
    
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    
    [Header("UI")]
    [SerializeField] private HealthBar _healthBar;
    [SerializeField] private Text _scoreText;
    
    private void Update()
    {
        HandleMovement();
        HandleCombat();
        HandleUI();
        HandleAudio();
        HandleAnimation();
    }
    
    private void HandleMovement() { /* 50 lines */ }
    private void HandleCombat() { /* 50 lines */ }
    private void HandleUI() { /* 30 lines */ }
    private void HandleAudio() { /* 20 lines */ }
    private void HandleAnimation() { /* 30 lines */ }
    
    public void TakeDamage(float damage) { /* 20 lines */ }
    public void Heal(float amount) { /* 15 lines */ }
    public void AddScore(int score) { /* 10 lines */ }
    public void PlaySound(string soundName) { /* 10 lines */ }
    public void SaveData() { /* 25 lines */ }
    public void LoadData() { /* 25 lines */ }
}
// 총 400+ 행 - 너무 많은 책임
```

#### ✅ Good Example

```csharp
// 책임 1: 이동 처리
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 5f;
    private CharacterController _controller;
    
    public void Move(Vector3 direction) { /* 이동 로직 */ }
}

// 책임 2: 전투 처리
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Weapon _weapon;
    [SerializeField] private float _health = 100f;
    
    public event Action<float> OnHealthChanged;
    public event Action OnDeath;
    
    public void Attack() { /* 공격 로직 */ }
    public void TakeDamage(float damage) { /* 피해 로직 */ }
}

// 책임 3: UI 표현
public class PlayerUI : MonoBehaviour
{
    [SerializeField] private HealthBar _healthBar;
    [SerializeField] private Text _scoreText;
    
    public void UpdateHealth(float health) { /* UI 업데이트 */ }
    public void UpdateScore(int score) { /* 점수 업데이트 */ }
}

// 책임 4: 오디오 처리
public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    
    public void PlaySound(string soundName) { /* 사운드 재생 */ }
}

// 조합: MonoBehaviour 는 조립만 담당
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private PlayerCombat _combat;
    [SerializeField] private PlayerUI _ui;
    [SerializeField] private PlayerAudio _audio;
    
    private void Awake()
    {
        _combat.OnHealthChanged += _ui.UpdateHealth;
        _combat.OnDeath += HandleDeath;
    }
}
```

---

### 2. OCP (Open-Closed Principle) - 개방-폐쇄 원칙

**핵심:** 확장에는 열려 있고, 수정에는 닫혀 있어야 한다. **switch/if-else 금지.**

**적용 방법:**
- 인터페이스 또는 추상 클래스를 정의
- 새로운 기능은 기존 코드 수정 없이 추가
- 전략 패턴, 팩토리 패턴 활용

**위반 신호 (절대 금지):**
```csharp
// ❌ 새 타입 추가 시 이 코드를 수정해야 함 = OCP 위반
public void CreateEnemy(string type)
{
    switch (type)
    {
        case "Goblin": return Instantiate(goblinPrefab);
        case "Orc": return Instantiate(orcPrefab);
        case "Dragon": return Instantiate(dragonPrefab);
        // 새 적 추가 시 switch 문 수정 필요 = 위반
    }
}

// ❌ 타입 체크 = 다형성 사용하지 않음 = OCP 위반
public void ProcessDamage(Character target)
{
    if (target is Player) { /* 플레이어 처리 */ }
    else if (target is Enemy) { /* 적 처리 */ }
    else if (target is Boss) { /* 보스 처리 */ }
}
```

**올바른 접근:**
```csharp
// ✅ 딕셔너리 조회 - 새 적 추가 시 기존 코드 수정 불필요
public interface IEnemyFactory { IEnemy Create(); }

public class EnemySpawner
{
    [Inject] private Dictionary<string, IEnemyFactory> _factories;
    
    public IEnemy Spawn(string type) => _factories[type].Create();
    // switch 없음 - 팩토리만 추가하면 됨
}

// ✅ 다형성 - 타입 체크 불필요
public interface IDamageable { void TakeDamage(float damage); }

public class DamageHandler
{
    public void Process(IDamageable target, float damage)
    {
        target.TakeDamage(damage);  // 타입 관계없이 일관됨
    }
}
```

#### ❌ Bad Example

```csharp
public class SkillManager : MonoBehaviour
{
    public void UseSkill(string skillType, Character target)
    {
        switch (skillType)
        {
            case "Fireball":
                var fireball = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
                fireball.SetDamage(50);
                fireball.SetEffect(EffectType.Burn);
                break;
            
            case "IceBolt":
                var ice = Instantiate(iceBoltPrefab, transform.position, Quaternion.identity);
                ice.SetDamage(30);
                ice.SetEffect(EffectType.Freeze);
                break;
            
            case "Lightning":
                var lightning = Instantiate(lightningPrefab, transform.position, Quaternion.identity);
                lightning.SetDamage(40);
                lightning.SetChainCount(3);
                break;
            
            // 새로운 스킬 추가 시 매번 수정 필요
        }
    }
}
```

#### ✅ Good Example

```csharp
// 스킬 인터페이스
public interface ISkill
{
    string SkillName { get; }
    void Execute(Character caster, Character target);
}

// 베이스 클래스
public abstract class SkillBase : MonoBehaviour, ISkill
{
    public abstract string SkillName { get; }
    public abstract void Execute(Character caster, Character target);
    
    protected void ApplyDamage(Character target, float damage) { /* 공통 로직 */ }
    protected void ApplyEffect(Character target, EffectType effect) { /* 공통 로직 */ }
}

// 구체적인 스킬 구현
public class FireballSkill : SkillBase
{
    public override string SkillName => "Fireball";
    
    public override void Execute(Character caster, Character target)
    {
        var fireball = Instantiate(fireballPrefab, caster.transform.position, Quaternion.identity);
        fireball.SetDamage(50);
        fireball.SetEffect(EffectType.Burn);
        fireball.Target = target;
    }
}

public class IceBoltSkill : SkillBase
{
    public override string SkillName => "IceBolt";
    
    public override void Execute(Character caster, Character target)
    {
        var ice = Instantiate(iceBoltPrefab, caster.transform.position, Quaternion.identity);
        ice.SetDamage(30);
        ice.SetEffect(EffectType.Freeze);
        ice.Target = target;
    }
}

public class LightningSkill : SkillBase
{
    public override string SkillName => "Lightning";
    
    public override void Execute(Character caster, Character target)
    {
        var lightning = Instantiate(lightningPrefab, caster.transform.position, Quaternion.identity);
        lightning.SetDamage(40);
        lightning.SetChainCount(3);
        lightning.Target = target;
    }
}

// 매니저 - 수정 없이 확장 가능
public class SkillManager : MonoBehaviour
{
    [Inject] private Dictionary<string, ISkill> _skills;
    
    public void UseSkill(string skillName, Character target)
    {
        if (_skills.TryGetValue(skillName, out var skill))
        {
            skill.Execute(GetCaster(), target);
        }
    }
}

// DI 등록 - 새 스킬 추가 시 이곳만 수정
public class SkillInstaller : GameInstaller
{
    public override void Install(ServiceCollection services)
    {
        services.Add<ISkill>().To<FireballSkill>().AsTransient();
        services.Add<ISkill>().To<IceBoltSkill>().AsTransient();
        services.Add<ISkill>().To<LightningSkill>().AsTransient();
        // 새 스킬 추가 시 여기만 추가
    }
}
```

---

### 3. LSP (Liskov Substitution Principle) - 리스코프 치환 원칙

**핵심:** 자식 클래스는 부모 클래스를 항상 대체할 수 있어야 한다.

**위반 신호:**
- `if (obj is ChildType)` 타입 체크가 많음
- 자식에서 `NotImplementedException` 또는 `throw` 발생
- 오버라이드 시 Preconditions 를 강화

#### ❌ Bad Example

```csharp
public abstract class Character : MonoBehaviour
{
    public virtual void TakeDamage(float damage)
    {
        _health -= damage;
        if (_health <= 0) Die();
    }
    
    public abstract void Die();
}

public class Player : Character
{
    public override void TakeDamage(float damage)
    {
        // 부모의 로직을 완전히 무시
        if (_isInvincible) return;  // 추가 조건
        base.TakeDamage(damage * 0.5f);  // 값 변경
    }
    
    public override void Die()
    {
        // 플레이어는 죽지 않음 - 예외 발생
        throw new InvalidOperationException("Player cannot die!");
    }
}

public class Boss : Character
{
    public override void TakeDamage(float damage)
    {
        // 타입에 따라 다른 동작 - 외부에서 타입 체크 필요
        if (this is Boss boss && boss._phase == BossPhase.Invulnerable)
            return;
        base.TakeDamage(damage);
    }
}

// 사용하는 코드 - 타입 체크 필요 (LSP 위반)
public class DamageHandler : MonoBehaviour
{
    public void HandleDamage(Character character, float damage)
    {
        if (character is Player)
        {
            // 플레이어 특별 처리
        }
        else if (character is Boss boss)
        {
            // 보스 특별 처리
        }
        else
        {
            character.TakeDamage(damage);
        }
    }
}
```

#### ✅ Good Example

```csharp
public abstract class Character : MonoBehaviour
{
    [SerializeField] protected float _maxHealth = 100f;
    protected float _currentHealth;
    
    public event Action<float> OnHealthChanged;
    public event Action OnDied;
    
    public bool IsAlive => _currentHealth > 0;
    
    protected virtual void Awake()
    {
        _currentHealth = _maxHealth;
    }
    
    public virtual void TakeDamage(float damage)
    {
        if (!IsAlive) return;  // 이미 죽었으면 무시
        
        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        OnHealthChanged?.Invoke(_currentHealth);
        
        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    
    public virtual void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        OnHealthChanged?.Invoke(_currentHealth);
    }
    
    protected virtual void Die()
    {
        OnDied?.Invoke();
    }
}

public class Player : Character
{
    [SerializeField] private float _invincibleTime = 1f;
    private bool _isInvincible;
    
    protected override void Awake()
    {
        base.Awake();
        _isInvincible = false;
    }
    
    public override void TakeDamage(float damage)
    {
        if (_isInvincible) return;  // 무적 상태면 무시 (조건 추가 OK)
        
        base.TakeDamage(damage);  // 부모 로직 유지
        
        if (IsAlive)
        {
            StartCoroutine(InvincibleCoroutine());
        }
    }
    
    protected override void Die()
    {
        // 플레이어 죽음 처리 (예외 없음)
        base.Die();
        // 추가: UI 업데이트, 리스폰 로직 등
    }
    
    private IEnumerator InvincibleCoroutine()
    {
        _isInvincible = true;
        yield return new WaitForSeconds(_invincibleTime);
        _isInvincible = false;
    }
}

public class Boss : Character
{
    [SerializeField] private BossPhase[] _phases;
    private int _currentPhaseIndex;
    
    protected override void Die()
    {
        _currentPhaseIndex++;
        if (_currentPhaseIndex < _phases.Length)
        {
            // 다음 페이즈로 전환
            ChangePhase(_currentPhaseIndex);
            _currentHealth = _maxHealth;  // 체력 회복
            OnHealthChanged?.Invoke(_currentHealth);
        }
        else
        {
            // 진짜 죽음
            base.Die();
        }
    }
}

// 사용하는 코드 - 타입 체크 불필요
public class DamageHandler : MonoBehaviour
{
    public void HandleDamage(Character character, float damage)
    {
        // 모든 Character 는 일관된 방식으로 동작
        character.TakeDamage(damage);
    }
}
```

---

### 4. ISP (Interface Segregation Principle) - 인터페이스 분리 원칙

**핵심:** 클라이언트는 사용하지 않는 메서드에 의존하지 않아야 한다.

**판단 기준:**
- 인터페이스에 5 개 이상 메서드가 있으면 분리 고려
- 구현체에서 `throw new NotImplementedException()` 가 있으면 위반
- 인터페이스 이름에 "And", "With" 가 포함되면 분리 신호

#### ❌ Bad Example

```csharp
// 거대한 인터페이스 - 모든 캐릭터가 모두 구현해야 함
public interface ICharacter
{
    void Move(Vector3 direction);
    void Attack();
    void TakeDamage(float damage);
    void Die();
    void PlayAnimation(string animationName);
    void PlaySound(string soundName);
    void SaveData();
    void LoadData();
    void ShowUI();
    void HideUI();
}

// FlyingUnit 은 이동만 사용, 나머지는 불필요
public class FlyingUnit : MonoBehaviour, ICharacter
{
    public void Move(Vector3 direction) { /* 사용 */ }
    public void Attack() { /* 사용 안 함 */ throw new NotImplementedException(); }
    public void TakeDamage(float damage) { /* 사용 */ }
    public void Die() { /* 사용 */ }
    public void PlayAnimation(string animationName) { /* 사용 안 함 */ }
    public void PlaySound(string soundName) { /* 사용 안 함 */ }
    public void SaveData() { /* 사용 안 함 */ }
    public void LoadData() { /* 사용 안 함 */ }
    public void ShowUI() { /* 사용 안 함 */ }
    public void HideUI() { /* 사용 안 함 */ }
}

// Turret 는 공격만 사용, 이동은 불필요
public class Turret : MonoBehaviour, ICharacter
{
    public void Move(Vector3 direction) { /* 사용 안 함 - 제자리 */ }
    public void Attack() { /* 사용 */ }
    public void TakeDamage(float damage) { /* 사용 */ }
    public void Die() { /* 사용 */ }
    // ... 나머지 모두 구현하지만 대부분 사용 안 함
}
```

#### ✅ Good Example

```csharp
// 작고 구체적인 인터페이스들
public interface IMovable
{
    void Move(Vector3 direction);
    Vector3 Position { get; }
}

public interface IAttackable
{
    void Attack();
    float AttackRange { get; }
}

public interface IDamageable
{
    void TakeDamage(float damage);
    float CurrentHealth { get; }
    bool IsAlive { get; }
}

public interface IDestructible
{
    event Action OnDied;
}

public interface IAnimatable
{
    void PlayAnimation(string animationName);
}

public interface ISaveable
{
    CharacterData Save();
    void Load(CharacterData data);
}

// 필요에 따라 조합
public class FlyingUnit : MonoBehaviour, IMovable, IDamageable, IDestructible
{
    public void Move(Vector3 direction) { /* 비행 이동 */ }
    public Vector3 Position => transform.position;
    
    public void TakeDamage(float damage) { /* 피해 처리 */ }
    public float CurrentHealth => _health;
    public bool IsAlive => _health > 0;
    
    public event Action OnDied;
}

public class Turret : MonoBehaviour, IAttackable, IDamageable, IDestructible
{
    public void Attack() { /* 포격 */ }
    public float AttackRange => _range;
    
    public void TakeDamage(float damage) { /* 피해 처리 */ }
    public float CurrentHealth => _health;
    public bool IsAlive => _health > 0;
    
    public event Action OnDied;
}

public class Player : MonoBehaviour, 
    IMovable, IAttackable, IDamageable, IDestructible,
    IAnimatable, ISaveable
{
    // 모든 인터페이스 구현 - 플레이어는 모든 기능이 필요
    public void Move(Vector3 direction) { }
    public Vector3 Position => transform.position;
    public void Attack() { }
    public float AttackRange => _weapon.Range;
    public void TakeDamage(float damage) { }
    public float CurrentHealth => _health;
    public bool IsAlive => _health > 0;
    public event Action OnDied;
    public void PlayAnimation(string animationName) { }
    public CharacterData Save() { /* 저장 */ }
    public void Load(CharacterData data) { /* 로드 */ }
}

// 사용하는 코드 - 필요한 인터페이스만 의존
public class MovementSystem
{
    public void UpdateMovement(IMovable movable, Vector3 input)
    {
        movable.Move(input);
    }
}

public class CombatSystem
{
    public void ProcessAttack(IAttackable attacker, IDamageable target)
    {
        attacker.Attack();
        target.TakeDamage(CalculateDamage(attacker));
    }
}
```

---

### 5. DIP (Dependency Inversion Principle) - 의존 역전 원칙

**핵심:** 구체적이 아닌 추상화에 의존한다. **Unity Inspector 와 조화되어야 한다.**

**적용 방법:**
- 필드/프로퍼티 타입을 인터페이스로 선언
- 생성자/프로퍼티로 의존성 주입 (DI Container 활용)
- `new ConcreteClass()` 직접 인스턴스화 금지
- **단, Unity Inspector 를 통한 기획자 편집은 유지**

**위반 신호:**
```csharp
// ❌ 구체 클래스에 직접 의존
public class Player : MonoBehaviour
{
    [SerializeField] private AudioManager _audio;  // 구체 클래스
    [SerializeField] private SaveManager _save;    // 구체 클래스
}

// ❌ 직접 인스턴스화
public class Player : MonoBehaviour
{
    private void Start()
    {
        _audio = new AudioManager();  // new 금지
        _save = new JsonSaveManager(); // new 금지
    }
}

// ❌ DIP 만 지키고 Inspector 무시
public class Player : MonoBehaviour
{
    [Inject] private IWeapon _weapon;
    // ❌ 기획자가 Inspector 에서 무기 값을 수정할 수 없음
}
```

**올바른 접근:**
```csharp
// ✅ 인터페이스 의존 + Inspector 조화
public class Player : MonoBehaviour
{
    // Inspector: 기획자가 수정 (데이터)
    [Header("Stats")]
    [SerializeField] private float _baseHealth = 100f;
    [SerializeField] private float _moveSpeed = 5f;
    
    // DI: 코드에서 주입 (로직/서비스)
    [Inject] private IWeapon _weapon;
    [Inject] private IAudioManager _audio;
    [Inject] private ISaveManager _save;
}

// ✅ ScriptableObject + DI 조합
[CreateAssetMenu(fileName = "Weapon", menuName = "Game/Weapon")]
public class WeaponData : ScriptableObject
{
    public float damage = 10f;  // Inspector 에서 수정 가능
    public GameObject projectilePrefab;
}

public class Sword : MonoBehaviour, IWeapon
{
    [SerializeField] private WeaponData _data;  // Inspector 에서 데이터 설정
    [Inject] private IEffectPool _effects;      // DI 로 서비스 주입
    
    public void Attack()
    {
        // _data.damage 사용 (기획자가 수정 가능)
        // _effects 사용 (DI 로 주입)
    }
}

// ✅ ScriptableObject 도 DI 등록
public class GameInstaller : GameInstaller
{
    [SerializeField] private GameData _gameData;  // Inspector 에서 설정
    
    public override void Install(ServiceCollection services)
    {
        services.Add<GameData>().ToInstance(_gameData);  // DI 에 등록
        services.Add<IPhysicsConfig>()
            .ToFactory(r => new PhysicsConfig(r.Resolve<GameData>().gravity));
    }
}
```

#### ❌ Bad Example

```csharp
public class Player : MonoBehaviour
{
    // 구체 클래스에 직접 의존
    [SerializeField] private AudioPlayer _audioPlayer;
    [SerializeField] private ParticleSystem _hitEffect;
    [SerializeField] private SaveManager _saveManager;
    
    private void Start()
    {
        // 직접 인스턴스화
        _audioPlayer = new AudioPlayer();
        _saveManager = new JsonSaveManager();
    }
    
    public void TakeDamage(float damage)
    {
        _audioPlayer.Play("hit");  // AudioPlayer 에 의존
        Instantiate(_hitEffect, transform.position, Quaternion.identity);
        _saveManager.Save(this);  // JsonSaveManager 에 의존
    }
}

// 테스트 불가 - 실제 클래스에 단단히 결합
```

#### ✅ Good Example

```csharp
public class Player : MonoBehaviour
{
    // 인터페이스에 의존
    [Inject] private IAudioManager _audio;
    [Inject] private IEffectPool _effectPool;
    [Inject] private ISaveManager _saveManager;
    [Inject] private ILogger _logger;
    
    private void Awake()
    {
        // DI Container 에서 자동 주입
    }
    
    public void TakeDamage(float damage)
    {
        _audio.PlaySFX("hit");  // IAudioManager 에 의존
        _effectPool.Spawn("HitEffect", transform.position);  // IEffectPool
        _saveManager.SavePlayerData(GetSaveData());  // ISaveManager
        _logger.Log($"Player took {damage} damage");  // ILogger
    }
}

// DI 등록
public class GameInstaller : GameInstaller
{
    public override void Install(ServiceCollection services)
    {
        services.Add<IAudioManager>().To<AudioManager>().AsSingleton();
        services.Add<IEffectPool>().To<EffectPool>().AsSingleton();
        services.Add<ISaveManager>().To<JsonSaveManager>().AsSingleton();
        services.Add<ILogger>().To<ConsoleLogger>().AsSingleton();
        
        // 테스트 시에는 Mock 으로 교체 가능
        // services.Add<IAudioManager>().To<MockAudioManager>().AsSingleton();
    }
}

// 테스트 코드 - Mock 사용 가능
public class PlayerTests
{
    [Test]
    public void TakeDamage_PlaysHitSound()
    {
        // Arrange
        var mockAudio = new Mock<IAudioManager>();
        var mockEffect = new Mock<IEffectPool>();
        var mockSave = new Mock<ISaveManager>();
        
        var player = new Player();
        // Mock 주입
        player.SetDependencies(mockAudio.Object, mockEffect.Object, mockSave.Object);
        
        // Act
        player.TakeDamage(10f);
        
        // Assert
        mockAudio.Verify(a => a.PlaySFX("hit"), Times.Once);
    }
}
```

---

## ✅ 실전 적용 체크리스트

### AI 코딩 전 5 가지 질문

코드를 생성하기 전에 반드시 다음 5 가지를 스스로 검토합니다:

```
□ 1. SRP 검토
   "이 클래스는 하나의 책임만 가지는가? 
    설명에 '그리고'가 2 개 이상 있는가?"

□ 2. OCP 검토
   "새로운 기능이 추가될 때 기존 코드를 수정해야 하는가?
    인터페이스나 추상 클래스로 확장 가능한가?"

□ 3. LSP 검토
   "자식 클래스가 부모 클래스를 완전히 대체할 수 있는가?
    타입 체크 (is, as) 가 필요한가?"

□ 4. ISP 검토
   "인터페이스에 사용되지 않는 메서드가 있는가?
    5 개 이상 메서드가 있는 인터페이스를 분리했는가?"

□ 5. DIP 검토
   "구체 클래스를 직접 인스턴스화하고 있는가 (new 키워드)?
    필드/파라미터 타입이 인터페이스인가?"
```

### 코드 리뷰 시 자동 확인 항목

| 항목 | 체크 방법 | 위반 시 액션 |
|------|-----------|-------------|
| **클래스 길이** | 500 행 초과 | 기능별로 분리 |
| **메서드 길이** | 50 행 초과 | 하위 메서드로 분할 |
| **new 키워드** | 의존성 주입 가능한 곳 | DI 로 변경 |
| **switch-case** | 타입 기반 분기 | 다형성으로 변경 |
| **public 필드** | 직접 접근 가능 | 프로퍼티 또는 메서드로 변경 |
| **MonoBehaviour** | 3 개 이상 기능 | 컴포넌트 분리 |

---

## 🎮 Unity 특화 사례

### 1. MonoBehaviour 비대화 방지

#### ❌ Bad Example - God Class

```csharp
public class GameManager : MonoBehaviour
{
    // 모든 것을 하나의 클래스에서 처리
    private void Update()
    {
        HandlePlayerInput();
        UpdateEnemies();
        CheckCollisions();
        UpdateUI();
        PlayMusic();
        SaveProgress();
        CheckAchievements();
        HandleNetworkMessages();
    }
    
    private void HandlePlayerInput() { /* 50 lines */ }
    private void UpdateEnemies() { /* 50 lines */ }
    private void CheckCollisions() { /* 40 lines */ }
    private void UpdateUI() { /* 30 lines */ }
    private void PlayMusic() { /* 20 lines */ }
    private void SaveProgress() { /* 30 lines */ }
    private void CheckAchievements() { /* 25 lines */ }
    private void HandleNetworkMessages() { /* 40 lines */ }
    
    // 총 300+ 행, 8 개 이상의 책임
}
```

#### ✅ Good Example - 컴포넌트 분리

```csharp
// 시스템 인터페이스들
public interface IGameSystem
{
    void Initialize();
    void Update();
    void Shutdown();
}

// 개별 시스템들
public class InputSystem : MonoBehaviour, IGameSystem
{
    public void Initialize() { /* 입력 설정 */ }
    public void Update() { /* 입력 처리 */ }
    public void Shutdown() { /* 정리 */ }
}

public class EnemySystem : MonoBehaviour, IGameSystem
{
    [Inject] private List<Enemy> _enemies;
    
    public void Initialize() { /* 적 스폰 설정 */ }
    public void Update() { /* 적 AI 업데이트 */ }
    public void Shutdown() { /* 적 정리 */ }
}

public class CollisionSystem : MonoBehaviour, IGameSystem
{
    public void Initialize() { /* 충돌 설정 */ }
    public void Update() { /* 충돌 체크 */ }
    public void Shutdown() { /* 정리 */ }
}

public class UISystem : MonoBehaviour, IGameSystem
{
    [Inject] private IPlayer _player;
    
    public void Initialize() { /* UI 초기화 */ }
    public void Update() { /* UI 업데이트 */ }
    public void Shutdown() { /* UI 정리 */ }
}

// GameManager 는 조립만 담당
public class GameManager : MonoBehaviour
{
    [SerializeField] private List<MonoBehaviour> _systems;
    private List<IGameSystem> _gameSystems = new();
    
    [Inject] private IResolver _resolver;
    
    private void Awake()
    {
        // DI 에서 시스템 주입 받기
        foreach (var system in _systems)
        {
            if (system is IGameSystem gameSystem)
            {
                _gameSystems.Add(gameSystem);
                gameSystem.Initialize();
            }
        }
    }
    
    private void Update()
    {
        foreach (var system in _gameSystems)
        {
            system.Update();
        }
    }
    
    private void OnDestroy()
    {
        foreach (var system in _gameSystems)
        {
            system.Shutdown();
        }
    }
}

// DI 등록
public class CoreInstaller : GameInstaller
{
    public override void Install(ServiceCollection services)
    {
        services.Add<IGameSystem>().To<InputSystem>().AsSingleton();
        services.Add<IGameSystem>().To<EnemySystem>().AsSingleton();
        services.Add<IGameSystem>().To<CollisionSystem>().AsSingleton();
        services.Add<IGameSystem>().To<UISystem>().AsSingleton();
    }
}
```

### 2. Unity 이벤트 패턴 (느슨한 결합)

#### ❌ Bad Example - 직접 참조

```csharp
public class Player : MonoBehaviour
{
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private AchievementSystem _achievementSystem;
    
    public void TakeDamage(float damage)
    {
        _health -= damage;
        _uiManager.UpdateHealth(_health);  // 직접 호출
        _audioManager.Play("hit");  // 직접 호출
        _achievementSystem.Check("takeDamage");  // 직접 호출
    }
}
```

#### ✅ Good Example - 이벤트 기반

```csharp
public class Player : MonoBehaviour
{
    // 이벤트 정의
    public event Action<float> OnHealthChanged;
    public event Action<string> OnEventTriggered;
    
    public void TakeDamage(float damage)
    {
        _health -= damage;
        OnHealthChanged?.Invoke(_health);  // 이벤트 발생만
        OnEventTriggered?.Invoke("takeDamage");
    }
}

// UI 는 이벤트 구독
public class HealthUI : MonoBehaviour
{
    [Inject] private Player _player;
    
    private void OnEnable()
    {
        _player.OnHealthChanged += UpdateHealthBar;
    }
    
    private void OnDisable()
    {
        _player.OnHealthChanged -= UpdateHealthBar;
    }
    
    private void UpdateHealthBar(float health)
    {
        // UI 업데이트
    }
}

// 오디오는 이벤트 구독
public class SFXManager : MonoBehaviour
{
    [Inject] private IAudioManager _audio;
    
    private void OnEnable()
    {
        SubscribeToEvents();
    }
    
    private void SubscribeToEvents()
    {
        // 모든 플레이어 이벤트 구독
        FindObjectsOfType<Player>()
            .ToList()
            .ForEach(p => p.OnEventTriggered += HandlePlayerEvent);
    }
    
    private void HandlePlayerEvent(string eventName)
    {
        if (eventName == "takeDamage")
            _audio.Play("hit");
    }
}
```

### 3. ScriptableObject 활용 (데이터와 로직 분리)

#### ❌ Bad Example

```csharp
public class Weapon : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private float _range = 50f;
    [SerializeField] private int _magazineSize = 30;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private ParticleSystem _muzzleFlash;
    
    // 데이터와 로직이 섞여있음
    // 무기별 새 스크립트 필요
}
```

#### ✅ Good Example

```csharp
// 데이터는 ScriptableObject 로
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public float damage = 10f;
    public float fireRate = 0.5f;
    public float range = 50f;
    public int magazineSize = 30;
    public GameObject bulletPrefab;
    public ParticleSystem muzzleFlash;
    public AudioClip fireSound;
}

// 로직은 별도 클래스
public class Weapon : MonoBehaviour
{
    [Inject] private IWeaponFactory _weaponFactory;
    
    private WeaponData _currentWeapon;
    private float _nextFireTime;
    
    public void Initialize(WeaponData data)
    {
        _currentWeapon = data;
    }
    
    public void Fire()
    {
        if (Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + _currentWeapon.fireRate;
            ExecuteFire();
        }
    }
    
    private void ExecuteFire()
    {
        // _currentWeapon 데이터 사용
        var bullet = Instantiate(_currentWeapon.bulletPrefab, ...);
        // 데미지, 이팩트 등 적용
    }
}

// DI 에서 데이터 로드
public class WeaponInstaller : GameInstaller
{
    [SerializeField] private WeaponData[] _weaponDataList;
    
    public override void Install(ServiceCollection services)
    {
        var weaponDict = _weaponDataList.ToDictionary(w => w.weaponName);
        services.Add<Dictionary<string, WeaponData>>().ToInstance(weaponDict);
        services.Add<IWeaponFactory>().To<WeaponFactory>().AsSingleton();
    }
}
```

---

## 📝 요약

| 원칙 | 핵심 질문 | 키워드 | 제한 |
|------|-----------|--------|------|
| **SRP** | "이 클래스가 2 개 이상의 이유를 위해 변경되는가?" | 분리, 단일 책임 | 최대 300 행, 10 메서드 |
| **OCP** | "확장할 때 switch/if 를 수정하는가?" | 인터페이스, 추상화, 다형성 | switch/if-else 금지 |
| **LSP** | "자식이 부모를 대체할 때 예외가 발생하는가?" | 치환 가능성, 일관성 | 타입 체크 금지 |
| **ISP** | "사용하지 않는 메서드를 구현하는가?" | 작은 인터페이스, 목적별 분리 | 최대 5 메서드 |
| **DIP** | "구체 클래스를 new 로 만들고 있는가?" | 인터페이스 의존, DI Container | Inspector 와 조화 |

---

## 🚨 AI 코딩 전 필수 점검 (Mandatory Checklist)

**코드를 생성하기 전에 반드시 다음 5 가지를 확인합니다:**

```
□ [SRP] 이 클래스가 2 개 이상의 책임을 가지는가?
   → Player 가 이동 + 공격 + 인벤토리를 모두 처리하는가?
   → 300 행을 초과하는가? 메서드가 10 개 이상인가?
   
□ [OCP] 새 타입 추가 시 switch/if 를 수정해야 하는가?
   → 새 캐릭터/아이템/스킬 추가 시 기존 코드를 고치는가?
   → `is`, `as` 타입 체크를 사용하는가?
   
□ [LSP] 자식 클래스에서 예외 (throw) 가 발생하는가?
   → 부모 메서드를 오버라이드 시 예외를 던지는가?
   → 부모의 전제조건을 강화하는가?
   
□ [ISP] 인터페이스에 5 개 이상 메서드가 있는가?
   → 사용하지 않는 메서드를 구현해야 하는가?
   → NotImplementedException 가 있는가?
   
□ [DIP] 구체 클래스를 new 로 만들고 있는가?
   → Unity Inspector 와 충돌하는가?
   → 기획자가 값을 수정할 수 있는가?
```

**하나라도 "Yes" 면 설계를 다시 검토합니다.**

---

## 🔗 관련 문서

- [Recycle DI Container 설계](Recycle_DI.md)
- [Unity Best Practices](docs/UnityBestPractices.md)

---

*최종 업데이트: 2026-03-12*
