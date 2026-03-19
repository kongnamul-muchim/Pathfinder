# DI Container Library

## Quick Start

```csharp
// 1. 등록
var container = new DIContainer()
    .Register<ILogger, ConsoleLogger>()
    .Register<IService, MyService>();

// 2. 해결
var service = container.Resolve<IService>();
```

## Attributes

| Attribute | 용도 |
|-----------|------|
| `[Inject]` | 필수 의존성 주입 |
| `[InjectOptional]` | 선택적 의존성 (없어도 무시) |

## API Reference

| Method | Description |
|--------|-------------|
| `Register<TInterface, TImplementation>()` | 인터페이스 → 구현체 매핑 |
| `RegisterInstance<T>(T instance)` | 싱글톤 인스턴스 등록 |
| `Resolve<T>()` | 의존성 해결 |
| `InjectInto(object)` | 기존 객체에 주입 (MonoBehaviour용) |
| `IsRegistered<T>()` | 등록 여부 확인 |

## Unity Integration

```csharp
public class Player : MonoBehaviour
{
    [Inject] private IInputManager _input;
    [InjectOptional] private ILogger _logger;
}

// 주입 실행
container.InjectInto(player);
```

## 주의사항

- 스레드 안전하지 않음
- 순환 의존성 자동 감지