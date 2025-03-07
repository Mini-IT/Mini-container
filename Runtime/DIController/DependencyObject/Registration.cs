using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MiniContainer
{
    public class Registration : IRegistration
    {
        public RegistrationType RegistrationType { get; internal set; }

        public Component Prefab { get; internal set; }

        public Transform Parent { get; internal set; }

        public string GameObjectName { get; internal set; }

        public Type ImplementationType { get; internal set; }

        public object Implementation { get; internal set; }
        
        public Func<object> GetImplementation { get; set; }

        public List<Type> InterfaceTypes { get; private set; }

        public ServiceLifeTime LifeTime { get; internal set; }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registration As<TInterface>()
            => As(typeof(TInterface));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registration As<TInterface1, TInterface2>()
            => As(typeof(TInterface1), typeof(TInterface2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registration As<TInterface1, TInterface2, TInterface3>()
            => As(typeof(TInterface1), typeof(TInterface2), typeof(TInterface3));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registration As<TInterface1, TInterface2, TInterface3, TInterface4>()
            => As(typeof(TInterface1), typeof(TInterface2), typeof(TInterface3), typeof(TInterface4));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registration AsSelf()
        {
            AddInterfaceType(ImplementationType);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registration AsImplementedInterfaces()
        {
            InterfaceTypes ??= new List<Type>();
            var interfaces = ImplementationType.GetInterfaces();

            foreach (var interfaceType in interfaces)
            {
                if (!InterfaceTypes.Contains(interfaceType))
                {
                    InterfaceTypes.Add(interfaceType);
                }
            }
            
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registration As(Type interfaceType)
        {
            AddInterfaceType(interfaceType);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registration As(Type interfaceType1, Type interfaceType2)
        {
            AddInterfaceType(interfaceType1);
            AddInterfaceType(interfaceType2);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registration As(Type interfaceType1, Type interfaceType2, Type interfaceType3)
        {
            AddInterfaceType(interfaceType1);
            AddInterfaceType(interfaceType2);
            AddInterfaceType(interfaceType3);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registration As(params Type[] interfaceTypes)
        {
            foreach (var interfaceType in interfaceTypes)
            {
                AddInterfaceType(interfaceType);
            }
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddInterfaceType(Type interfaceType)
        {
            if (!interfaceType.IsAssignableFrom(ImplementationType))
            {
                Logs.InvalidOperation($"{ImplementationType} is not assignable from {interfaceType}");
            }

            InterfaceTypes ??= new List<Type>(4); // Pre-allocate memory
            if (!InterfaceTypes.Contains(interfaceType))
            {
                InterfaceTypes.Add(interfaceType);
            }
        }
    }
}