using UnityEngine;

namespace MiniContainer
{
    [DefaultExecutionOrder(-6000)]
    public class SubContainer : Container
    {
        private IBaseDIService _diService;

        public void Init(IBaseDIService diService, IContainer container)
        {
            _diService = diService;
            DIContainer = container;
            AutoRegisterAll();
            Register(diService);
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
            _diService.RegisterInstanceAsSelf(registrable);
        }
    }
}
