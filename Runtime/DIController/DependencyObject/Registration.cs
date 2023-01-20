using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

namespace MiniContainer
{
    public class Registration
    {
        public RegistrationType RegistrationType { get; internal set; }

        public Component Prefab { get; internal set; }

        public Transform Parent { get; internal set; }

        public string GameObjectName { get; internal set; }

        public Type ImplementationType { get; internal set; }

        public object Implementation { get; internal set; }

        public List<Type> InterfaceTypes { get; private set; }

        public ServiceLifeTime LifeTime { get; internal set; }

        public bool OnSceneDestroyRelease { get; internal set; }

        public Registration As<TInterface>()
            => As(typeof(TInterface));

        public Registration As<TInterface1, TInterface2>()
            => As(typeof(TInterface1), typeof(TInterface2));

        public Registration As<TInterface1, TInterface2, TInterface3>()
            => As(typeof(TInterface1), typeof(TInterface2), typeof(TInterface3));

        public Registration As<TInterface1, TInterface2, TInterface3, TInterface4>()
            => As(typeof(TInterface1), typeof(TInterface2), typeof(TInterface3), typeof(TInterface4));

        public Registration AsSelf()
        {
            AddInterfaceType(ImplementationType);
            return this;
        }

        public Registration AsImplementedInterfaces()
        {
            InterfaceTypes ??= new List<Type>();
            var interfaces = ImplementationType.GetInterfaces().ToList();

            for (var i = 0; i < interfaces.Count; i++)
            {
                i = CheckInterface(interfaces, i);
            }

            InterfaceTypes.AddRange(interfaces);
            return this;
        }

        private static int CheckInterface(IList<Type> interfaces, int i)
        {
            if (interfaces[i] != typeof(IContainerUpdateListener) &&
                interfaces[i] != typeof(IContainerSceneLoadedListener) &&
                interfaces[i] != typeof(IContainerSceneUnloadedListener) &&
                interfaces[i] != typeof(IContainerApplicationFocusListener) &&
                interfaces[i] != typeof(IContainerApplicationPauseListener) &&
                interfaces[i] != typeof(IDisposable))
            {
                return i;
            }

            interfaces.Remove(interfaces[i]);
            i--;
            return i;
        }

        public Registration As(Type interfaceType)
        {
            AddInterfaceType(interfaceType);
            return this;
        }

        public Registration As(Type interfaceType1, Type interfaceType2)
        {
            AddInterfaceType(interfaceType1);
            AddInterfaceType(interfaceType2);
            return this;
        }

        public Registration As(Type interfaceType1, Type interfaceType2, Type interfaceType3)
        {
            AddInterfaceType(interfaceType1);
            AddInterfaceType(interfaceType2);
            AddInterfaceType(interfaceType3);
            return this;
        }

        public Registration As(params Type[] interfaceTypes)
        {
            foreach (var interfaceType in interfaceTypes)
            {
                AddInterfaceType(interfaceType);
            }
            return this;
        }

        private void AddInterfaceType(Type interfaceType)
        {
            if (!interfaceType.IsAssignableFrom(ImplementationType))
            {
                throw new Exception($"{ImplementationType} is not assignable from {interfaceType}");
            }

            InterfaceTypes ??= new List<Type>();
            if (!InterfaceTypes.Contains(interfaceType))
            {
                InterfaceTypes.Add(interfaceType);
            }
        }
    }
}