using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MiniContainer
{
    public class DIService : IDIService
    {
        private readonly List<IRegistration> _registrations;
        private readonly List<Type> _ignoreTypeList;

        public DIService()
        {
            _ignoreTypeList = new List<Type>()
            {
                typeof(IContainerUpdateListener),
                typeof(IContainerSceneLoadedListener),
                typeof(IContainerSceneUnloadedListener),
                typeof(IContainerApplicationFocusListener),
                typeof(IContainerApplicationPauseListener),
                typeof(IDisposable)
            };
            _registrations = new List<IRegistration>();
        }

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
            return new DIContainer(_registrations, _ignoreTypeList);
        }
    }
}