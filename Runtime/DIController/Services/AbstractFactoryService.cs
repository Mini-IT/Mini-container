using System;
namespace ArcCore
{
    public abstract class AbstractFactoryService<T> : IFactoryService<T>
    {
        private readonly IContainer _container;

        protected AbstractFactoryService(IContainer container)
        {
            _container = container;
        }

        public virtual T GetService(Type type)
        {
            return (T)_container.Resolve(type);
        }

        public virtual void Dispose()
        {
        }
    }
}
