using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MiniContainer
{
    public class DependencyObject : IDisposable
    {
        internal readonly Type ImplementationType;
        internal readonly List<Type> InterfaceTypes;
        internal readonly ServiceLifeTime LifeTime;
        
        private readonly Listeners _listeners;
        
        private readonly Func<object> _getImplementation;
        
        internal Type ServiceType { get; set; }
        internal object Implementation { get; set; }

        private IDisposable _disposable;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DependencyObject(Type serviceType, Type implementationType, object implementation,
            ServiceLifeTime lifeTime, List<Type> interfaceTypes, Func<object> getImplementation = null)
        {
            _listeners = new Listeners();
            ServiceType = serviceType;
            LifeTime = lifeTime;
            ImplementationType = implementationType;
            Implementation = implementation;
            InterfaceTypes = interfaceTypes;
            _getImplementation = getImplementation;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DependencyObject(DependencyObject dependencyObject)
        {
            _listeners = new Listeners();
            ServiceType = dependencyObject.ServiceType;
            LifeTime = dependencyObject.LifeTime;
            ImplementationType = dependencyObject.ImplementationType;
            Implementation = dependencyObject.Implementation;
            InterfaceTypes = dependencyObject.InterfaceTypes;
            _getImplementation = dependencyObject._getImplementation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetImplementation(out object implementation)
        {
            if (_getImplementation != null)
            {
                implementation = _getImplementation();
                return true;
            }
            implementation = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDisposable()
        {
            if (Implementation is IDisposable disposable)
            {
                _disposable = disposable;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetListeners(IContainerListener containerListener)
        {
            _listeners.SetListeners(containerListener, Implementation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {            
            _disposable?.Dispose();
            _disposable = null;
            
            _listeners.Dispose();
            
            Implementation = null;

            if (InterfaceTypes != null && InterfaceTypes.Count > 0)
            {
                InterfaceTypes.Clear();
            }
            
            GC.Collect(0, GCCollectionMode.Optimized);
        }
    }
}