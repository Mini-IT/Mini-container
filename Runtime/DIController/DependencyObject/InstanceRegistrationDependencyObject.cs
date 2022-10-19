using System;

namespace ArcCore
{
    public class InstanceRegistrationDependencyObject : DependencyObject
    {
        public InstanceRegistrationDependencyObject(Type serviceType, object implementation, bool onSceneDestroyRelease)
            : base(serviceType, implementation, onSceneDestroyRelease)
        {
        }
    }
}