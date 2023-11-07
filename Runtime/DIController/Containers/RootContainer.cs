using UnityEngine;

namespace MiniContainer
{
    [DefaultExecutionOrder(-6500)]
    public abstract class RootContainer : Container
    {
        private IBaseDIService _diService;

		public void Init(IBaseDIService diService, IContainer container)
        {
            _diService = diService;
            DIContainer = container;
            AutoRegisterAll();
            Register(diService);
        }

        public void ResolveContainer()
        {
            DIContainer.ResolveInstanceRegistered();
            Resolve();
            AutoResolveAll();
        }

        protected sealed override void DoRegister(IRegistrable registrable)
        {
            _diService.RegisterInstanceAsSelf(registrable);
        }
    }
}