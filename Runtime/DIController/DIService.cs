using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace ArcCore
{
    public class DIService : IDIService
    {
        private readonly ConcurrentDictionary<Type, DependencyObject> _serviceDictionary;

        public DIService()
        {
            _serviceDictionary = new ConcurrentDictionary<Type, DependencyObject>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register<T>(T dependencyObject) where T : DependencyObject
        {
            if (!_serviceDictionary.TryAdd(dependencyObject.ServiceType, dependencyObject))
            {
                throw new Exception($"Already exist {dependencyObject.ServiceType}");
            }
        }

        public DIContainer GenerateContainer()
        {
            return new DIContainer(_serviceDictionary);
        }
    }
}
