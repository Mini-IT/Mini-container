using System;
using System.Collections.Generic;

namespace MiniContainer
{
    public class InstanceRegistrationDependencyObject : DependencyObject
    {
        public InstanceRegistrationDependencyObject(Type serviceType,
            Type implementationType,
            object implementation,
            ServiceLifeTime lifeTime,
            List<Type> interfaceTypes) 
            : base(serviceType, implementationType, implementation, lifeTime, interfaceTypes)
        {
        }
    }
}