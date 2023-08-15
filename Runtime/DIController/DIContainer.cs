using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MiniContainer
{
    public class DIContainer : IContainer
    {
        private ConstructorInfo _constructorInfo;
        private readonly List<Type> _ignoreTypeList;
        private readonly List<ConstructorInfo> _objectGraph;
        private readonly List<IRegistration> _registrations;
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<Type, DependencyObject>> _serviceDictionary;
        private ConcurrentDictionary<int, ConcurrentDictionary<Type, DependencyObject>> ServiceDictionary
        {
            get
            {
                RegistrationProcess();
                return _serviceDictionary;
            }
        }

        private static int _currentScopeID = -1;

        public DIContainer(List<IRegistration> registrations, List<Type> ignoreTypeList)
        {
            _serviceDictionary = new ConcurrentDictionary<int, ConcurrentDictionary<Type, DependencyObject>>();
            _objectGraph = new List<ConstructorInfo>();
            _ignoreTypeList = ignoreTypeList;
            _registrations = registrations;
            CreateScope();
        }

        public int GetCurrentScope()
        {
            return _currentScopeID;
        }
        
        public void SetCurrentScope(int scopeID)
        {
            if (ServiceDictionary.ContainsKey(scopeID))
            {
                _currentScopeID = scopeID;
            }
            else
            {
                ContainerDebug.InvalidOperation($"Scope with ID:{scopeID} doesn't exist");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CreateScope()
        {
            _currentScopeID++;
            _serviceDictionary[_currentScopeID] = new ConcurrentDictionary<Type, DependencyObject>();
            ContainerDebug.Log($"<color=green> New scope has been created with ID: {_currentScopeID}</color>");
            return _currentScopeID;
        }

        private void RegistrationProcess()
        {
            if (_registrations.Count > 0)
            {
                for (var i = 0; i < _registrations.Count; i++)
                {
                    for (var j = 0; j < _registrations[i].InterfaceTypes.Count; j++)
                    {
                        var interfaceType = _registrations[i].InterfaceTypes[j];
                        if (Any(interfaceType))
                        {
                            _registrations[i].InterfaceTypes.Remove(interfaceType);
                            j--;
                            continue;
                        }

                        var dependencyObject = GetDependencyObject(i, interfaceType);

                        if (dependencyObject == null) continue;
                        if (!_serviceDictionary[0].TryAdd(dependencyObject.ServiceType, dependencyObject))
                        {
                            ContainerDebug.InvalidOperation($"Already exist {dependencyObject.ServiceType}");
                        }
                    }
                }

                _registrations.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Any(Type interfaceType)
        {
            var any = false;
            for (var index = 0; index < _ignoreTypeList.Count; index++)
            {
                var t = _ignoreTypeList[index];
                if (t != interfaceType) continue;
                any = true;
                break;
            }

            return any;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DependencyObject GetDependencyObject(int i, Type interfaceType)
        {
            DependencyObject dependencyObject = null;

            switch (_registrations[i].RegistrationType)
            {
                case RegistrationType.Base:
                    dependencyObject = new DependencyObject(
                        interfaceType,
                        _registrations[i].ImplementationType,
                        _registrations[i].Implementation,
                        _registrations[i].LifeTime,
                        _registrations[i].InterfaceTypes);

                    break;
                case RegistrationType.Component:
                    dependencyObject = new ComponentDependencyObject(
                        interfaceType,
                        _registrations[i].ImplementationType,
                        _registrations[i].Implementation,
                        _registrations[i].LifeTime,
                        _registrations[i].InterfaceTypes,
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
                        _registrations[i].InterfaceTypes);
                    break;
            }

            return dependencyObject;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResolveInstanceRegistered()
        {
            foreach (var dependencyObject in ServiceDictionary[0])
            {
                if (dependencyObject.Value is InstanceRegistrationDependencyObject)
                {
                    ResolveObject(dependencyObject.Value.Implementation);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type serviceType)
        {
            if (IsComponentDependencyObject(serviceType, out var implementation))
            {
                return implementation;
            }
            
            var dependencyObject = ResolveType(serviceType);

            var impl = dependencyObject.Implementation;

            if (dependencyObject.LifeTime == ServiceLifeTime.Transient)
            {
                dependencyObject.Dispose();
            }

            return impl;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsComponentDependencyObject(Type serviceType, out Component implementation)
        {
            implementation = null;
            if (!ServiceDictionary[0].TryGetValue(serviceType, out var dependencyObject))
            {
                return false;
            }
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

                var parameters = SelectParameters(method.GetParameters());

                method.Invoke(implementation, parameters);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DependencyObject ResolveType(Type serviceType)
        {
            if (!ServiceDictionary[0].TryGetValue(serviceType, out var dependencyObject))
            {
                ConstructorInfo obj = null;
                if (_objectGraph.Count > 0)
                {
                    obj = _objectGraph[^1];
                }

                ContainerDebug.InvalidOperation(obj == null
                    ? $"There is no such a service {serviceType} registered"
                    : $"{obj.DeclaringType} tried to find {serviceType} but dependency is not found.");
            }

            if (dependencyObject != null)
            {
                if (TryToGetCachedImpl(dependencyObject, out var value))
                {
                    return value;
                }

                if (value != null && value.LifeTime == ServiceLifeTime.Scoped)
                {
                    dependencyObject = value;
                }

                var actualType = dependencyObject.ImplementationType;

                if (actualType.IsAbstract || actualType.IsInterface)
                {
                    ContainerDebug.InvalidOperation($"Cannot instantiate abstract classes or interfaces {actualType}");
                }

                if (actualType.GetConstructors().Length == 0)
                {
                    ContainerDebug.InvalidOperation($"{actualType} has no public constructors ");
                }

                _constructorInfo = GetConstructorInfo(actualType);
                if (_constructorInfo == null)
                {
                    _constructorInfo = actualType.GetConstructors()[0];
                }

                CheckCircularDependency();

                _objectGraph.Add(_constructorInfo);

                var parameters = SelectParameters(_constructorInfo.GetParameters());

                var objectActivator = Activator.GetActivator<object>(_constructorInfo);

                dependencyObject.Implementation = objectActivator(parameters);

                ResolveObject(dependencyObject.Implementation);
                if (dependencyObject.LifeTime != ServiceLifeTime.Transient)
                {
                    CheckInterfaces(dependencyObject);
                    TryToSetImplementationTypes(dependencyObject);
                }
            }

            _objectGraph.Clear();
            return dependencyObject;
        }

        private bool TryToGetCachedImpl(DependencyObject dependencyObject, out DependencyObject value)
        {
            switch (dependencyObject.LifeTime)
            {
                case ServiceLifeTime.Scoped:
                {
                    var scope = _serviceDictionary[_currentScopeID];
                    if (scope.TryGetValue(dependencyObject.ServiceType, out var scopedDependencyObject))
                    {
                        if (scopedDependencyObject.Implementation != null)
                        {
                            value = scopedDependencyObject;
                            return true;
                        }
                    }

                    scopedDependencyObject = new DependencyObject(dependencyObject);
                    scope[scopedDependencyObject.ServiceType] = scopedDependencyObject;
                    value = scopedDependencyObject;
                    return false;
                }
                case ServiceLifeTime.Singleton:
                {
                    if (dependencyObject.Implementation != null)
                    {
                        value = dependencyObject;
                        return true;
                    }
                    break;
                }                 
            }
            value = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object[] SelectParameters(ParameterInfo[] parameters)
        {
            var instances = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                instances[i] = ResolveType(p.ParameterType).Implementation;
            }

            return instances;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCircularDependency()
        {
            for (var i = 0; i < _objectGraph.Count; i++)
            {
                var c = _objectGraph[i];
                if (c.GetHashCode() != _constructorInfo.GetHashCode()) continue;
                ContainerDebug.InvalidOperation($"{_constructorInfo.DeclaringType} has circular dependency!");
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ConstructorInfo GetConstructorInfo(Type actualType)
        {
            ConstructorInfo first = null;
            for (var i = 0; i < actualType.GetConstructors().Length; i++)
            {
                var c = actualType.GetConstructors()[i];
                if (c.GetParameters().Length <= 0) continue;
                first = c;
                break;
            }

            return first;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryToSetImplementationTypes(DependencyObject dependencyObject)
        {
            if (dependencyObject.LifeTime == ServiceLifeTime.Transient)
            {
                return;
            }

            for (var i = 0; i < dependencyObject.InterfaceTypes.Count; i++)
            {
                var serviceType = dependencyObject.InterfaceTypes[i];
                switch (dependencyObject.LifeTime)
                {
                    case ServiceLifeTime.Singleton:
                        ServiceDictionary[0][serviceType].Implementation ??= dependencyObject.Implementation;
                        break;
                    case ServiceLifeTime.Scoped:
                        if (!ServiceDictionary[_currentScopeID]
                                .TryGetValue(serviceType, out var scopedDependencyObject))
                        {
                            dependencyObject.ServiceType = serviceType;
                            scopedDependencyObject = new DependencyObject(dependencyObject);
                            ServiceDictionary[_currentScopeID].TryAdd(serviceType, scopedDependencyObject);
                        }

                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckInterfaces(DependencyObject dependencyObject)
        {
            var dependencyObjectListeners = dependencyObject.Listeners;
            var implementation = dependencyObject.Implementation;
            if (implementation is IContainerUpdateListener containerUpdate)
            {
                dependencyObjectListeners.ContainerUpdate = containerUpdate;
            }

            if (implementation is IDisposable disposable)
            {
                dependencyObject.Disposable = disposable;
            }

            switch (implementation)
            {
                case IContainerSceneLoadedListener containerSceneLoaded:
                    dependencyObjectListeners.ContainerSceneLoaded = containerSceneLoaded;
                    break;
                case IContainerSceneUnloadedListener containerSceneUnloaded:
                    dependencyObjectListeners.ContainerSceneUnloaded = containerSceneUnloaded;
                    break;
                case IContainerApplicationFocusListener containerApplicationFocus:
                    dependencyObjectListeners.ContainerApplicationFocus = containerApplicationFocus;
                    break;
                case IContainerApplicationPauseListener containerApplicationPause:
                    dependencyObjectListeners.ContainerApplicationPause = containerApplicationPause;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseScope(int scopeID)
        {
            if (scopeID <= 0)
            {
                ContainerDebug.Log("<color=yellow>You can't release the main scope</color>");
                return;
            }

            if (_serviceDictionary.TryGetValue(scopeID, out var scope))
            {
                foreach (var scopedDependencyObject in scope)
                {
                    scopedDependencyObject.Value.Dispose();
                }

                scope.Clear();
                ContainerDebug.Log($"<color=green> Scope has been released with ID {scopeID}</color>");
            }

            _currentScopeID = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseAll()
        {
            foreach (var scopes in ServiceDictionary)
            {
                foreach (var scope in scopes.Value)
                {
                    if (scope.Value.Implementation == null
                        || scope.Value.Implementation is MonoBehaviour)
                    {
                        continue;
                    }

                    scope.Value.Dispose();
                }
            }

            ServiceDictionary.Clear();
            _objectGraph.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunUpdate()
        {
            foreach (var scopes in ServiceDictionary)
            {
                foreach (var scope in scopes.Value)
                {
                    scope.Value.Listeners.ContainerUpdate?.Update();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunSceneLoaded(int scene)
        {
            foreach (var scopes in ServiceDictionary)
            {
                foreach (var scope in scopes.Value)
                {
                    scope.Value.Listeners.ContainerSceneLoaded?.OnSceneLoaded(scene);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunSceneUnloaded(int scene)
        {
            foreach (var scopes in ServiceDictionary)
            {
                foreach (var scope in scopes.Value)
                {
                    scope.Value.Listeners.ContainerSceneUnloaded?.OnSceneUnloaded(scene);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunApplicationFocus(bool focus)
        {
            foreach (var scopes in ServiceDictionary)
            {
                foreach (var scope in scopes.Value)
                {
                    scope.Value.Listeners.ContainerApplicationFocus?.OnApplicationFocus(focus);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunApplicationPause(bool pause)
        {
            foreach (var scopes in ServiceDictionary)
            {
                foreach (var scope in scopes.Value)
                {
                    scope.Value.Listeners.ContainerApplicationPause?.OnApplicationPause(pause);
                }
            }
        }
    }
}