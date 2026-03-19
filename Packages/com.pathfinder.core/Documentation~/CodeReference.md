# Core API Reference

## DI System

### DIContainer
```csharp
public class DIContainer
{
    DIContainer Register<TInterface, TImplementation>();
    DIContainer RegisterInstance<T>(T instance);
    T Resolve<T>();
    void InjectInto(object instance);
    bool IsRegistered<T>();
}
```

### Attributes
```csharp
[Inject]          // 필수 주입
[InjectOptional]  // 선택적 주입
```

### DIContainerManager (Static)
```csharp
static DIContainer Global { get; }
static DIContainer CurrentSceneContainer { get; }
static T Resolve<T>();
static void InjectInto(object obj);
static void ClearGlobal();
```

### Installer
```csharp
public abstract class Installer : MonoBehaviour
{
    public abstract void Install(DIContainer container);
}
```

### RootContext
```csharp
public class RootContext : MonoBehaviour
{
    Installer[] _installers;
    void Awake(); // Installer 실행, 자식에 DI 주입
}
```

---

## Interfaces

### IInteractable
```csharp
public interface IInteractable
{
    string GetInteractionText();
    bool CanInteract();
    void OnInteract();
    Transform GetPromptTransform();
}
```

### IAbilityManager
```csharp
public interface IAbilityManager
{
    bool HasAbility(AbilityType ability);
    void UnlockAbility(AbilityType ability);
    IReadOnlyCollection<AbilityType> GetUnlockedAbilities();
    event Action<AbilityType> OnAbilityUnlocked;
    
    int GetLives();
    void AddExtraLife();
    bool ConsumeLife();
    event Action<int> OnLivesChanged;
}
```

### IDeathManager
```csharp
public interface IDeathManager
{
    void OnPlayerDeath();
    void Respawn();
    Vector3 GetLastCheckpoint();
    void SetCheckpoint(Vector3 position);
}
```

### ICheckpoint
```csharp
public interface ICheckpoint
{
    void Activate();
    bool IsActivated();
    Vector3 GetPosition();
}
```

---

## Enums

### AbilityType
```csharp
public enum AbilityType
{
    None,
    DoubleJump,
    Dash
}
```

### ServiceLifetime
```csharp
public enum ServiceLifetime
{
    Singleton,  // 싱글톤
    Transient   // 매번 새 인스턴스
}
```