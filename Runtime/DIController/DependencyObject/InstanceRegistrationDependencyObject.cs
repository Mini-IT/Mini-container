using System;

namespace MiniContainer
{
    public class InstanceRegistrationDependencyObject : DependencyObject
    {
        public InstanceRegistrationDependencyObject(Type serviceType, object implementation, bool onSceneDestroyRelease)
            : base(serviceType, implementation, onSceneDestroyRelease)
        {
        }
    }
}