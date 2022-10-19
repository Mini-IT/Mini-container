namespace ArcCore
{
    public class ServiceProvider : IServiceProvider
    {
        private readonly IContainer _container;

        public ServiceProvider(IContainer container)
        {
            _container = container;
        }

        public T GetService<T>()
        {
            return _container.Resolve<T>();
        }

        public void Dispose()
        {
        }
    }
}
