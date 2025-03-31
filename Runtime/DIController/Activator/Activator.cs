using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MiniContainer
{
    public static class Activator
    {
        // String constants
        private const string Args = "args";
        private const string Instance = "instance";
        private const string Value = "value";
        private const string Parameters = "parameters";
        
        internal delegate object ObjectActivator(params object[] args);
        
        // Cache for object activators
        private static readonly Dictionary<ConstructorInfo, ObjectActivator> _activatorCache = 
            new Dictionary<ConstructorInfo, ObjectActivator>(128);
        
        // Cache for default constructors
        private static readonly Dictionary<Type, Func<object>> _defaultConstructorCache = 
            new Dictionary<Type, Func<object>>(128);
        
        // Cache for property setters
        private static readonly Dictionary<PropertyInfo, Action<object, object>> _propertySetterCache = 
            new Dictionary<PropertyInfo, Action<object, object>>(256);
            
        // Cache for field setters
        private static readonly Dictionary<FieldInfo, Action<object, object>> _fieldSetterCache = 
            new Dictionary<FieldInfo, Action<object, object>>(256);
            
        // Cache for method invokers
        private static readonly Dictionary<MethodInfo, Func<object, object[], object>> _methodInvokerCache = 
            new Dictionary<MethodInfo, Func<object, object[], object>>(128);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ObjectActivator GetActivator(ConstructorInfo ctor)
        {
            // Check cache
            if (_activatorCache.TryGetValue(ctor, out var activator))
            {
                return activator;
            }
            
            var paramsInfo = ctor.GetParameters();

            //create a single param of type object[]
            var param = Expression.Parameter(typeof(object[]), Args);

            var argsExp = new Expression[paramsInfo.Length];

            //pick each arg from the params array 
            //and create a typed expression of them
            for (var i = 0; i < paramsInfo.Length; i++)
            {
                Expression index = Expression.Constant(i);
                var paramType = paramsInfo[i].ParameterType;

                Expression paramAccessorExp = Expression.ArrayIndex(param, index);
                Expression paramCastExp = Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            //make a NewExpression that calls the
            //ctor with the args we just created
            var newExp = Expression.New(ctor, argsExp);

            //create a lambda with the New
            //Expression as body and our param object[] as arg
            var lambda = Expression.Lambda<ObjectActivator>(newExp, param);

            //compile it
            var compiled = lambda.Compile();
            
            // Save to cache
            _activatorCache[ctor] = compiled;
            
            return compiled;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Func<object> CreateDefaultConstructor(Type type)
        {
            // Check cache
            if (_defaultConstructorCache.TryGetValue(type, out var constructor))
            {
                return constructor;
            }
            
            var newExp = Expression.New(type);

            // Create a new lambda expression with the NewExpression as the body.
            var lambda = Expression.Lambda<Func<object>>(newExp);

            // Compile our new lambda expression.
            var compiled = lambda.Compile();
            
            // Save to cache
            _defaultConstructorCache[type] = compiled;
            
            return compiled;
        }
        
        /// <summary>
        /// Creates a fast property setter using Expression Trees
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Action<object, object> CreatePropertySetter(PropertyInfo property)
        {
            // Check cache
            if (_propertySetterCache.TryGetValue(property, out var setter))
            {
                return setter;
            }
            
            var instanceParam = Expression.Parameter(typeof(object), Instance);
            var valueParam = Expression.Parameter(typeof(object), Value);
            
            var instanceCast = Expression.Convert(instanceParam, property.DeclaringType);
            var valueCast = Expression.Convert(valueParam, property.PropertyType);
            
            var setterCall = Expression.Call(instanceCast, property.GetSetMethod(true), valueCast);
            
            var compiled = Expression.Lambda<Action<object, object>>(setterCall, instanceParam, valueParam).Compile();
            
            // Save to cache
            _propertySetterCache[property] = compiled;
            
            return compiled;
        }
        
        /// <summary>
        /// Creates a fast field setter using Expression Trees
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Action<object, object> CreateFieldSetter(FieldInfo field)
        {
            // Check cache
            if (_fieldSetterCache.TryGetValue(field, out var setter))
            {
                return setter;
            }
            
            var instanceParam = Expression.Parameter(typeof(object), Instance);
            var valueParam = Expression.Parameter(typeof(object), Value);
            
            var instanceCast = Expression.Convert(instanceParam, field.DeclaringType);
            var valueCast = Expression.Convert(valueParam, field.FieldType);
            
            var fieldAccess = Expression.Field(instanceCast, field);
            var assignExpression = Expression.Assign(fieldAccess, valueCast);
            
            var compiled = Expression.Lambda<Action<object, object>>(assignExpression, instanceParam, valueParam).Compile();
            
            // Save to cache
            _fieldSetterCache[field] = compiled;
            
            return compiled;
        }
        
        /// <summary>
        /// Creates a fast method invoker using Expression Trees
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Func<object, object[], object> CreateMethodInvoker(MethodInfo method)
        {
            // Check cache
            if (_methodInvokerCache.TryGetValue(method, out var invoker))
            {
                return invoker;
            }
            
            var instanceParam = Expression.Parameter(typeof(object), Instance);
            var parametersParam = Expression.Parameter(typeof(object[]), Parameters);
            
            var instanceCast = Expression.Convert(instanceParam, method.DeclaringType);
            
            var parameterExpressions = new List<Expression>();
            var paramInfos = method.GetParameters();
            
            for (int i = 0; i < paramInfos.Length; i++)
            {
                var paramInfo = paramInfos[i];
                var paramAccessor = Expression.ArrayIndex(parametersParam, Expression.Constant(i));
                var paramCast = Expression.Convert(paramAccessor, paramInfo.ParameterType);
                parameterExpressions.Add(paramCast);
            }
            
            var methodCall = Expression.Call(instanceCast, method, parameterExpressions);
            
            // If method returns void, wrap the call in a block and return null
            Expression body;
            if (method.ReturnType == typeof(void))
            {
                body = Expression.Block(methodCall, Expression.Constant(null));
            }
            else
            {
                body = Expression.Convert(methodCall, typeof(object));
            }
            
            var compiled = Expression.Lambda<Func<object, object[], object>>(body, instanceParam, parametersParam).Compile();
            
            // Save to cache
            _methodInvokerCache[method] = compiled;
            
            return compiled;
        }
        
        /// <summary>
        /// Gets a property setter from cache or creates a new one
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Action<object, object> GetPropertySetter(PropertyInfo property)
        {
            if (_propertySetterCache.TryGetValue(property, out var setter))
            {
                return setter;
            }
            
            return CreatePropertySetter(property);
        }
        
        /// <summary>
        /// Gets a field setter from cache or creates a new one
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Action<object, object> GetFieldSetter(FieldInfo field)
        {
            if (_fieldSetterCache.TryGetValue(field, out var setter))
            {
                return setter;
            }
            
            return CreateFieldSetter(field);
        }
        
        /// <summary>
        /// Gets a method invoker from cache or creates a new one
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Func<object, object[], object> GetMethodInvoker(MethodInfo method)
        {
            if (_methodInvokerCache.TryGetValue(method, out var invoker))
            {
                return invoker;
            }
            
            return CreateMethodInvoker(method);
        }
    }
}