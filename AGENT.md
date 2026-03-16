# Agent Guidelines

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

## General Development Notes

- Follow existing code conventions in the codebase
- Maintain consistency with established patterns
- Document public APIs with XML comments
- Prefer explicit over implicit for maintainability
