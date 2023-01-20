using System;
using System.Collections.Generic;

namespace MiniContainer
{
    public class DependencyObject
    {
        public Type ServiceType { get; }

        public Type ImplementationType { get; }

        public bool OnSceneDestroyRelease { get; }

        public List<Type> InterfaceTypes { get; }

        private object _implementation;

        public object Implementation
        {
            get => _implementation;
            internal set
            {
                _implementation = value;
                if (value == null)
                {
                    ContainerUpdate = null;
                    ContainerSceneLoaded = null;
                    ContainerSceneUnloaded = null;
                    ContainerApplicationFocus = null;
                    ContainerApplicationPause = null;
                }
            }
        }

        public IContainerUpdateListener ContainerUpdate { get; internal set; }
        public IContainerSceneLoadedListener ContainerSceneLoaded { get; internal set; }
        public IContainerSceneUnloadedListener ContainerSceneUnloaded { get; internal set; }
        public IContainerApplicationFocusListener ContainerApplicationFocus { get; internal set; }
        public IContainerApplicationPauseListener ContainerApplicationPause { get; internal set; }
        public IDisposable Disposable { get; internal set; }
        public ServiceLifeTime LifeTime { get; }

        public DependencyObject(Type serviceType, Type implementationType, object implementation, ServiceLifeTime lifeTime, List<Type> interfaceTypes, bool onSceneDestroyRelease)
        {
            ServiceType = serviceType;
            LifeTime = lifeTime;
            ImplementationType = implementationType;
            Implementation = implementation;
            InterfaceTypes = interfaceTypes;
            OnSceneDestroyRelease = onSceneDestroyRelease;
        }
    }
}
