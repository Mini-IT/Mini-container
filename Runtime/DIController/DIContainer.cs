using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MiniContainer
{
    public class DIContainer : IContainer
    {
        private readonly List<Type> _ignoreTypeList;

        private ConcurrentDictionary<Type, DependencyObject> _serviceDictionary;

        private ConcurrentDictionary<Type, DependencyObject> ServiceDictionary
        {
            get
            {
                _serviceDictionary ??= new ConcurrentDictionary<Type, DependencyObject>();

                if (_registrations.Count > 0)
                {
                    for (var i = 0; i < _registrations.Count; i++)
                    {
                        for (var j = 0; j < _registrations[i].InterfaceTypes.Count; j++)
                        {
                            var interfaceType = _registrations[i].InterfaceTypes[j];
                            if (_ignoreTypeList.Any(t => t == interfaceType))
                            {
                                _registrations[i].InterfaceTypes.Remove(interfaceType);
                                j--;
                                continue;
                            }

                            DependencyObject dependencyObject = null;
                            switch (_registrations[i].RegistrationType)
                            {
                                case RegistrationType.Base:
                                    dependencyObject = new DependencyObject(
                                        interfaceType,
                                        _registrations[i].ImplementationType,
                                        _registrations[i].Implementation,
                                        _registrations[i].LifeTime,
                                        _registrations[i].InterfaceTypes,
                                        _registrations[i].OnSceneDestroyRelease);

                                    break;
                                case RegistrationType.Component:
                                    dependencyObject = new ComponentDependencyObject(
                                        interfaceType,
                                        _registrations[i].ImplementationType,
                                        _registrations[i].Implementation,
                                        _registrations[i].LifeTime,
                                        _registrations[i].InterfaceTypes,
                                        _registrations[i].OnSceneDestroyRelease,
                                        _registrations[i].Prefab,
                                        _registrations[i].Parent,
                                        _registrations[i].GameObjectName);
                                    break;
                                case RegistrationType.Instance:
                                    dependencyObject = new InstanceRegistrationDependencyObject(
                                        interfaceType,
                                        _registrations[i].ImplementationType,
                                        _registrations[i].Implementation,
                                        _registrations[i].LifeTime,
                                        _registrations[i].InterfaceTypes,
                                        _registrations[i].OnSceneDestroyRelease);
                                    break;
                            }

                            if (dependencyObject == null) continue;
                            if (!_serviceDictionary.TryAdd(dependencyObject.ServiceType, dependencyObject))
                            {
                                throw new Exception($"Already exist {dependencyObject.ServiceType}");
                            }
                        }
                    }

                    _registrations.Clear();
                }

                return _serviceDictionary;
            }
        }

        private readonly List<ConstructorInfo> _objectGraph;
        private readonly List<IRegistration> _registrations;

        public DIContainer(List<IRegistration> registrations, List<Type> ignoreTypeList)
        {
            _objectGraph = new List<ConstructorInfo>();
            _ignoreTypeList = ignoreTypeList;
            _registrations = registrations;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResolveInstanceRegistered(bool onSceneDestroyRelease)
        {
            foreach (var dependencyObject in ServiceDictionary)
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

            if (IsComponentDependencyObject(dependencyObject, out var implementation))
            {
                return implementation;
            }
            
            var impl = dependencyObject.GetOrSetInstance;

            if (!dependencyObject.IsResolved)
            {
                ResolveObject(impl);
            }

            if (dependencyObject.LifeTime == ServiceLifeTime.Singleton)
            {
                dependencyObject.IsResolved = true;
            }

            return impl;
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
            var properties = implementation.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var attr = property.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var propertyImpl = ResolveType(property.PropertyType).Implementation;
                property.SetValue(implementation, propertyImpl);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveField(object implementation)
        {
            var fields = implementation.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var fieldImpl = ResolveType(field.FieldType).Implementation;
                field.SetValue(implementation, fieldImpl);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveMethod(object implementation)
        {
            var methods = implementation.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var parameters = method.GetParameters().Select(p => ResolveType(p.ParameterType).Implementation)
                    .ToArray();

                method.Invoke(implementation, parameters);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetInstance(Type serviceType)
        {
            return !ServiceDictionary.TryGetValue(serviceType, out var dependencyObject)
                ? null
                : dependencyObject.Implementation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DependencyObject ResolveType(Type serviceType)
        {
            if (!ServiceDictionary.TryGetValue(serviceType, out var dependencyObject))
            {
                var obj = _objectGraph.LastOrDefault();
                if (obj == null)
                {
                    throw new Exception($"There is no such a service {serviceType} registered");
                }

                throw new Exception($"{obj.DeclaringType} tried to find {serviceType} but dependency is not found.");
            }

            if (dependencyObject is ComponentDependencyObject)
            {
                return dependencyObject;
            }

            if (TryToGetSingletonImpl(dependencyObject))
            {
                return dependencyObject;
            }

            var actualType = dependencyObject.ImplementationType;

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

            if (_objectGraph.Any(c => c.GetHashCode() == constructorInfo.GetHashCode()))
            {
                throw new Exception($"{constructorInfo.DeclaringType} has circular dependency!");
            }

            _objectGraph.Add(constructorInfo);

            var parameters = constructorInfo.GetParameters().Select(p => ResolveType(p.ParameterType).GetOrSetInstance)
                .ToArray();

            var implementation = Activator.CreateInstance(actualType, parameters);

            dependencyObject.GetOrSetInstance = implementation;

            CheckInterfaces(implementation, dependencyObject);
            dependencyObject.WeakReferenceCount++;
            _objectGraph.Clear();
            return dependencyObject;
        }

        private bool TryToGetSingletonImpl(DependencyObject dependencyObject)
        {
            if (dependencyObject.LifeTime != ServiceLifeTime.Singleton)
            {
                return false;
            }

            if (dependencyObject.Implementation != null)
            {
                return true;
            }

            for (var i = 0; i < dependencyObject.InterfaceTypes.Count; i++)
            {
                var impl = ServiceDictionary[dependencyObject.InterfaceTypes[i]].Implementation;
                if (impl != null)
                {
                    for (var j = 0; j < dependencyObject.InterfaceTypes.Count; j++)
                    {
                        ServiceDictionary[dependencyObject.InterfaceTypes[j]].Implementation = impl;
                    }

                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckInterfaces(object implementation, DependencyObject dependencyObject)
        {
            if (implementation is IContainerUpdateListener containerUpdate)
            {
                dependencyObject.GetListeners().ContainerUpdate =
                    new WeakReference<IContainerUpdateListener>(containerUpdate);
            }

            if (implementation is IDisposable disposable)
            {
                dependencyObject.Disposable = new WeakReference<IDisposable>(disposable);
            }

            switch (implementation)
            {
                case IContainerSceneLoadedListener containerSceneLoaded:
                    dependencyObject.GetListeners().ContainerSceneLoaded =
                        new WeakReference<IContainerSceneLoadedListener>(containerSceneLoaded);
                    break;
                case IContainerSceneUnloadedListener containerSceneUnloaded:
                    dependencyObject.GetListeners().ContainerSceneUnloaded =
                        new WeakReference<IContainerSceneUnloadedListener>(containerSceneUnloaded);
                    break;
                case IContainerApplicationFocusListener containerApplicationFocus:
                    dependencyObject.GetListeners().ContainerApplicationFocus =
                        new WeakReference<IContainerApplicationFocusListener>(containerApplicationFocus);
                    break;
                case IContainerApplicationPauseListener containerApplicationPause:
                    dependencyObject.GetListeners().ContainerApplicationPause =
                        new WeakReference<IContainerApplicationPauseListener>(containerApplicationPause);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(Type type)
        {
            if (!ServiceDictionary.TryGetValue(type, out var value))
            {
                return;
            }
            if (value.Implementation == null)
            {
                return;
            }

            ReleaseObject(value);
            ServiceDictionary.TryRemove(type, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseObject(DependencyObject dependencyObject)
        {
            foreach (var service in ServiceDictionary)
            {
                if (service.Value.Implementation == null)
                {
                    continue;
                }

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

            if (dependencyObject.Disposable != null &&
                dependencyObject.Disposable.TryGetTarget(out var disposable))
            {
                disposable.Dispose();
            }

            dependencyObject.Implementation = null;
            dependencyObject.IsResolved = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseAll()
        {
            foreach (var dependencyObject in ServiceDictionary)
            {
                if (dependencyObject.Value.Implementation == null
                    || dependencyObject.Value.Implementation is MonoBehaviour)
                {
                    continue;
                }

                if (dependencyObject.Value.Disposable != null &&
                    dependencyObject.Value.Disposable.TryGetTarget(out var disposable))
                {
                    disposable.Dispose();
                }

                dependencyObject.Value.Implementation = null;
                dependencyObject.Value.IsResolved = false;
            }

            ServiceDictionary.Clear();
            _objectGraph.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseScene()
        {
            foreach (var dependencyObject in ServiceDictionary)
            {
                if (dependencyObject.Value.Implementation == null
                    || dependencyObject.Value.Implementation is MonoBehaviour
                    || !dependencyObject.Value.OnSceneDestroyRelease)
                {
                    continue;
                }

                ReleaseObject(dependencyObject.Value);
                ServiceDictionary.TryRemove(dependencyObject.Key, out _);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunUpdate()
        {
            foreach (var dependencyObject in ServiceDictionary)
            {
                foreach (var weakReference in dependencyObject.Value.WeakReferenceMap)
                {
                    if (weakReference.Value.Target != null)
                    {
                        continue;
                    }

                    dependencyObject.Value.WeakReferenceMap.TryRemove(weakReference.Key, out var _);
                    dependencyObject.Value.ListenersMap.TryRemove(weakReference.Key, out var _);
                }

                foreach (var listeners in dependencyObject.Value.ListenersMap)
                {
                    if (listeners.Value.ContainerUpdate != null
                        && listeners.Value.ContainerUpdate.TryGetTarget(out var target))
                    {
                        target.Update();
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunSceneLoaded(int scene)
        {
            foreach (var dependencyObject in ServiceDictionary)
            {
                foreach (var listeners in dependencyObject.Value.ListenersMap)
                {
                    if (listeners.Value.ContainerSceneLoaded != null &&
                        listeners.Value.ContainerSceneLoaded.TryGetTarget(out var target))
                    {
                        target.OnSceneLoaded(scene);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunSceneUnloaded(int scene)
        {
            foreach (var dependencyObject in ServiceDictionary)
            {
                foreach (var listeners in dependencyObject.Value.ListenersMap)
                {
                    if (listeners.Value.ContainerSceneUnloaded != null &&
                        listeners.Value.ContainerSceneUnloaded.TryGetTarget(out var target))
                    {
                        target.OnSceneUnloaded(scene);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunApplicationFocus(bool focus)
        {
            foreach (var dependencyObject in ServiceDictionary)
            {
                foreach (var listeners in dependencyObject.Value.ListenersMap)
                {
                    if (listeners.Value.ContainerApplicationFocus != null &&
                        listeners.Value.ContainerApplicationFocus.TryGetTarget(out var target))
                    {
                        target.OnApplicationFocus(focus);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunApplicationPause(bool pause)
        {
            foreach (var dependencyObject in ServiceDictionary)
            {
                foreach (var listeners in dependencyObject.Value.ListenersMap)
                {
                    if (listeners.Value.ContainerApplicationPause != null &&
                        listeners.Value.ContainerApplicationPause.TryGetTarget(out var target))
                    {
                        target.OnApplicationPause(pause);
                    }
                }
            }
        }
    }
}