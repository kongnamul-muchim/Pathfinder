using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Pathfinder.Core.DI
{
    public class DIContainer : IDisposable
    {
        private readonly Dictionary<string, object> _registrations = new();
        private readonly Dictionary<string, object> _singletonInstances = new();
        private readonly HashSet<string> _disposables = new();

        public DIContainer Register<TInterface, TImplementation>(string key = null, ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            var fullKey = GetFullKey<TInterface>(key);
            
            _registrations[fullKey] = new Registration
            {
                InterfaceType = typeof(TInterface),
                ImplementationType = typeof(TImplementation),
                Lifetime = lifetime
            };

            if (lifetime == ServiceLifetime.Singleton && typeof(IDisposable).IsAssignableFrom(typeof(TImplementation)))
            {
                _disposables.Add(fullKey);
            }

            return this;
        }

        public DIContainer RegisterInstance<T>(T instance, string key = null) where T : class
        {
            var fullKey = GetFullKey<T>(key);
            
            _registrations[fullKey] = new Registration
            {
                InterfaceType = typeof(T),
                ImplementationType = typeof(T),
                Lifetime = ServiceLifetime.Singleton,
                Instance = instance
            };

            _singletonInstances[fullKey] = instance;

            if (instance is IDisposable)
            {
                _disposables.Add(fullKey);
            }

            return this;
        }

        public T Resolve<T>(string key = null) where T : class
        {
            return (T)Resolve(typeof(T), key);
        }

        public object Resolve(Type type, string key = null)
        {
            var fullKey = GetFullKey(type, key);

            if (_singletonInstances.TryGetValue(fullKey, out var existing))
                return existing;

            if (!_registrations.TryGetValue(fullKey, out var registration))
            {
                if (!type.IsInterface && !type.IsAbstract)
                {
                    registration = new Registration
                    {
                        InterfaceType = type,
                        ImplementationType = type,
                        Lifetime = ServiceLifetime.Transient
                    };
                }
                else
                {
                    throw new InvalidOperationException($"Service '{fullKey}' is not registered.");
                }
            }

            var reg = (Registration)registration;
            var instance = CreateInstance(reg.ImplementationType);

            if (reg.Lifetime == ServiceLifetime.Singleton)
            {
                _singletonInstances[fullKey] = instance;
            }

            return instance;
        }

        public bool TryResolve<T>(string key, out T result) where T : class
        {
            result = null;
            var fullKey = GetFullKey<T>(key);

            if (_singletonInstances.TryGetValue(fullKey, out var existing))
            {
                result = (T)existing;
                return true;
            }

            if (!_registrations.TryGetValue(fullKey, out var registration))
                return false;

            var reg = (Registration)registration;
            var instance = CreateInstance(reg.ImplementationType);

            if (reg.Lifetime == ServiceLifetime.Singleton)
            {
                _singletonInstances[fullKey] = instance;
            }

            result = (T)instance;
            return true;
        }

        public void InjectInto(object instance)
        {
            if (instance == null) return;

            var type = instance.GetType();
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var fields = type.GetFields(bindingFlags)
                .Where(f => f.GetCustomAttribute<InjectAttribute>() != null ||
                           f.GetCustomAttribute<InjectOptionalAttribute>() != null);

            foreach (var field in fields)
            {
                InjectField(field, instance);
            }

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
                Debug.LogWarning($"Optional dependency '{field.Name}' not registered for '{instance.GetType().Name}'");
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
                Debug.LogWarning($"Optional dependency '{property.Name}' not registered for '{instance.GetType().Name}'");
            }
        }

        private object CreateInstance(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            
            var injectConstructor = constructors
                .FirstOrDefault(c => c.GetCustomAttribute<InjectAttribute>() != null);

            if (injectConstructor == null)
            {
                injectConstructor = constructors
                    .OrderByDescending(c => c.GetParameters().Length)
                    .FirstOrDefault();
            }

            if (injectConstructor == null)
            {
                throw new InvalidOperationException($"No suitable constructor found for type '{type.Name}'");
            }

            var parameters = injectConstructor.GetParameters();
            var parameterInstances = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                try
                {
                    parameterInstances[i] = Resolve(paramType);
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to resolve parameter '{parameters[i].Name}' of type '{paramType.Name}' in constructor of '{type.Name}'", ex);
                }
            }

            var instance = injectConstructor.Invoke(parameterInstances);
            InjectInto(instance);

            return instance;
        }

        private string GetFullKey<T>(string customKey)
        {
            return GetFullKey(typeof(T), customKey);
        }

        private string GetFullKey(Type type, string customKey)
        {
            var typeName = type.FullName;
            return string.IsNullOrEmpty(customKey) ? typeName : $"{typeName}:{customKey}";
        }

        public void Dispose()
        {
            foreach (var key in _disposables)
            {
                if (_singletonInstances.TryGetValue(key, out var instance) && instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _registrations.Clear();
            _singletonInstances.Clear();
            _disposables.Clear();
        }

        private class Registration
        {
            public Type InterfaceType { get; set; }
            public Type ImplementationType { get; set; }
            public ServiceLifetime Lifetime { get; set; }
            public object Instance { get; set; }
        }
    }
}
