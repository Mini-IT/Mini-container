using UnityEngine;

namespace MiniContainer
{
    [DefaultExecutionOrder(-6000)]
    public class SubContainer : Container
    {
        protected IBaseDIService DIService;

        public void Init(IBaseDIService diService, IContainer container)
        {
            DIService = diService;
            DIContainer = container;
            AutoRegisterAll();
            Register();
            container.ResolveInstanceRegistered(true);
            Resolve();
        }

        private void Awake()
        {
            CompositionRoot.Instance.SubContainerInit(this);
            AutoResolveAll();
        }

        protected override void DoRegister(IRegistrable registrable)
        {
            DIService.RegisterInstanceAsSelf(registrable);
        }
    }
}
