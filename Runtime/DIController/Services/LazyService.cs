namespace MiniContainer
{
    public class LazyService<T> : ILazyService<T>
    {
        private readonly IContainer _container;

        public LazyService(IContainer container)
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