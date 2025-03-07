using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniContainer
{
    public interface IRegistration
    {
        RegistrationType RegistrationType { get; }
        Component Prefab { get; }
        Transform Parent { get; }
        string GameObjectName { get; }
        Type ImplementationType { get; }
        object Implementation { get; }
        Func<object> GetImplementation { get; }
        List<Type> InterfaceTypes { get; }
        ServiceLifeTime LifeTime { get; }
        Registration As<TInterface>();
        Registration As<TInterface1, TInterface2>();
        Registration As<TInterface1, TInterface2, TInterface3>();
        Registration As<TInterface1, TInterface2, TInterface3, TInterface4>();
        Registration As(Type interfaceType);
        Registration As(Type interfaceType1, Type interfaceType2);
        Registration As(Type interfaceType1, Type interfaceType2, Type interfaceType3);
        Registration As(params Type[] interfaceTypes);
        Registration AsSelf();
        Registration AsImplementedInterfaces();
        void AddInterfaceType(Type interfaceType);
    }
}