namespace ArcCore
{
    public abstract class RootContainer : Container
    {
        protected IDIService DiService { get; private set; }

        public void Init(IDIService diService, IContainer container)
        {
            DiService = diService;
            DIContainer = container;
            Register();
            container.ResolveInstanceRegistered(false);
            Resolve();
            AutoInjectAll();
        }
    }
}
