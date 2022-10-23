using System.Collections.Generic;
using UnityEngine;

namespace MiniContainer
{
    public abstract class Container :  MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> _autoRegisterGameObjects;

        [SerializeField]
        private List<GameObject> _autoInjectGameObjects;

        protected IContainer DIContainer { get; set; }

        protected virtual void Register() { }

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
                else
                {
                    Debug.LogError("IRegistrable not found");
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
            }
        }

        protected abstract void DoRegister(IRegistrable registrable);

        protected void AutoInjectAll()
        {
            if (_autoInjectGameObjects == null)
                return;

            foreach (var target in _autoInjectGameObjects)
            {
                if (target != null) // Check missing reference
                {
                    InjectGameObject(target);
                }
            }
        }

        private void InjectGameObject(GameObject go)
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
