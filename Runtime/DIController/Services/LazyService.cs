namespace MiniContainer
{
    public class LazyService<T> : ILazyService<T>
    {
        private readonly DIContainer _container;

        public LazyService(DIContainer container)
        {
            _container = container;
        }

        public T GetService()
        {
            return _container.Resolve<T>();
        }

        public void Dispose()
        {
        }
    }
}
