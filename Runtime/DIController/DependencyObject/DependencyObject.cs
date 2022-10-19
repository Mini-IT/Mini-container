using System;

namespace ArcCore
{
    public class DependencyObject
    {
        public Type ServiceType { get; }

        public Type ImplementationType { get; }

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

        public bool OnSceneDestroyRelease { get; }

        protected DependencyObject(Type serviceType, object implementation, bool onSceneDestroyRelease)
        {
            Implementation = implementation;
            ServiceType = serviceType;
            OnSceneDestroyRelease = onSceneDestroyRelease;
            LifeTime = ServiceLifeTime.Singleton;
        }

        protected DependencyObject(Type serviceType, bool onSceneDestroyRelease)
        {
            ServiceType = serviceType;
            OnSceneDestroyRelease = onSceneDestroyRelease;
            LifeTime = ServiceLifeTime.Transient;
        }

        public DependencyObject(Type serviceType, ServiceLifeTime lifeTime, bool onSceneDestroyRelease)
        {
            ServiceType = serviceType;
            LifeTime = lifeTime;
            OnSceneDestroyRelease = onSceneDestroyRelease;
        }

        public DependencyObject(Type serviceType, Type implementationType, ServiceLifeTime lifeTime, bool onSceneDestroyRelease)
        {
            ServiceType = serviceType;
            LifeTime = lifeTime;
            ImplementationType = implementationType;
            OnSceneDestroyRelease = onSceneDestroyRelease;
        }
    }
}
