using UnityEngine;

namespace MiniContainer
{
    [DefaultExecutionOrder(-6000)]
    public class SubContainer : Container
    {
        public void Init(IBaseDIService builder, IContainer container)
        {
            DIContainer = container;
            AutoRegisterAll(builder);
            Register(builder);
            container.ResolveInstanceRegistered();
            Resolve();
            AutoResolveAll();
        }

        protected virtual void Awake()
        {
            CompositionRoot.Instance.SubContainerInit(this);
        }
    }
}
