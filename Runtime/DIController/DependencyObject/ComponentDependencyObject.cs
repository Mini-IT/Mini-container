using System;
using UnityEngine;

namespace MiniContainer
{
    public class ComponentDependencyObject : DependencyObject
    {
        public Component Prefab { get; }
        public Transform Parent { get; }
        public string GameObjectName { get; }

        public ComponentDependencyObject(Component prefab, Transform parent, Type serviceType, bool onSceneDestroyRelease) 
            : base(serviceType, onSceneDestroyRelease)
        {
            Prefab = prefab;
            Parent = parent;
        }

        public ComponentDependencyObject(string gameObjectName, Transform parent, Type serviceType, bool onSceneDestroyRelease)
            : base(serviceType, onSceneDestroyRelease)
        {
            GameObjectName = gameObjectName;
            Parent = parent;
        }
    }
}