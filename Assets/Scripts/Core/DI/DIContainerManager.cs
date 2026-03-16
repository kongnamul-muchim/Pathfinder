using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pathfinder.Core.DI
{
    public static class DIContainerManager
    {
        private static readonly List<DIContainer> _containers = new();
        private static bool _initialized = false;

        public static DIContainer Global { get; private set; }
        public static DIContainer CurrentSceneContainer { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            if (_initialized) return;

            Global = new DIContainer();
            _containers.Add(Global);
            _initialized = true;

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            Debug.Log("[DIContainerManager] Global container initialized");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;

            var container = new DIContainer();
            CurrentSceneContainer = container;
            _containers.Add(container);

            Debug.Log($"[DIContainerManager] Scene container created for: {scene.name}");

            ExecuteInstallers(container);
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            if (_containers.Count <= 1) return;

            var lastContainer = _containers.Last();
            if (lastContainer != Global)
            {
                lastContainer.Dispose();
                _containers.RemoveAt(_containers.Count - 1);
                CurrentSceneContainer = _containers.Count > 1 ? _containers[_containers.Count - 1] : null;

                Debug.Log($"[DIContainerManager] Scene container disposed for: {scene.name}");
            }
        }

        private static void ExecuteInstallers(DIContainer container)
        {
            var rootContexts = UnityEngine.Object.FindObjectsOfType<RootContext>();

            if (rootContexts.Length == 0)
            {
                Debug.LogWarning("[DIContainerManager] No RootContext found in scene. Installers will be auto-collected.");
                AutoCollectAndExecuteInstallers(container);
                return;
            }

            foreach (var rootContext in rootContexts.OrderBy(r => r.ExecutionOrder))
            {
                rootContext.ExecuteInstallers(container);
            }
        }

        private static void AutoCollectAndExecuteInstallers(DIContainer container)
        {
            var installers = UnityEngine.Object.FindObjectsOfType<Installer>();

            foreach (var installer in installers.OrderBy(i => i.Priority))
            {
                try
                {
                    installer.Install(container);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DIContainerManager] Installer failed: {installer.GetType().Name}\n{ex}");
                }
            }
        }

        public static T Resolve<T>(string key = null) where T : class
        {
            EnsureInitialized();

            var fullKey = GetFullKey<T>(key);

            for (int i = _containers.Count - 1; i >= 0; i--)
            {
                if (_containers[i].TryResolve<T>(fullKey, out var result))
                {
                    return result;
                }
            }

            throw new InvalidOperationException($"Service not found: {fullKey}");
        }

        public static object Resolve(Type type, string key = null)
        {
            EnsureInitialized();

            var fullKey = GetFullKey(type, key);

            for (int i = _containers.Count - 1; i >= 0; i--)
            {
                if (_containers[i].TryResolve(fullKey, out var result))
                {
                    return result;
                }
            }

            throw new InvalidOperationException($"Service not found: {fullKey}");
        }

        public static bool TryResolve<T>(string key, out T result) where T : class
        {
            EnsureInitialized();

            var fullKey = GetFullKey<T>(key);

            for (int i = _containers.Count - 1; i >= 0; i--)
            {
                if (_containers[i].TryResolve<T>(fullKey, out result))
                {
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static void InjectInto(object instance)
        {
            EnsureInitialized();

            if (instance == null)
            {
                Debug.LogWarning("[DIContainerManager] Cannot inject into null instance");
                return;
            }

            for (int i = _containers.Count - 1; i >= 0; i--)
            {
                _containers[i].InjectInto(instance);
            }
        }

        public static void InjectInto(GameObject gameObject)
        {
            if (gameObject == null) return;

            var components = gameObject.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var component in components)
            {
                InjectInto(component);
            }
        }

        public static void ClearGlobal()
        {
            Global?.Clear();
            Debug.Log("[DIContainerManager] Global container cleared");
        }

        public static void ClearAll()
        {
            foreach (var container in _containers)
            {
                container.Dispose();
            }
            _containers.Clear();
            Global = new DIContainer();
            _containers.Add(Global);
            CurrentSceneContainer = null;

            Debug.Log("[DIContainerManager] All containers cleared");
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private static string GetFullKey<T>(string customKey)
        {
            return GetFullKey(typeof(T), customKey);
        }

        private static string GetFullKey(Type type, string customKey)
        {
            var typeName = type.FullName;
            return string.IsNullOrEmpty(customKey) ? typeName : $"{typeName}:{customKey}";
        }
    }
}
