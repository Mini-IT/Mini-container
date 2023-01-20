using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MiniContainer
{
    public static class RegisterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Registration RegisterComponentInNewPrefab<TService>(
            this IDIService diService,
            TService prefab,
            Transform parent = null)
            where TService : Component
        {
            var registration = new Registration();
            registration.ImplementationType = prefab.GetType();
            registration.As(prefab.GetType());
            registration.RegistrationType = RegistrationType.Component;
            registration.LifeTime = ServiceLifeTime.Transient;
            registration.OnSceneDestroyRelease = false;
            registration.Prefab = prefab;
            registration.Parent = parent;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterComponentOnNewGameObject<TService>(
            this IDIService diService,
            Transform parent = null,
            string newGameObjectName = null)
            where TService : Component
        {
            var registration = new Registration();
            registration.ImplementationType = typeof(TService);
            registration.As<TService>();
            registration.RegistrationType = RegistrationType.Component;
            registration.LifeTime = ServiceLifeTime.Transient;
            registration.OnSceneDestroyRelease = false;
            registration.GameObjectName = newGameObjectName;
            registration.Parent = parent;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration Register<TService>(this IDIService diService,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
        {
            var registration = new Registration();
            registration.ImplementationType = typeof(TService);
            registration.As<TService>();
            registration.RegistrationType = RegistrationType.Base;
            registration.LifeTime = serviceLifeTime;
            registration.OnSceneDestroyRelease = false;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration Register(this IDIService diService,
            Type type,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
        {
            var registration = new Registration();
            registration.ImplementationType = type;
            registration.As(type);
            registration.RegistrationType = RegistrationType.Base;
            registration.LifeTime = serviceLifeTime;
            registration.OnSceneDestroyRelease = false;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterInstance<TService>(this IDIService diService,
            TService implementation)
        {
            var registration = new Registration();
            registration.ImplementationType = typeof(TService);
            registration.As<TService>();
            registration.RegistrationType = RegistrationType.Instance;
            registration.LifeTime = ServiceLifeTime.Singleton;
            registration.OnSceneDestroyRelease = false;
            registration.Implementation = implementation;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterInstanceAsSelf(this IDIService diService,
            object implementation)
        {
            var registration = new Registration();
            registration.ImplementationType = implementation.GetType();
            registration.As(implementation.GetType());
            registration.RegistrationType = RegistrationType.Instance;
            registration.LifeTime = ServiceLifeTime.Singleton;
            registration.OnSceneDestroyRelease = false;
            registration.Implementation = implementation;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration Register<TService, TImplementation>(this IDIService diService,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
            where TImplementation : class, TService
        {
            var registration = new Registration();
            registration.ImplementationType = typeof(TImplementation);
            registration.As<TService>();
            registration.RegistrationType = RegistrationType.Base;
            registration.LifeTime = serviceLifeTime;
            registration.OnSceneDestroyRelease = false;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterComponentInNewPrefab<TService>(
            this IBaseDIService diService,
            TService prefab,
            Transform parent = null)
            where TService : Component
        {

            var registration = new Registration();
            registration.ImplementationType = prefab.GetType();
            registration.As(prefab.GetType());
            registration.RegistrationType = RegistrationType.Component;
            registration.LifeTime = ServiceLifeTime.Transient;
            registration.OnSceneDestroyRelease = true;
            registration.Parent = parent;
            registration.Prefab = prefab;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterComponentOnNewGameObject<TService>(
            this IBaseDIService diService,
            Transform parent = null,
            string newGameObjectName = null)
            where TService : Component
        {
            var registration = new Registration();
            registration.ImplementationType = typeof(TService);
            registration.As<TService>();
            registration.RegistrationType = RegistrationType.Component;
            registration.LifeTime = ServiceLifeTime.Transient;
            registration.OnSceneDestroyRelease = true;
            registration.GameObjectName = newGameObjectName;
            registration.Parent = parent;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration Register<TService>(this IBaseDIService diService,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
        {
            var registration = new Registration();
            registration.ImplementationType = typeof(TService);
            registration.As<TService>();
            registration.RegistrationType = RegistrationType.Base;
            registration.LifeTime = serviceLifeTime;
            registration.OnSceneDestroyRelease = true;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration Register(this IBaseDIService diService,
            Type type,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
        {
            var registration = new Registration();
            registration.ImplementationType = type;
            registration.As(type);
            registration.RegistrationType = RegistrationType.Base;
            registration.LifeTime = serviceLifeTime;
            registration.OnSceneDestroyRelease = true;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterInstance<TService>(this IBaseDIService diService,
            TService implementation)
        {
            var registration = new Registration();
            registration.ImplementationType = typeof(TService);
            registration.As<TService>();
            registration.RegistrationType = RegistrationType.Instance;
            registration.LifeTime = ServiceLifeTime.Singleton;
            registration.OnSceneDestroyRelease = true;
            registration.Implementation = implementation;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterInstanceAsSelf(this IBaseDIService diService,
            object implementation)
        {
            var registration = new Registration();
            registration.ImplementationType = implementation.GetType();
            registration.As(implementation.GetType());
            registration.RegistrationType = RegistrationType.Instance;
            registration.LifeTime = ServiceLifeTime.Singleton;
            registration.OnSceneDestroyRelease = true;
            registration.Implementation = implementation;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration Register<TService, TImplementation>(this IBaseDIService diService,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
            where TImplementation : class, TService
        {
            var registration = new Registration();
            registration.ImplementationType = typeof(TImplementation);
            registration.As<TService>();
            registration.RegistrationType = RegistrationType.Base;
            registration.LifeTime = serviceLifeTime;
            registration.OnSceneDestroyRelease = false;

            return diService.Register(registration);
        }
    }
}