using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MiniContainer
{
    public class DIContainer : IContainer
    {
        private readonly List<ConstructorInfo> _objectGraph;
        private readonly ConcurrentDictionary<Type, DependencyObject> _serviceDictionary;

        public DIContainer(ConcurrentDictionary<Type, DependencyObject> serviceDictionary)
        {
            _objectGraph = new List<ConstructorInfo>();
            _serviceDictionary = serviceDictionary;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResolveInstanceRegistered(bool onSceneDestroyRelease)
        {
            foreach (var dependencyObject in _serviceDictionary)
            {
                if (dependencyObject.Value is InstanceRegistrationDependencyObject
                    && dependencyObject.Value.OnSceneDestroyRelease == onSceneDestroyRelease)
                {
                    ResolveObject(dependencyObject.Value.Implementation);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type serviceType)
        {
            var dependencyObject = ResolveType(serviceType);
            if (IsComponentDependencyObject(dependencyObject, out var implementation)) return implementation;

            ResolveObject(dependencyObject.Implementation);
            return dependencyObject.Implementation;
        }

        private bool IsComponentDependencyObject(DependencyObject dependencyObject, out Component implementation)
        {
            implementation = null;
            if (dependencyObject is ComponentDependencyObject componentDependencyObject)
            {
                if (componentDependencyObject.Prefab == null)
                {
                    var name = string.IsNullOrEmpty(componentDependencyObject.GameObjectName)
                        ? componentDependencyObject.ServiceType.Name
                        : componentDependencyObject.GameObjectName; 
                    
                    var go = new GameObject(name);
                    go.SetActive(false);

                    var parent = componentDependencyObject.Parent;
                    if (parent != null)
                    {
                        go.transform.SetParent(parent);
                    }
                    implementation = go.AddComponent(componentDependencyObject.ServiceType);
                    ResolveObject(implementation);
                    go.SetActive(true);
                }
                else
                {
                    var wasActive = componentDependencyObject.Prefab.gameObject.activeSelf;
                    if (wasActive)
                    {
                        componentDependencyObject.Prefab.gameObject.SetActive(false);
                    }

                    implementation = componentDependencyObject.Parent == null
                        ? UnityEngine.Object.Instantiate(componentDependencyObject.Prefab)
                        : UnityEngine.Object.Instantiate(componentDependencyObject.Prefab,
                            componentDependencyObject.Parent);

                    ResolveObject(implementation);

                    if (wasActive)
                    {
                        implementation.gameObject.SetActive(true);
                        componentDependencyObject.Prefab.gameObject.SetActive(true);
                    }
                }

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResolveObject(object implementation)
        {
            ResolveField(implementation);
            ResolveProperty(implementation);
            ResolveMethod(implementation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveProperty(object implementation)
        {
            var properties = implementation.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var attr = property.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null) continue;

                var propertyImpl = ResolveType(property.PropertyType).Implementation;
                property.SetValue(implementation, propertyImpl);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveField(object implementation)
        {
            var fields = implementation.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null) continue;

                var fieldImpl = ResolveType(field.FieldType).Implementation;
                field.SetValue(implementation, fieldImpl);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveMethod(object implementation)
        {
            var methods = implementation.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null) continue;

                var parameters = method.GetParameters().Select(p => ResolveType(p.ParameterType).Implementation).ToArray();

                method.Invoke(implementation, parameters);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetInstance(Type serviceType)
        {
            return !_serviceDictionary.TryGetValue(serviceType, out var dependencyObject) ? null : dependencyObject.Implementation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DependencyObject ResolveType(Type serviceType)
        {
            if (!_serviceDictionary.TryGetValue(serviceType, out var dependencyObject))
            {
                throw new Exception($"Service of type {serviceType} is not found");
            }

            if (dependencyObject is ComponentDependencyObject)
            {
                return dependencyObject;
            }

            if (dependencyObject.LifeTime == ServiceLifeTime.Singleton)
            {
                if (dependencyObject.Implementation != null) return dependencyObject;
            }
            else
            {
                dependencyObject.Disposable?.Dispose();
                dependencyObject.Implementation = null;
            }

            var actualType = dependencyObject.ImplementationType ?? dependencyObject.ServiceType;

            if (actualType.IsAbstract || actualType.IsInterface)
            {
                throw new Exception($"Cannot instantiate abstract classes or interfaces {actualType}");
            }

            if (actualType.GetConstructors().Length == 0)
            {
                throw new Exception($"{actualType} has no public constructors ");
            }

            var constructorInfo = actualType.GetConstructors().FirstOrDefault(c => c.GetParameters().Length > 0);
            if (constructorInfo == null)
            {
                constructorInfo = actualType.GetConstructors().First();
            }

            try
            {
                if (_objectGraph.Any(c => c.GetHashCode() == constructorInfo.GetHashCode()))
                    throw new Exception($"{constructorInfo.DeclaringType} has circular dependency!");

                _objectGraph.Add(constructorInfo);

                var parameters = constructorInfo.GetParameters().Select(p => ResolveType(p.ParameterType).Implementation).ToArray();

                var implementation = Activator.CreateInstance(actualType, parameters);
                CheckInterfaces(implementation, dependencyObject);
                dependencyObject.Implementation = implementation;

                _objectGraph.Clear();
                return dependencyObject;
            }
            catch (Exception e)
            {
                throw new Exception($"{e.Message}");
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckInterfaces(object implementation, DependencyObject dependencyObject)
        {
            if (implementation is IContainerUpdateListener containerUpdate)
            {
                dependencyObject.ContainerUpdate = containerUpdate;
            }
            if (implementation is IContainerSceneLoadedListener containerSceneLoaded)
            {
                dependencyObject.ContainerSceneLoaded = containerSceneLoaded;
            }
            if (implementation is IContainerSceneUnloadedListener containerSceneUnloaded)
            {
                dependencyObject.ContainerSceneUnloaded = containerSceneUnloaded;
            }
            if (implementation is IContainerApplicationFocusListener containerApplicationFocus)
            {
                dependencyObject.ContainerApplicationFocus = containerApplicationFocus;
            }
            if (implementation is IContainerApplicationPauseListener containerApplicationPause)
            {
                dependencyObject.ContainerApplicationPause = containerApplicationPause;
            }
            if (implementation is IDisposable disposable)
            {
                dependencyObject.Disposable = disposable;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(Type type)
        {

            if (!_serviceDictionary.TryGetValue(type, out var value)) return;
            if (value.Implementation == null) return;
            ReleaseObject(value);
            _serviceDictionary.TryRemove(type, out _);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseObject(DependencyObject dependencyObject)
        {
            foreach (var service in _serviceDictionary)
            {
                if (service.Value.Implementation == null) continue;
                var fields = service.Value.Implementation.GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.FieldType == dependencyObject.ServiceType)
                    {
                        field.SetValue(service.Value.Implementation, null);
                    }
                }
            }

            dependencyObject.Disposable?.Dispose();
            dependencyObject.Implementation = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseAll()
        {

            foreach (var dependencyObject in _serviceDictionary)
            {
                if (dependencyObject.Value.Implementation == null || dependencyObject.Value.Implementation is MonoBehaviour)
                    continue;

                dependencyObject.Value.Disposable?.Dispose();
                dependencyObject.Value.Implementation = null;
            }
            _serviceDictionary.Clear();


            _objectGraph.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseScene()
        {
            foreach (var dependencyObject in _serviceDictionary)
            {
                if (dependencyObject.Value.Implementation == null || dependencyObject.Value.Implementation is MonoBehaviour || !dependencyObject.Value.OnSceneDestroyRelease)
                    continue;

                ReleaseObject(dependencyObject.Value);
                _serviceDictionary.TryRemove(dependencyObject.Key, out _);
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunUpdate()
        {
            foreach (var dependencyObject in _serviceDictionary)
            {
                dependencyObject.Value.ContainerUpdate?.Update();
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunSceneLoaded(int scene)
        {
            foreach (var dependencyObject in _serviceDictionary)
            {
                dependencyObject.Value.ContainerSceneLoaded?.OnSceneLoaded(scene);
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunSceneUnloaded(int scene)
        {
            foreach (var dependencyObject in _serviceDictionary)
            {
                dependencyObject.Value.ContainerSceneUnloaded?.OnSceneUnloaded(scene);
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunApplicationFocus(bool focus)
        {
            foreach (var dependencyObject in _serviceDictionary)
            {
                dependencyObject.Value.ContainerApplicationFocus?.OnApplicationFocus(focus);
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunApplicationPause(bool pause)
        {
            foreach (var dependencyObject in _serviceDictionary)
            {
                dependencyObject.Value.ContainerApplicationPause?.OnApplicationPause(pause);
            }

        }
    }
}