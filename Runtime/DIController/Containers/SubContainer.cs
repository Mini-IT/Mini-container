using UnityEngine;

namespace MiniContainer
{
    [DefaultExecutionOrder(-6000)]
    public class SubContainer : Container
    {
        protected IBaseDIService DIService { get; private set; }

        public void Init(IBaseDIService diService, IContainer container)
        {
            DIService = diService;
            DIContainer = container;
            AutoRegisterAll();
            Register();
            container.ResolveInstanceRegistered();
            Resolve();
            AutoResolveAll();
        }

        protected virtual void Awake()
        {
            CompositionRoot.Instance.SubContainerInit(this);
        }

        protected override void DoRegister(IRegistrable registrable)
        {
            DIService.RegisterInstanceAsSelf(registrable);
        }
    }
}
