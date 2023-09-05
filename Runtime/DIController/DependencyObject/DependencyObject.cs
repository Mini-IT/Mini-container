using System;
using System.Collections.Generic;

namespace MiniContainer
{
    public class DependencyObject : IDisposable
    {
        internal Type ServiceType { get; set; }

        internal Type ImplementationType { get; }
        
        internal List<Type> InterfaceTypes { get; }

        internal object Implementation { get; set; }
        
        internal ServiceLifeTime LifeTime { get; }
        
        internal Listeners Listeners { get; }
        
        private IDisposable _disposable;

        internal DependencyObject(Type serviceType, Type implementationType, object implementation,
            ServiceLifeTime lifeTime, List<Type> interfaceTypes)
        {
            Listeners = new Listeners();
            ServiceType = serviceType;
            LifeTime = lifeTime;
            ImplementationType = implementationType;
            Implementation = implementation;
            InterfaceTypes = interfaceTypes;
        }
        
        internal DependencyObject(DependencyObject dependencyObject)
        {
            Listeners = new Listeners();
            ServiceType = dependencyObject.ServiceType;
            LifeTime = dependencyObject.LifeTime;
            ImplementationType = dependencyObject.ImplementationType;
            Implementation = dependencyObject.Implementation;
            InterfaceTypes = dependencyObject.InterfaceTypes;
        }

        public void TryToSetDisposable()
        {
            if (Implementation is IDisposable disposable)
            {
                _disposable = disposable;
            }
        }

        public void TryToSetListeners()
        {
            if (Implementation is IContainerSceneLoadedListener containerSceneLoaded)
            {
                Listeners.ContainerSceneLoaded = containerSceneLoaded;
            }
            
            if (Implementation is IContainerUpdateListener containerUpdate)
            {
                Listeners.ContainerUpdate = containerUpdate;
            }
            
            if (Implementation is IContainerSceneUnloadedListener containerSceneUnloaded)
            {
                Listeners.ContainerSceneUnloaded = containerSceneUnloaded;
            }

            if (Implementation is IContainerApplicationFocusListener containerApplicationFocus)
            {
                Listeners.ContainerApplicationFocus = containerApplicationFocus;
            }

            if (Implementation is IContainerApplicationPauseListener containerApplicationPause)
            {
                Listeners.ContainerApplicationPause = containerApplicationPause;
            }
        }

        public void Dispose()
        {
            Implementation = null;
            _disposable?.Dispose();
            Listeners.Dispose();
        }
    }
}