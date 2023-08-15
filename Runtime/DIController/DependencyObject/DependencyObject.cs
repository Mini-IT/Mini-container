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

        internal IDisposable Disposable { get; set; }

        internal ServiceLifeTime LifeTime { get; }

        internal Listeners Listeners { get; }
        
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

        public void Dispose()
        {
            Implementation = null;
            Disposable?.Dispose();
            Listeners.Dispose();
        }
    }
}