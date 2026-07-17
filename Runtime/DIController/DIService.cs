using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MiniContainer
{
    public class DIService : IDIService
    {
        private readonly List<IRegistration> _registrations = new List<IRegistration>();
        private DIContainer _container;

        private readonly List<Type> _ignoreTypeList = new List<Type>()
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
            _ignoreTypeList.Add(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRegistered<T>()
        {
            var type = typeof(T);
            
            if (_container != null && _container.IsRegistered(type))
            {
                return true;
            }

            if (IsTypeIgnored(type))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsTypeIgnored(Type type)
        {
            for (var i = 0; i < _ignoreTypeList.Count; i++)
            {
                if (_ignoreTypeList[i] == type)
                {
                    return true;
                }
            }

            return false;
        }

        public DIContainer GenerateContainer()
        {
            _container = new DIContainer(_registrations, _ignoreTypeList);
            this.RegisterInstance(new ScopeManager(_container)).AsImplementedInterfaces();
            this.RegisterInstance<IContainerLifeCycle>(_container);
            return _container;
        }
    }
}