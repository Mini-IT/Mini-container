namespace MiniContainer
{
    public abstract class RootContainer : Container
    {
        protected IDIService DIService { get; private set; }

        public void Init(IDIService diService, IContainer container)
        {
            DIService = diService;
            DIContainer = container;
            AutoRegisterAll();
            Register();
            container.ResolveInstanceRegistered(false);
            Resolve();
            AutoResolveAll();
        }

        protected override void DoRegister(IRegistrable registrable)
        {
            DIService.RegisterInstanceAsSelf(registrable);
        }
    }
}
