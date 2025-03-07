using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MiniContainer
{
    public static class Activator
    {
        private const string Args = "args";
        internal delegate object ObjectActivator(params object[] args);
        
        // Cache for object activators
        private static readonly Dictionary<ConstructorInfo, ObjectActivator> _activatorCache = 
            new Dictionary<ConstructorInfo, ObjectActivator>(128);
        
        // Cache for default constructors
        private static readonly Dictionary<Type, Func<object>> _defaultConstructorCache = 
            new Dictionary<Type, Func<object>>(128);
        
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
    }
}