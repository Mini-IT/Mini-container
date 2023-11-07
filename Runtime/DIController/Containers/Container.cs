using System.Collections.Generic;
using UnityEngine;

namespace MiniContainer
{
    public abstract class Container :  MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> _autoRegisterGameObjects;

        [SerializeField]
        private List<GameObject> _autoResolveGameObjects;
        
        protected IContainer DIContainer { get; set; }

        protected virtual void Register(IBaseDIService builder) { }

        protected virtual void Resolve() { }

        protected void AutoRegisterAll()
        {
            if (_autoRegisterGameObjects == null)
                return;

            foreach (var target in _autoRegisterGameObjects)
            {
                if (target != null) // Check missing reference
                {
                    RegisterGameObject(target);
                }
            }
        }

        private void RegisterGameObject(GameObject go)
        {
            var buffer = ObjectListBuffer<MonoBehaviour>.Get();

            if (go == null) return;

            buffer.Clear();
            go.GetComponents(buffer);
            foreach (var monoBehaviour in buffer)
            {
                if (monoBehaviour is IRegistrable registrable)
                {
                    DoRegister(registrable);
                }
                //else
                //{
                //    Debug.LogError($"AutoRegisterGameObject cannot be registered because {monoBehaviour.GetType()} doesn't implement IRegistrable");
                //}
            }
        }

        protected abstract void DoRegister(IRegistrable registrable);
        
        protected void AutoResolveAll()
        {
            if (_autoResolveGameObjects == null)
                return;

            foreach (var target in _autoResolveGameObjects)
            {
                if (target != null) // Check missing reference
                {
                    ResolveGameObject(target);
                }
            }
        }

        private void ResolveGameObject(GameObject go)
        {
            var buffer = ObjectListBuffer<MonoBehaviour>.Get();

            if (go == null) return;

            buffer.Clear();
            go.GetComponents(buffer);
            foreach (var monoBehaviour in buffer)
            {
                DIContainer.ResolveObject(monoBehaviour);
            }
        }

    }
}
