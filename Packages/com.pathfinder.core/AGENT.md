# Pathfinder Core Package

AI 어시스턴트를 위한 문서 가이드입니다.

## 상황별 문서 참조

| 상황 | 참조 문서 |
|------|----------|
| DI 사용법 이해 | [DI_Library.md](Documentation~/DI_Library.md) |
| 코드 작성 표준 확인 | [SOLID_Coding_Standard.md](Documentation~/SOLID_Coding_Standard.md) |
| API 시그니처 확인 | [CodeReference.md](Documentation~/CodeReference.md) |

## 핵심 규칙

1. **SRP**: 클래스 300행, 메서드 10개 초과 금지
2. **OCP**: switch/if-else 타입 분기 금지
3. **DIP**: `[Inject]`로 의존성 주입, `new` 금지

## DI 패턴

```csharp
// 등록
container.Register<IInterface, Implementation>();

// 주입
[Inject] private IInterface _service;
```

## 패키지 구조

```
Runtime/
├── DI/           # DI 컨테이너 시스템
├── Interfaces/   # 핵심 인터페이스
└── Enums/        # 열거형
```