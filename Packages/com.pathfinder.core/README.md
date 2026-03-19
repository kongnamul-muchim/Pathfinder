# Pathfinder Core

Unity용 경량 DI 컨테이너와 핵심 인터페이스 패키지입니다.

## 설치

`Packages/manifest.json`에 추가:
```json
{
  "dependencies": {
    "com.pathfinder.core": "file:../com.pathfinder.core"
  }
}
```

## 빠른 시작

### 1. Installer 작성
```csharp
public class GameInstaller : Installer
{
    public override void Install(DIContainer container)
    {
        container.Register<IAbilityManager, AbilityManager>(lifetime: ServiceLifetime.Singleton);
        container.Register<IDeathManager, DeathManager>(lifetime: ServiceLifetime.Singleton);
    }
}
```

### 2. RootContext 설정
- 씬에 빈 GameObject 생성
- RootContext 컴포넌트 추가
- Installers 리스트에 GameInstaller 추가

### 3. 의존성 주입
```csharp
public class PlayerController : MonoBehaviour
{
    [Inject] private IAbilityManager _abilityManager;
    [Inject] private IDeathManager _deathManager;
}
```

## 포함된 파일

### DI System (6개)
- DIContainer.cs
- DIContainerManager.cs
- Installer.cs
- InjectAttribute.cs
- RootContext.cs
- ServiceLifetime.cs

### Interfaces (4개)
- IInteractable.cs
- IAbilityManager.cs
- IDeathManager.cs
- ICheckpoint.cs

### Enums (1개)
- AbilityType.cs

## 문서

- [DI Library](Documentation~/DI_Library.md)
- [SOLID Coding Standard](Documentation~/SOLID_Coding_Standard.md)
- [API Reference](Documentation~/CodeReference.md)