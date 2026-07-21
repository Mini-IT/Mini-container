using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MiniContainer
{
    public class DIService : IDIService
    {
        private readonly List<IRegistration> _registrations = new List<IRegistration>();

        private readonly HashSet<Type> _ignoreTypes = new HashSet<Type>()
        {
            typeof(IDisposable)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Register<T>(T registration) where T : IRegistration
        {
            _registrations.Add(registration);
            return registration;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IgnoreType<T>(T type) where T : Type
        {
            _ignoreTypes.Add(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRegistered<T>()
        {
            var type = typeof(T);
            
            if (_ignoreTypes.Contains(type))
            {
                return false;
            }

            for (int i = 0; i < _registrations.Count; i++)
            {
                if (_registrations[i].InterfaceTypes.Contains(type))
                {
                    return true;
                }
            }

            return false;
        }

        public DIContainer GenerateContainer()
        {
            var container = new DIContainer(_registrations, _ignoreTypes);
            this.RegisterInstance(new ScopeManager(container)).AsImplementedInterfaces();
            this.RegisterInstance<IContainerLifeCycle>(container);
            return container;
        }
    }
}