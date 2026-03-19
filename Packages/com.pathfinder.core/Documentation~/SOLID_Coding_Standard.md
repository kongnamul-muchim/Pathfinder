# SOLID Coding Standard

## 필수 체크리스트

```
□ [SRP] 클래스가 300행 초과 or 메서드 10개 초과?
□ [OCP] 새 타입 추가 시 switch/if 수정 필요?
□ [LSP] 자식에서 throw NotImplementedException?
□ [ISP] 인터페이스에 5개 이상 메서드?
□ [DIP] new 키워드로 구체 클래스 생성?
```

## 원칙 요약

| 원칙 | 핵심 | 위반 신호 |
|------|------|----------|
| **SRP** | 단일 책임 | 300행+, "그리고" 2개 이상 |
| **OCP** | 확장 열림, 수정 닫힘 | switch/if-else 타입 분기 |
| **LSP** | 자식이 부모 대체 가능 | throw, 타입 체크 |
| **ISP** | 작은 인터페이스 | 5개+ 메서드, NotImplementedException |
| **DIP** | 추상화 의존 | new ConcreteClass() |

## 안티패턴

```csharp
// ❌ God Object
public class Player : MonoBehaviour
{
    void HandleMovement() { }
    void HandleCombat() { }
    void HandleInventory() { }
}

// ❌ OCP 위반
switch (type)
{
    case "Goblin": ...
    case "Orc": ...
}

// ❌ DIP 위반
_audio = new AudioManager();
```

## 올바른 패턴

```csharp
// ✅ 책임 분리 + DI
public class Player : MonoBehaviour
{
    [Inject] private PlayerMovement _movement;
    [Inject] private PlayerCombat _combat;
}

// ✅ 다형성
public interface IEnemy { void Spawn(); }
_enemies[type].Spawn(); // switch 없음

// ✅ 인터페이스 의존
[Inject] private IAudioManager _audio;
```