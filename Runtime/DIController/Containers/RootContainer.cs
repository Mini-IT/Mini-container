using UnityEngine;

namespace MiniContainer
{
    [DefaultExecutionOrder(-6500)]
    public abstract class RootContainer : Container
    {
        public void Init(IBaseDIService builder, IContainer container)
        {
            DIContainer = container;
            AutoRegisterAll(builder);
            Register(builder);
        }

        public void ResolveContainer()
        {
            DIContainer.ResolveInstanceRegistered();
            Resolve();
            AutoResolveAll();
        }
    }
}