using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace MiniContainer
{
    public class DIContainer : IContainer, IContainerLifeCycle
    {
        private readonly List<Type> _ignoreTypeList;
        private readonly HashSet<int> _objectGraphHashCodes;
        private readonly List<IRegistration> _registrations;
        private readonly ConcurrentDictionary<int, Dictionary<Type, DependencyObject>> _serviceDictionary;
        private readonly object _dictionaryLock = new object();
        private readonly ConcurrentDictionary<Type, FieldInfo[]> _fieldCache;
        private readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache;
        private readonly ConcurrentDictionary<Type, MethodInfo[]> _methodCache;
        private readonly ConcurrentDictionary<Type, ConstructorInfo[]> _constructorCache;
        private readonly bool _enableParallelInitialization;
        private ConstructorInfo _constructorInfo;

        public event Action OnContainerUpdate;
        public event Action<int> OnContainerSceneLoaded;
        public event Action<int> OnContainerSceneUnloaded;
        public event Action<bool> OnContainerApplicationFocus;
        public event Action<bool> OnContainerApplicationPause;
        
        private ConcurrentDictionary<int, Dictionary<Type, DependencyObject>> ServiceDictionary
        {
            get
            {
                RegistrationProcess();
                return _serviceDictionary;
            }
        }

        private int _currentScopeID = -1;
  
        public DIContainer(List<IRegistration> registrations, List<Type> ignoreTypeList, bool enableParallelInitialization = true)
        {
            _serviceDictionary = new ConcurrentDictionary<int, Dictionary<Type, DependencyObject>>();
            _fieldCache = new ConcurrentDictionary<Type, FieldInfo[]>(4, 128);
            _propertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>(4, 128);
            _methodCache = new ConcurrentDictionary<Type, MethodInfo[]>(4, 128);
            _constructorCache = new ConcurrentDictionary<Type, ConstructorInfo[]>(4, 128);
            _objectGraphHashCodes = new HashSet<int>(16);
            _ignoreTypeList = ignoreTypeList;
            _registrations = registrations;
            _enableParallelInitialization = enableParallelInitialization;
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
                Logs.InvalidOperation($"Scope with ID:{scopeID} doesn't exist");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CreateScope()
        {
            _currentScopeID++;
            var newScope = new Dictionary<Type, DependencyObject>(32);
            _serviceDictionary[_currentScopeID] = newScope;
            Logs.Log($"<color=green> New scope has been created with ID: {_currentScopeID}</color>");
            return _currentScopeID;
        }

        private void RegistrationProcess()
        {
            if (_registrations.Count <= 0)
            {
                return;
            }
            
            if (!_serviceDictionary.TryGetValue(0, out var mainScope))
            {
                var estimatedCapacity = _registrations.Count * 3;
                mainScope = new Dictionary<Type, DependencyObject>(estimatedCapacity);
                _serviceDictionary[0] = mainScope;
            }
            
            var totalInterfaces = 0;
            for (var i = 0; i < _registrations.Count; i++)
            {
                var registration = _registrations[i];
                totalInterfaces += registration.InterfaceTypes.Count;
            }
            
            var targetCapacity = Math.Max(totalInterfaces, mainScope.Count + _registrations.Count * 2);
            if (mainScope.Count < targetCapacity)
            {
                mainScope.EnsureCapacity(targetCapacity);
            }
            
            for (var i = 0; i < _registrations.Count; i++)
            {
                var registration = _registrations[i];
                for (var j = 0; j < registration.InterfaceTypes.Count; j++)
                {
                    var interfaceType = registration.InterfaceTypes[j];
                    if (IsIgnoreType(interfaceType))
                    {
                        registration.InterfaceTypes.RemoveAt(j);
                        j--;
                        continue;
                    }

                    var dependencyObject = GetDependencyObject(i, interfaceType);

                    if (dependencyObject == null) continue;
                    
                    lock (_dictionaryLock)
                    {
                        if (!mainScope.TryAdd(dependencyObject.ServiceType, dependencyObject))
                        {
                            Logs.InvalidOperation($"Already exist {dependencyObject.ServiceType}");
                        }
                    }
                }
            }

            _registrations.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsIgnoreType(Type interfaceType)
        {
            var count = _ignoreTypeList.Count;
            for (var i = 0; i < count; i++)
            {
                if (_ignoreTypeList[i] == interfaceType)
                {
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DependencyObject GetDependencyObject(int i, Type interfaceType)
        {
            var registration = _registrations[i];
            DependencyObject dependencyObject = null;

            switch (registration.RegistrationType)
            {
                case RegistrationType.Base:
                    dependencyObject = new DependencyObject(
                        interfaceType,
                        registration.ImplementationType,
                        registration.Implementation,
                        registration.LifeTime,
                        registration.InterfaceTypes,
                        registration.GetImplementation);

                    break;
                case RegistrationType.Component:
                    dependencyObject = new ComponentDependencyObject(
                        interfaceType,
                        registration.ImplementationType,
                        registration.Implementation,
                        registration.LifeTime,
                        registration.InterfaceTypes,
                        registration.Prefab,
                        registration.Parent,
                        registration.GameObjectName);
                    break;
                case RegistrationType.Instance:
                    dependencyObject = new InstanceRegistrationDependencyObject(
                        interfaceType,
                        registration.ImplementationType,
                        registration.Implementation,
                        registration.LifeTime,
                        registration.InterfaceTypes);
                    break;
            }

            return dependencyObject;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResolveInstanceRegistered()
        {
            var mainScope = ServiceDictionary[0];
            
            if (!_enableParallelInitialization)
            {
                foreach (var dependencyObject in mainScope.Values)
                {
                    if (dependencyObject is InstanceRegistrationDependencyObject)
                    {
                        ResolveObject(dependencyObject.Implementation);
                    }
                }
                return;
            }
            
            var instanceObjects = new List<object>();
            foreach (var dependencyObject in mainScope.Values)
            {
                if (dependencyObject is InstanceRegistrationDependencyObject)
                {
                    instanceObjects.Add(dependencyObject.Implementation);
                }
            }
            
            if (instanceObjects.Count > 0)
            {
                Parallel.ForEach(instanceObjects, ResolveObject);
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
                dependencyObject.Implementation = null;
            }

            return impl;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsComponentDependencyObject(Type serviceType, out Component implementation)
        {
            implementation = null;
            var mainScope = ServiceDictionary[0];
            
            if (!mainScope.TryGetValue(serviceType, out var dependencyObject))
            {
                return false;
            }

            if (dependencyObject is not ComponentDependencyObject componentDependencyObject)
            {
                return false;
            }
            
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
            var type = implementation.GetType();
            var properties = _propertyCache.GetOrAdd(type, t => 
                t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

            foreach (var property in properties)
            {
                var attr = property.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var propertyImpl = ResolveType(property.PropertyType).Implementation;
                var setter = Activator.GetPropertySetter(property);
                setter(implementation, propertyImpl);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveField(object implementation)
        {
            var type = implementation.GetType();
            var fields = _fieldCache.GetOrAdd(type, t => 
                t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var fieldImpl = ResolveType(field.FieldType).Implementation;
                var setter = Activator.GetFieldSetter(field);
                setter(implementation, fieldImpl);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveMethod(object implementation)
        {
            var type = implementation.GetType();
            var methods = _methodCache.GetOrAdd(type, t => 
                t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var parameters = SelectParameters(method.GetParameters());
                var invoker = Activator.GetMethodInvoker(method);
                invoker(implementation, parameters);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DependencyObject ResolveType(Type serviceType)
        {
            var dependencyObject = TryGetDependencyObject(serviceType);

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
                
                if (dependencyObject.TryGetImplementation(out var implementation))
                {
                    dependencyObject.Implementation = implementation;
                }
                else
                {
                    var actualType = dependencyObject.ImplementationType;

                    if (actualType.IsAbstract || actualType.IsInterface)
                    {
                        Logs.InvalidOperation($"Cannot instantiate abstract classes or interfaces {actualType}");
                    }
                    
                    _constructorInfo = GetConstructorInfo(actualType);
                
                    if (_constructorInfo != null)
                    {
                        CheckCircularDependency();
                        _objectGraphHashCodes.Add(_constructorInfo.GetHashCode());
                
                        var objectActivator = Activator.GetActivator(_constructorInfo);
                        var parameters = SelectParameters(_constructorInfo.GetParameters());
                        dependencyObject.Implementation = objectActivator.Invoke(parameters);
                    }
                    else
                    {
                        dependencyObject.Implementation = Activator.CreateDefaultConstructor(actualType).Invoke();
                    }
                }
                
                ResolveObject(dependencyObject.Implementation);
                CheckDisposableInterface(dependencyObject);
                SetImplementationTypes(dependencyObject);
                Initialize(dependencyObject);
            }
            
            _constructorInfo = null;
            _objectGraphHashCodes.Clear();
            return dependencyObject;
        }

        private static void Initialize(DependencyObject dependencyObject)
        {
            if (dependencyObject.Implementation is IContainerInitializable initializable)
            {
                initializable.Initialize();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DependencyObject TryGetDependencyObject(Type serviceType)
        {
            if (_serviceDictionary.TryGetValue(0, out var mainScope) && 
                mainScope.TryGetValue(serviceType, out var dependencyObject))
            {
                return dependencyObject;
            }
            
            Logs.InvalidOperation(_constructorInfo == null
                ? $"There is no such a service {serviceType} registered"
                : $"{_constructorInfo.DeclaringType} tried to find {serviceType} but dependency is not found.");
            
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryToGetCachedImpl(DependencyObject dependencyObject, out DependencyObject value)
        {
            switch (dependencyObject.LifeTime)
            {
                case ServiceLifeTime.Scoped:
                {
                    if (_serviceDictionary.TryGetValue(_currentScopeID, out var scope))
                    {
                        lock (_dictionaryLock)
                        {
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
                        }
                        return false;
                    }
                    
                    scope = new Dictionary<Type, DependencyObject>(32);
                    _serviceDictionary[_currentScopeID] = scope;
                    
                    var newScopedDependencyObject = new DependencyObject(dependencyObject);
                    scope[newScopedDependencyObject.ServiceType] = newScopedDependencyObject;
                    value = newScopedDependencyObject;
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
            var hashCode = _constructorInfo.GetHashCode();
            if (_objectGraphHashCodes.Contains(hashCode))
            {
                Logs.InvalidOperation($"{_constructorInfo.DeclaringType} has circular dependency!");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ConstructorInfo GetConstructorInfo(Type actualType)
        {
            var ctors = _constructorCache.GetOrAdd(actualType, type => type.GetConstructors());
            
            if (ctors.Length == 0)
            {
                Logs.Log($"<color=yellow>{actualType} has no public constructors</color>");
                return null;
            }
            
            var bestConstructor = ctors[0];
            
            for (var i = 0; i < ctors.Length; i++)
            {
                var constructor = ctors[i];
                if (constructor.GetParameters().Length > 0)
                {
                    bestConstructor = constructor;
                    break;
                }
            }

            return bestConstructor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetImplementationTypes(DependencyObject dependencyObject)
        {
            if (dependencyObject.LifeTime == ServiceLifeTime.Transient)
            {
                return;
            }

            var interfaceTypes = dependencyObject.InterfaceTypes;
            var count = interfaceTypes.Count;
            for (var i = 0; i < count; i++)
            {
                var serviceType = interfaceTypes[i];
                switch (dependencyObject.LifeTime)
                {
                    case ServiceLifeTime.Singleton:
                        var mainScope = ServiceDictionary[0];
                        if (mainScope.TryGetValue(serviceType, out var singletonDependencyObject))
                        {
                            singletonDependencyObject.Implementation ??= dependencyObject.Implementation;
                        }
                        break;
                    case ServiceLifeTime.Scoped:
                        var currentScope = _serviceDictionary[_currentScopeID];
                        lock (_dictionaryLock)
                        {
                            if (!currentScope.TryGetValue(serviceType, out var scopedDependencyObject))
                            {
                                dependencyObject.ServiceType = serviceType;
                                scopedDependencyObject = new DependencyObject(dependencyObject);
                                currentScope[serviceType] = scopedDependencyObject;
                            }
                        }
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposableInterface(DependencyObject dependencyObject)
        {
            if (dependencyObject.LifeTime == ServiceLifeTime.Transient)
            {
                return;
            }
            
            dependencyObject.SetDisposable();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseScope(int scopeID)
        {
            if (scopeID <= 0)
            {
                Logs.Log("<color=yellow>You can't release the main scope</color>");
                return;
            }

            if (_serviceDictionary.TryGetValue(scopeID, out var scope))
            {
                foreach (var scopedDependencyObject in scope.Values)
                {
                    scopedDependencyObject.Dispose();
                }

                scope.Clear();
                Logs.Log($"<color=green> Scope has been released with ID {scopeID}</color>");
            }

            if (scopeID == _currentScopeID)
            {
                _currentScopeID = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseAll()
        {
            foreach (var scopes in ServiceDictionary)
            {
                foreach (var scope in scopes.Value.Values)
                {
                    if (scope.Implementation is null or UnityEngine.Object)
                    {
                        continue;
                    }

                    scope.Dispose();
                }
            }

            ServiceDictionary.Clear();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(Type type)
        {
            if (ServiceDictionary == null || ServiceDictionary.Count == 0) return;
            
            var mainScope = ServiceDictionary[0];
            if (!mainScope.TryGetValue(type, out var value))
            {
                return;
            }
            
            if (value.Implementation == null)
            {
                return;
            }
            
            value.Dispose();
            
            lock (_dictionaryLock)
            {
                mainScope.Remove(type);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunUpdate()
        {
            OnContainerUpdate?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunSceneLoaded(int scene)
        {
           OnContainerSceneLoaded?.Invoke(scene);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunSceneUnloaded(int scene)
        {
            OnContainerSceneUnloaded?.Invoke(scene);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunApplicationFocus(bool focus)
        {
            OnContainerApplicationFocus?.Invoke(focus);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunApplicationPause(bool pause)
        {
            OnContainerApplicationPause?.Invoke(pause);
        }
    }
}