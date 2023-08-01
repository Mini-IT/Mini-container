using System;

namespace MiniContainer
{
    public class FactoryService<T> : IFactoryService<T>
    {
        private readonly IContainer _container;

        public FactoryService(IContainer container)
        {
            _container = container;
        }

        public T GetService(Type type)
        {
            return (T)_container.Resolve(type);
        }

        public TService GetService<TService>() where TService : T 
        {
            return _container.Resolve<TService>();
        }
    }
}