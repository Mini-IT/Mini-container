namespace MiniContainer
{
    public abstract class RootContainer : Container
    {
        protected IDIService DIService { get; private set; }

        public void Init(IDIService diService, IContainer container)
        {
            DIService = diService;
            DIContainer = container;
            Register();
            container.ResolveInstanceRegistered(false);
            Resolve();
            AutoInjectAll();
        }
    }
}
