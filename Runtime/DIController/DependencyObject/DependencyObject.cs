using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MiniContainer
{
    public class DependencyObject
    {
        internal Type ServiceType { get; }

        internal Type ImplementationType { get; }

        internal bool OnSceneDestroyRelease { get; }

        internal bool IsResolved { get; set; }

        internal List<Type> InterfaceTypes { get; }
        
        internal object Implementation { get; set; }

        internal ConcurrentDictionary<int, WeakReference> WeakReferenceMap { get; } = new ConcurrentDictionary<int, WeakReference>();

        internal WeakReference<IDisposable> Disposable { get; set; }
        
        internal ServiceLifeTime LifeTime { get; }

        internal ConcurrentDictionary<int, Listeners> ListenersMap { get; } = new ConcurrentDictionary<int, Listeners>();

        internal int WeakReferenceCount { get; set; }

        internal void SetWeakReference(object impl)
        {
            WeakReferenceMap.TryAdd(WeakReferenceCount, new WeakReference(impl));
        }
        
        internal Listeners GetListeners()
        {
            if (!ListenersMap.TryGetValue(WeakReferenceCount, out var listeners))
            {            
                listeners = ListenersMap[WeakReferenceCount] = new Listeners();
            }
            return listeners;
        }
        
        internal DependencyObject(Type serviceType, Type implementationType, object implementation, ServiceLifeTime lifeTime, List<Type> interfaceTypes, bool onSceneDestroyRelease)
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