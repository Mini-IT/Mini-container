using UnityEngine;

namespace ArcCore
{
    [DefaultExecutionOrder(-6000)]
    public class SubContainer : Container
    {
        protected IBaseDIService DiService;

        public void Init(IBaseDIService diService, IContainer container)
        {
            DiService = diService;
            DIContainer = container;
            Register();
            container.ResolveInstanceRegistered(true);
            Resolve();
        }

        private void Awake()
        {
            CompositionRoot.Instance.SubContainerInit(this);
            AutoInjectAll();
        }
    }
}
