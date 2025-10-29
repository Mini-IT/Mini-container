using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MiniContainer
{
    public class DIService : IDIService
    {
        private readonly List<IRegistration> _registrations = new List<IRegistration>();

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

        public DIContainer GenerateContainer()
        {
            var container = new DIContainer(_registrations, _ignoreTypeList);
            this.RegisterInstance(new ScopeManager(container)).AsImplementedInterfaces();
            this.RegisterInstance<IContainerLifeCycle>(container);
            return container;
        }
    }
}