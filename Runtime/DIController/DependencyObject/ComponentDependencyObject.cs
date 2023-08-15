using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniContainer
{
    public class ComponentDependencyObject : DependencyObject
    {
        public Component Prefab { get; }
        public Transform Parent { get; }
        public string GameObjectName { get; }

        public ComponentDependencyObject(Type serviceType,
            Type implementationType,
            object implementation,
            ServiceLifeTime lifeTime,
            List<Type> interfaceTypes,
            Component prefab,
            Transform parent,
            string gameObjectName) 
            : base(serviceType, implementationType, implementation, lifeTime, interfaceTypes)
        {
            Prefab = prefab;
            Parent = parent;
            GameObjectName = gameObjectName;
        }
    }
}