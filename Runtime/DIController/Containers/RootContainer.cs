using UnityEngine;

namespace MiniContainer
{
    [DefaultExecutionOrder(-6500)]
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

        protected sealed override void DoRegister(IRegistrable registrable)
        {
            DIService.RegisterInstanceAsSelf(registrable);
        }
    }
}
