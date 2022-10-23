using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MiniContainer
{
    public static class RegisterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static void RegisterComponentInNewPrefab<TService>(
            this IDIService diService,
            TService prefab, 
            Transform parent = null)
            where TService : Component
        {
            var componentDependencyObject = new ComponentDependencyObject(prefab, parent, prefab.GetType(), false);

            diService.Register(componentDependencyObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterComponentOnNewGameObject<TService>(
            this IDIService diService,
            Transform parent = null,
            string newGameObjectName = null)
            where TService : Component
        {
            var componentDependencyObject = new ComponentDependencyObject(newGameObjectName, parent, typeof(TService), false);
            diService.Register(componentDependencyObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register<TService>(this IDIService diService,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
        {
            var dependencyObject = new DependencyObject(typeof(TService), serviceLifeTime, false);
            diService.Register(dependencyObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register(this IDIService diService, 
            Type type, 
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
        {
            var dependencyObject = new DependencyObject(type, serviceLifeTime, false);
            diService.Register(dependencyObject);
        }

        /// <summary>
        /// Store an instance from an implemented class
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterInstance<TService>(this IDIService diService,
            TService implementation)
        {
            var instanceRegistrationDependencyObject = new InstanceRegistrationDependencyObject(typeof(TService), implementation, false);
            diService.Register(instanceRegistrationDependencyObject);
        }

        /// <summary>
        /// Store an instance from an implemented class
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterInstanceAsSelf(this IDIService diService,
            object implementation)
        {
            var instanceRegistrationDependencyObject = new InstanceRegistrationDependencyObject(implementation.GetType(), implementation, false);
            diService.Register(instanceRegistrationDependencyObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register<TService, TImplementation>(this IDIService diService, 
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton) 
            where TImplementation : class, TService
        {
            var dependencyObject = new DependencyObject(typeof(TService), typeof(TImplementation), serviceLifeTime, false);
            diService.Register(dependencyObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static void RegisterComponentInNewPrefab<TService>(
            this IBaseDIService diService,
            TService prefab,
            Transform parent = null)
            where TService : Component
        {
            var componentDependencyObject = new ComponentDependencyObject(prefab, parent, prefab.GetType(), true);

            diService.Register(componentDependencyObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterComponentOnNewGameObject<TService>(
            this IBaseDIService diService,
            Transform parent = null,
            string newGameObjectName = null)
            where TService : Component
        {
            var componentDependencyObject = new ComponentDependencyObject(newGameObjectName, parent, typeof(TService), true);
            diService.Register(componentDependencyObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register<TService>(this IBaseDIService diService,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
        {
            var dependencyObject = new DependencyObject(typeof(TService), serviceLifeTime, true);
            diService.Register(dependencyObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register(this IBaseDIService diService, 
            Type type,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
        {
            var dependencyObject = new DependencyObject(type, serviceLifeTime, true);
            diService.Register(dependencyObject);
        }

        /// <summary>
        /// Store an instance from an implemented class
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterInstance<TService>(this IBaseDIService diService, 
            TService implementation)
        {
            var instanceRegistrationDependencyObject = new InstanceRegistrationDependencyObject(typeof(TService), implementation, true);
            diService.Register(instanceRegistrationDependencyObject);
        }

        /// <summary>
        /// Store an instance from an implemented class
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterInstanceAsSelf(this IBaseDIService diService,
            object implementation)
        {
            var instanceRegistrationDependencyObject = new InstanceRegistrationDependencyObject(implementation.GetType(), implementation, true);
            diService.Register(instanceRegistrationDependencyObject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Register<TService, TImplementation>(this IBaseDIService diService, 
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton) 
            where TImplementation : class, TService
        {
            var dependencyObject = new DependencyObject(typeof(TService), typeof(TImplementation), serviceLifeTime, true);
            diService.Register(dependencyObject);
        }
    }
}