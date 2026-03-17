# Agent Guidelines

## 📚 문서 참조 가이드

### 상황별 참조 문서

**코어 아키텍처 작업 시:**
- `DI_Library.md` - DI Container 구현 구조
- `SOLID_Coding_Standard.md` - 코딩 표준

**게임 시스템 작업 시:**
- `Docs_WarpPoint.md` - 워프/체크포인트 시스템
- `WarpPoint_Setup_Guide.md` - 설정 방법

**Unity 씬 작업 시:**
- `Unity_Scene_Setup_Guide.md` - 씬 구성
- `Animation_Setup_Guide.md` - 애니메이션 설정

**타일맵 작업 시:**
- **⚠️ 코드 생성 금지** - Unity Tile Palette 사용
- `Docs_TilemapGenerators.md` - 삭제된 생성기 코드 (참조용)

**진행 상황 확인:**
- `TaskList.md` - 작업 목록

---

## DI Container Implementation Reference

When implementing DI Container features, refer to `DI_Library.md` for the core structure and patterns.

### Key Reference Points from DI_Library.md

#### Core Components
- **DIContainer.cs**: Main container class with service registration and resolution
- **InjectAttribute**: Marks constructors, fields, or properties for injection
- **InjectOptionalAttribute**: Marks optional dependencies that can be null

#### Implementation Patterns

1. **Service Registration**
   - `Register<TInterface, TImplementation>()`: Maps interface to implementation
   - `RegisterInstance<T>(T instance)`: Registers pre-created singleton instances

2. **Dependency Resolution**
   - `Resolve<T>()`: Resolves service with all dependencies recursively
   - `Resolve(Type type)`: Non-generic version
   - Circular dependency detection via `_resolvingStack`

3. **Injection Types**
   - Constructor injection (preferred, with `[Inject]` attribute)
   - Field injection (`[Inject]` on fields)
   - Property injection (`[Inject]` on properties)
   - Optional injection (`[InjectOptional]` - skips if not registered)

4. **Unity Integration**
   - `InjectInto(object instance)`: Injects dependencies into existing MonoBehaviour
   - Call in `Awake()` of bootstrap/setup classes

#### Key Implementation Details

```csharp
// Constructor selection priority:
// 1. Constructor with [Inject] attribute
// 2. Constructor with most parameters (if no [Inject] marked)

// Member injection order:
// 1. All fields with [Inject] or [InjectOptional]
// 2. All properties with [Inject] or [InjectOptional]

// Error handling:
// - Throws InvalidOperationException for circular dependencies
// - Throws InvalidOperationException for unregistered services
// - Silent skip for optional dependencies not found
```

#### Container Hierarchy (Extended)

For multi-scoped containers (Global + Scene):
- Global Container: Application-lifetime singletons
- Scene Containers: Scene-specific services
- LIFO resolution: Search from current scene up to global

#### File Organization
```
Assets/Scripts/Core/DI/
├── InjectAttribute.cs      # [Inject], [InjectOptional]
├── ServiceLifetime.cs      # Singleton, Transient enum
├── DIContainer.cs          # Core container (refer to DI_Library.md structure)
├── DIContainerManager.cs   # Static list + LIFO management
├── Installer.cs            # MonoBehaviour installer base
└── RootContext.cs          # Scene installer orchestrator
```

### Unity-Specific Guidelines

1. **Installer Pattern**: Use `Installer` MonoBehaviour with `RootContext` for scene setup
2. **Timing**: Register in `Awake()`, inject in `Start()` to ensure dependencies ready
3. **Factories**: For dynamic instantiation (Enemy, Projectile), inject factory then call `InjectInto` on created instances
4. **Lifecycle**: Scene containers auto-dispose on scene unload (clear singletons)

### Testing Considerations

- Clear containers between tests: `container.Clear()`
- Not thread-safe - use one container per test
- Optional dependencies useful for test doubles

---

## ⚠️ 타일맵 작업 규칙

**코드로 자동 생성하지 않음**
- 모든 타일맵은 Unity 에디터에서 수동으로 배치
- Tile Palette 사용
- 자동 생성기는 삭제됨: `Docs_TilemapGenerators.md` 참조

---

## General Development Notes

- Follow existing code conventions in the codebase
- Maintain consistency with established patterns
- Document public APIs with XML comments
- Prefer explicit over implicit for maintainability
