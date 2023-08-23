using UnityEngine;

namespace MiniContainer
{
    [DefaultExecutionOrder(-6500)]
    public abstract class RootContainer : Container
    {
        protected IBaseDIService DIService { get; private set; }

        public void Init(IBaseDIService diService, IContainer container)
        {
            DIService = diService;
            DIContainer = container;
            AutoRegisterAll();
            Register();
        }

        public void ResolveContainer()
        {
            DIContainer.ResolveInstanceRegistered();
            Resolve();
            AutoResolveAll();
        }

        protected sealed override void DoRegister(IRegistrable registrable)
        {
            DIService.RegisterInstanceAsSelf(registrable);
        }
    }
}