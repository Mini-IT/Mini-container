using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace MiniContainer
{
    public class DIContainer : IContainer, IContainerListener
    {
        // Maximum pool size for each type
        private const int MaxPoolSize = 32;
        
        private ConstructorInfo _constructorInfo;
        private readonly List<Type> _ignoreTypeList;
        private readonly List<ConstructorInfo> _objectGraph;
        private readonly HashSet<int> _objectGraphHashCodes;
        private readonly List<IRegistration> _registrations;
        private readonly ConcurrentDictionary<int, Dictionary<Type, DependencyObject>> _serviceDictionary;
        private readonly object _dictionaryLock = new object();
        // Cache for reflection
        private readonly Dictionary<Type, FieldInfo[]> _fieldCache;
        private readonly Dictionary<Type, PropertyInfo[]> _propertyCache;
        private readonly Dictionary<Type, MethodInfo[]> _methodCache;
        private readonly Dictionary<Type, ConstructorInfo[]> _constructorCache;
        // Object pool for Transient dependencies
        private readonly Dictionary<Type, Stack<object>> _objectPool;
        // Flag to enable/disable pooling
        private readonly bool _enablePooling;
        // Flag to enable/disable parallel initialization
        private readonly bool _enableParallelInitialization;

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
  
        public DIContainer(List<IRegistration> registrations, List<Type> ignoreTypeList, bool enablePooling = true, 
            bool enableParallelInitialization = true)
        {
            _serviceDictionary = new ConcurrentDictionary<int, Dictionary<Type, DependencyObject>>();
            _fieldCache = new Dictionary<Type, FieldInfo[]>(128);
            _propertyCache = new Dictionary<Type, PropertyInfo[]>(128);
            _methodCache = new Dictionary<Type, MethodInfo[]>(128);
            _constructorCache = new Dictionary<Type, ConstructorInfo[]>(128);
            _objectGraph = new List<ConstructorInfo>(16);
            _objectGraphHashCodes = new HashSet<int>(16);
            _ignoreTypeList = ignoreTypeList;
            _registrations = registrations;
            _objectPool = new Dictionary<Type, Stack<object>>(64);
            _enablePooling = enablePooling;
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
            // Pre-allocate memory for dictionary
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
            
            // Pre-allocate memory for dictionary
            var mainScope = new Dictionary<Type, DependencyObject>(_registrations.Count * 2);
            _serviceDictionary[0] = mainScope;
            
            // Pre-calculate total interfaces count for more accurate memory allocation
            int totalInterfaces = 0;
            foreach (var registration in _registrations)
            {
                totalInterfaces += registration.InterfaceTypes.Count;
            }
            
            // Allocate memory with some extra space
            mainScope.EnsureCapacity(Math.Max(totalInterfaces, _registrations.Count * 2));
            
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
            
            // Собираем все объекты, которые нужно инициализировать
            var instanceObjects = new List<object>();
            foreach (var dependencyObject in mainScope.Values)
            {
                if (dependencyObject is InstanceRegistrationDependencyObject)
                {
                    instanceObjects.Add(dependencyObject.Implementation);
                }
            }
            
            // Параллельно инициализируем объекты
            if (instanceObjects.Count > 0)
            {
                Parallel.ForEach(instanceObjects, implementation =>
                {
                    // Для каждого объекта создаем локальные кэши, чтобы избежать блокировок
                    var localFieldCache = new Dictionary<Type, FieldInfo[]>();
                    var localPropertyCache = new Dictionary<Type, PropertyInfo[]>();
                    var localMethodCache = new Dictionary<Type, MethodInfo[]>();
                    
                    ResolveFieldParallel(implementation, localFieldCache);
                    ResolvePropertyParallel(implementation, localPropertyCache);
                    ResolveMethodParallel(implementation, localMethodCache);
                });
            }
        }
        
        // Параллельные версии методов разрешения зависимостей
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveFieldParallel(object implementation, Dictionary<Type, FieldInfo[]> localFieldCache)
        {
            var type = implementation.GetType();
            if (!localFieldCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                localFieldCache[type] = fields;
            }

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var fieldImpl = ResolveType(field.FieldType).Implementation;
                
                // Use cached setter or create a new one
                var setter = Activator.GetFieldSetter(field);
                setter(implementation, fieldImpl);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolvePropertyParallel(object implementation, Dictionary<Type, PropertyInfo[]> localPropertyCache)
        {
            var type = implementation.GetType();
            if (!localPropertyCache.TryGetValue(type, out var properties))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                localPropertyCache[type] = properties;
            }

            foreach (var property in properties)
            {
                var attr = property.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var propertyImpl = ResolveType(property.PropertyType).Implementation;
                
                // Use cached setter or create a new one
                var setter = Activator.GetPropertySetter(property);
                setter(implementation, propertyImpl);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveMethodParallel(object implementation, Dictionary<Type, MethodInfo[]> localMethodCache)
        {
            var type = implementation.GetType();
            if (!localMethodCache.TryGetValue(type, out var methods))
            {
                methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                localMethodCache[type] = methods;
            }

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var parameters = SelectParameters(method.GetParameters());
                
                // Use cached invoker or create a new one
                var invoker = Activator.GetMethodInvoker(method);
                invoker(implementation, parameters);
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
                // If object is Transient, return it to pool instead of destroying
                if (_enablePooling && !(impl is IDisposable) && !(impl is UnityEngine.Object))
                {
                    ReturnToPool(serviceType, impl);
                }
                dependencyObject.Implementation = null;
                dependencyObject.Dispose();
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
            if (!_propertyCache.TryGetValue(type, out var properties))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _propertyCache[type] = properties;
            }

            foreach (var property in properties)
            {
                var attr = property.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var propertyImpl = ResolveType(property.PropertyType).Implementation;
                
                // Use cached setter or create a new one
                var setter = Activator.GetPropertySetter(property);
                setter(implementation, propertyImpl);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveField(object implementation)
        {
            var type = implementation.GetType();
            if (!_fieldCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _fieldCache[type] = fields;
            }

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var fieldImpl = ResolveType(field.FieldType).Implementation;
                
                // Use cached setter or create a new one
                var setter = Activator.GetFieldSetter(field);
                setter(implementation, fieldImpl);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResolveMethod(object implementation)
        {
            var type = implementation.GetType();
            if (!_methodCache.TryGetValue(type, out var methods))
            {
                methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _methodCache[type] = methods;
            }

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute(typeof(ResolveAttribute));

                if (attr == null)
                {
                    continue;
                }

                var parameters = SelectParameters(method.GetParameters());
                
                // Use cached invoker or create a new one
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
                else if (_enablePooling && dependencyObject.LifeTime == ServiceLifeTime.Transient && 
                         TryGetFromPool(dependencyObject.ImplementationType, out var pooledObject))
                {
                    // Get object from pool if available
                    dependencyObject.Implementation = pooledObject;
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
                
                        _objectGraph.Add(_constructorInfo);
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
                CheckInterfaces(dependencyObject);
                SetImplementationTypes(dependencyObject);
                Initialize(dependencyObject);
            }
            
            _constructorInfo = null;
            _objectGraph.Clear();
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
            // Use TryGetValue instead of indexer to avoid creating temporary objects
            if (_serviceDictionary.TryGetValue(0, out var mainScope) && 
                mainScope.TryGetValue(serviceType, out var dependencyObject))
            {
                return dependencyObject;
            }
            
            ConstructorInfo obj = null;
            if (_objectGraph.Count > 0)
            {
                obj = _objectGraph[^1];
            }

            Logs.InvalidOperation(obj == null
                ? $"There is no such a service {serviceType} registered"
                : $"{obj.DeclaringType} tried to find {serviceType} but dependency is not found.");
            
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryToGetCachedImpl(DependencyObject dependencyObject, out DependencyObject value)
        {
            switch (dependencyObject.LifeTime)
            {
                case ServiceLifeTime.Scoped:
                {
                    // Используем TryGetValue вместо индексатора для избежания создания временных объектов
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
                    
                    // Если скоп не найден, создаем новый
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
            // Use HashSet for fast element check
            var hashCode = _constructorInfo.GetHashCode();
            if (_objectGraphHashCodes.Contains(hashCode))
            {
                Logs.InvalidOperation($"{_constructorInfo.DeclaringType} has circular dependency!");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ConstructorInfo GetConstructorInfo(Type actualType)
        {
            if (!_constructorCache.TryGetValue(actualType, out var ctors))
            {
                ctors = actualType.GetConstructors();
                _constructorCache[actualType] = ctors;
            }
            
            if (ctors.Length == 0)
            {
                Logs.Log($"<color=yellow>{actualType} has no public constructors</color>");
                return null;
            }
            
            var first = ctors[0];
            
            foreach (var c in ctors)
            {
                if (c.GetParameters().Length <= 0)
                {
                    continue;
                }
                first = c;
                break;
            }

            return first;
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
        private void CheckInterfaces(DependencyObject dependencyObject)
        {
            dependencyObject.SetDisposable();
            dependencyObject.SetListeners(this);
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
                    if (scope.Implementation is null or MonoBehaviour)
                    {
                        continue;
                    }

                    scope.Dispose();
                }
            }

            ServiceDictionary.Clear();
            _objectGraph.Clear();
            
            // Очистка пула объектов
            if (_enablePooling)
            {
                lock (_dictionaryLock)
                {
                    _objectPool.Clear();
                }
            }
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

            // Release the object and its dependencies
            ReleaseObject(value);
            
            // Remove from dictionary
            lock (_dictionaryLock)
            {
                mainScope.Remove(type);
            }
            
            // Force garbage collection to free memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseObject(DependencyObject dependencyObject)
        {
            // Clear all references to the object in other services
            var mainScope = ServiceDictionary[0];
            foreach (var service in mainScope.Values)
            {
                if (service.Implementation == null)
                {
                    continue;
                }

                // Clear fields
                var type = service.Implementation.GetType();
                if (!_fieldCache.TryGetValue(type, out var fields))
                {
                    fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    _fieldCache[type] = fields;
                }
                
                foreach (var field in fields)
                {
                    if (field.FieldType == dependencyObject.ServiceType)
                    {
                        field.SetValue(service.Implementation, null);
                    }
                }
                
                // Clear properties
                if (!_propertyCache.TryGetValue(type, out var properties))
                {
                    properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    _propertyCache[type] = properties;
                }
                
                foreach (var property in properties)
                {
                    if (property.PropertyType == dependencyObject.ServiceType && property.CanWrite)
                    {
                        property.SetValue(service.Implementation, null);
                    }
                }
            }

            dependencyObject.Dispose();
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
        
        // Новые методы для работы с пулом объектов
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetFromPool(Type type, out object instance)
        {
            instance = null;
            
            if (!_enablePooling)
                return false;
                
            lock (_dictionaryLock)
            {
                if (_objectPool.TryGetValue(type, out var stack) && stack.Count > 0)
                {
                    instance = stack.Pop();
                    return true;
                }
            }
            
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReturnToPool(Type type, object instance)
        {
            if (!_enablePooling)
                return;
                
            lock (_dictionaryLock)
            {
                if (!_objectPool.TryGetValue(type, out var stack))
                {
                    stack = new Stack<object>(MaxPoolSize);
                    _objectPool[type] = stack;
                }
                
                if (stack.Count < MaxPoolSize)
                {
                    stack.Push(instance);
                }
            }
        }
    }
}