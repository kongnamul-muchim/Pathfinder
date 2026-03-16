# Lightweight DI Container Library

A lightweight, reusable Dependency Injection container for C# using Reflection.

## Overview

This library provides:
- Service registration with interface/implementation mapping
- Recursive dependency resolution
- Constructor injection with `[Inject]` attribute support
- Field and property injection
- Optional dependency injection via `[InjectOptional]`
- Circular dependency detection
- Post-creation injection for Unity MonoBehaviour integration

## Installation

Copy the code below into your project as a single file `DIContainer.cs`.

## Source Code

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ============================================
// Attributes
// ============================================

/// <summary>
/// Marks constructors, fields, or properties for dependency injection.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property)]
public class InjectAttribute : Attribute { }

/// <summary>
/// Marks optional dependencies. If not registered, injection is skipped silently.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class InjectOptionalAttribute : Attribute { }

// ============================================
// DI Container
// ============================================

/// <summary>
/// Lightweight dependency injection container with recursive resolution.
/// </summary>
public class DIContainer
{
    private readonly Dictionary<Type, Type> _registrations = new();
    private readonly Dictionary<Type, object> _instances = new();
    private readonly Stack<Type> _resolvingStack = new();

    #region Registration

    /// <summary>
    /// Registers an interface with its implementation type.
    /// </summary>
    public DIContainer Register<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        _registrations[typeof(TInterface)] = typeof(TImplementation);
        return this;
    }

    /// <summary>
    /// Registers an instance directly (for singletons or pre-created objects).
    /// </summary>
    public DIContainer RegisterInstance<T>(T instance) where T : class
    {
        _instances[typeof(T)] = instance;
        return this;
    }

    #endregion

    #region Resolution

    /// <summary>
    /// Resolves a service by type, recursively resolving all dependencies.
    /// </summary>
    public T Resolve<T>() where T : class
    {
        var type = typeof(T);

        // Return existing singleton instance
        if (_instances.TryGetValue(type, out var existing))
            return (T)existing;

        // Check for circular dependency
        if (_resolvingStack.Contains(type))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected: {string.Join(" -> ", _resolvingStack)} -> {type.Name}");
        }

        // Get implementation type
        if (!_registrations.TryGetValue(type, out var implementationType))
        {
            implementationType = type.IsInterface ? null : type;

            if (implementationType == null)
            {
                throw new InvalidOperationException(
                    $"Service '{type.Name}' is not registered.");
            }
        }

        // Create instance with circular detection
        _resolvingStack.Push(implementationType);
        try
        {
            var instance = CreateInstance(implementationType);
            return (T)instance;
        }
        finally
        {
            _resolvingStack.Pop();
        }
    }

    /// <summary>
    /// Non-generic version of Resolve.
    /// </summary>
    public object Resolve(Type type)
    {
        if (_instances.TryGetValue(type, out var existing))
            return existing;

        if (_resolvingStack.Contains(type))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected: {string.Join(" -> ", _resolvingStack)} -> {type.Name}");
        }

        if (!_registrations.TryGetValue(type, out var implementationType))
        {
            implementationType = type.IsInterface ? null : type;

            if (implementationType == null)
            {
                throw new InvalidOperationException(
                    $"Service '{type.Name}' is not registered.");
            }
        }

        _resolvingStack.Push(implementationType);
        try
        {
            return CreateInstance(implementationType);
        }
        finally
        {
            _resolvingStack.Pop();
        }
    }

    #endregion

    #region Instance Creation

    private object CreateInstance(Type type)
    {
        var constructors = type.GetConstructors();
        
        // Prefer constructor with [Inject] attribute
        var injectConstructor = constructors
            .FirstOrDefault(c => c.GetCustomAttribute<InjectAttribute>() != null);

        // Fallback to constructor with most parameters
        if (injectConstructor == null)
        {
            injectConstructor = constructors
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();
        }

        if (injectConstructor == null)
        {
            throw new InvalidOperationException(
                $"No suitable constructor found for type '{type.Name}'");
        }

        // Resolve constructor parameters
        var parameters = injectConstructor.GetParameters();
        var parameterInstances = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            try
            {
                parameterInstances[i] = Resolve(paramType);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve parameter '{parameters[i].Name}' " +
                    $"of type '{paramType.Name}' in constructor of '{type.Name}'");
            }
        }

        // Create instance
        var instance = injectConstructor.Invoke(parameterInstances);

        // Inject fields and properties
        InjectMembers(instance);

        return instance;
    }

    #endregion

    #region Member Injection

    private void InjectMembers(object instance)
    {
        var type = instance.GetType();
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // Inject fields
        var fields = type.GetFields(bindingFlags)
            .Where(f => f.GetCustomAttribute<InjectAttribute>() != null ||
                        f.GetCustomAttribute<InjectOptionalAttribute>() != null);

        foreach (var field in fields)
        {
            InjectField(field, instance);
        }

        // Inject properties
        var properties = type.GetProperties(bindingFlags)
            .Where(p => p.GetCustomAttribute<InjectAttribute>() != null ||
                        p.GetCustomAttribute<InjectOptionalAttribute>() != null);

        foreach (var property in properties)
        {
            InjectProperty(property, instance);
        }
    }

    private void InjectField(FieldInfo field, object instance)
    {
        var isOptional = field.GetCustomAttribute<InjectOptionalAttribute>() != null;

        try
        {
            var dependency = Resolve(field.FieldType);
            field.SetValue(instance, dependency);
        }
        catch (InvalidOperationException) when (isOptional)
        {
            // Optional dependency - ignore if not registered
        }
    }

    private void InjectProperty(PropertyInfo property, object instance)
    {
        if (!property.CanWrite) return;

        var isOptional = property.GetCustomAttribute<InjectOptionalAttribute>() != null;

        try
        {
            var dependency = Resolve(property.PropertyType);
            property.SetValue(instance, dependency);
        }
        catch (InvalidOperationException) when (isOptional)
        {
            // Optional dependency - ignore if not registered
        }
    }

    #endregion

    #region External Injection

    /// <summary>
    /// Injects dependencies into an existing object (for Unity MonoBehaviour support).
    /// </summary>
    public void InjectInto(object instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var type = instance.GetType();
        _resolvingStack.Push(type);
        try
        {
            InjectMembers(instance);
        }
        finally
        {
            _resolvingStack.Pop();
        }
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Checks if a service type is registered.
    /// </summary>
    public bool IsRegistered<T>() => _registrations.ContainsKey(typeof(T));

    /// <summary>
    /// Checks if a service type is registered (non-generic).
    /// </summary>
    public bool IsRegistered(Type type) => _registrations.ContainsKey(type);

    /// <summary>
    /// Gets all registered service mappings.
    /// </summary>
    public IEnumerable<(Type Interface, Type Implementation)> GetRegistrations()
    {
        return _registrations.Select(kvp => (kvp.Key, kvp.Value));
    }

    /// <summary>
    /// Clears all registrations and instances.
    /// </summary>
    public void Clear()
    {
        _registrations.Clear();
        _instances.Clear();
        _resolvingStack.Clear();
    }

    #endregion
}
```

## Quick Start

### 1. Define Interfaces and Implementations

```csharp
public interface ILogger
{
    void Log(string message);
}

public class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine(message);
}

public interface IService
{
    void DoWork();
}

public class MyService : IService
{
    private readonly ILogger _logger;

    [Inject]
    public MyService(ILogger logger)
    {
        _logger = logger;
    }

    public void DoWork() => _logger.Log("Working...");
}
```

### 2. Configure Container

```csharp
var container = new DIContainer()
    .Register<ILogger, ConsoleLogger>()
    .Register<IService, MyService>();
```

### 3. Resolve Services

```csharp
var service = container.Resolve<IService>();
service.DoWork(); // Output: Working...
```

## Advanced Usage

### Constructor Selection

```csharp
public class MultiConstructorService
{
    private readonly ILogger _logger;
    private readonly IDatabase _database;

    [Inject] // Explicitly mark which constructor to use
    public MultiConstructorService(ILogger logger, IDatabase database)
    {
        _logger = logger;
        _database = database;
    }

    // Parameterless constructor - not used by DI
    public MultiConstructorService() { }
}
```

### Optional Dependencies

```csharp
public class ServiceWithOptionalDependency
{
    [InjectOptional]
    public IAnalytics Analytics { get; set; } // Can be null

    [Inject]
    public ServiceWithOptionalDependency(ILogger logger) { }
}
```

### Unity Integration

```csharp
// For MonoBehaviour objects that Unity creates
public class Player : MonoBehaviour
{
    [Inject]
    private IAudioSystem _audio { get; set; }

    [InjectOptional]
    private IAchievementSystem _achievements;
}

// Initialize dependencies
public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        var container = new DIContainer()
            .Register<IAudioSystem, AudioManager>();

        var player = GetComponent<Player>();
        container.InjectInto(player);
    }
}
```

### Pre-registered Instances

```csharp
var logger = new FileLogger("app.log");

var container = new DIContainer()
    .RegisterInstance<ILogger>(logger); // Singleton instance

var service1 = container.Resolve<IService>();
var service2 = container.Resolve<IService>();
// Both receive the same logger instance
```

## API Reference

| Method | Description |
|--------|-------------|
| `Register<TInterface, TImplementation>()` | Maps interface to implementation |
| `RegisterInstance<T>(T instance)` | Registers pre-created singleton |
| `Resolve<T>()` | Resolves service with all dependencies |
| `InjectInto(object)` | Injects into existing object |
| `IsRegistered<T>()` | Checks if type is registered |
| `Clear()` | Clears all registrations |

## Exceptions

- `InvalidOperationException`: Circular dependency detected or service not registered
- `ArgumentNullException`: Null instance passed to `InjectInto`

## Thread Safety

This container is **not thread-safe**. Create one container per thread or add external locking.

## License

Public Domain - Use as you wish.
