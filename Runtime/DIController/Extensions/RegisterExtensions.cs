using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MiniContainer
{
    public static class RegisterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IgnoreType<T>(this IBaseDIService diService)
        {
            diService.IgnoreType(typeof(T));
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
            registration.GameObjectName = newGameObjectName;
            registration.Parent = parent;
            
            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration Register<TService>(
            this IBaseDIService diService,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
        {
            var registration = new Registration();
            registration.ImplementationType = typeof(TService);
            registration.As<TService>();
            registration.RegistrationType = RegistrationType.Base;
            registration.LifeTime = serviceLifeTime;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration Register(
            this IBaseDIService diService, 
            Type type,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
        {
            var registration = new Registration();
            registration.ImplementationType = type;
            registration.As(type);
            registration.RegistrationType = RegistrationType.Base;
            registration.LifeTime = serviceLifeTime;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterInstance<TService>(
            this IBaseDIService diService,
            TService implementation) where TService : class
        {
            var registration = new Registration();
            registration.ImplementationType = typeof(TService);
            registration.As<TService>();
            registration.RegistrationType = RegistrationType.Instance;
            registration.LifeTime = ServiceLifeTime.Singleton;
            registration.Implementation = implementation;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterInstanceAsSelf(
            this IBaseDIService diService,
            object implementation)
        {
            var registration = new Registration();
            registration.ImplementationType = implementation.GetType();
            registration.As(implementation.GetType());
            registration.RegistrationType = RegistrationType.Instance;
            registration.LifeTime = ServiceLifeTime.Singleton;
            registration.Implementation = implementation;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterLazy<TService>(
            this IBaseDIService diService,
            IContainer container,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton)
        {
            diService.Register<TService>(serviceLifeTime);
            var lazy = new LazyService<TService>(container);
            var registration = new Registration();
            registration.ImplementationType = lazy.GetType();
            registration.As<ILazyService<TService>>();
            registration.RegistrationType = RegistrationType.Instance;
            registration.LifeTime = ServiceLifeTime.Singleton;
            registration.Implementation = lazy;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterLazy<TService, TImplementation>(
            this IBaseDIService diService,
            IContainer container,
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton) 
            where TImplementation : class, TService
        {
            diService.Register<TService, TImplementation>(serviceLifeTime);
            var lazy = new LazyService<TService>(container);
            var registration = new Registration();
            registration.ImplementationType = lazy.GetType();
            registration.As<ILazyService<TService>>();
            registration.RegistrationType = RegistrationType.Instance;
            registration.LifeTime = ServiceLifeTime.Singleton;
            registration.Implementation = lazy;

            return diService.Register(registration);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration RegisterFactory<TService>(
            this IBaseDIService diService,
            IContainer container)
        {
            var factory = new FactoryService<TService>(container);
            var registration = new Registration();
            registration.ImplementationType = factory.GetType();
            registration.As<IFactoryService<TService>>();
            registration.RegistrationType = RegistrationType.Instance;
            registration.LifeTime = ServiceLifeTime.Singleton;
            registration.Implementation = factory;

            return diService.Register(registration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Registration Register<TService, TImplementation>(
            this IBaseDIService diService, 
            ServiceLifeTime serviceLifeTime = ServiceLifeTime.Singleton) 
            where TImplementation : class, TService
        {
            var registration = new Registration();
            registration.ImplementationType = typeof(TImplementation);
            registration.As<TService>();
            registration.RegistrationType = RegistrationType.Base;
            registration.LifeTime = serviceLifeTime;

            return diService.Register(registration);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Registration RegisterSingleton<TService>(this IBaseDIService diService)
		{
			return Register<TService>(diService, ServiceLifeTime.Singleton);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Registration RegisterScoped<TService>(this IBaseDIService diService)
		{
			return Register<TService>(diService, ServiceLifeTime.Scoped);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Registration RegisterTransient<TService>(this IBaseDIService diService)
		{
			return Register<TService>(diService, ServiceLifeTime.Transient);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Registration RegisterSingleton<TService, TImplementation>(this IBaseDIService diService)
			where TImplementation : class, TService
		{
			return Register<TService, TImplementation>(diService, ServiceLifeTime.Singleton);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Registration RegisterScoped<TService, TImplementation>(this IBaseDIService diService)
			where TImplementation : class, TService
		{
			return Register<TService, TImplementation>(diService, ServiceLifeTime.Scoped);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Registration RegisterTransient<TService, TImplementation>(this IBaseDIService diService)
			where TImplementation : class, TService
		{
			return Register<TService, TImplementation>(diService, ServiceLifeTime.Transient);
		}
	}
}