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

        private List<IRegistrable> _autoRegistrables;
        
        protected IContainer DIContainer { get; set; }

        protected virtual void Register(IBaseDIService builder) { }

        protected virtual void Resolve() { }

        protected void AutoRegisterAll(IBaseDIService builder)
        {
            if (_autoRegisterGameObjects == null)
            {
                return;
            }
            
            _autoRegistrables = new List<IRegistrable>(_autoRegisterGameObjects.Count);

            foreach (var target in _autoRegisterGameObjects)
            {
                if (target != null)
                {
                    RegisterGameObject(builder, target);
                }
            }
        }

        private void RegisterGameObject(IBaseDIService builder, GameObject go)
        {
            if (go == null)
            {
                return;
            }

            using var bufferScope = ObjectListBuffer<MonoBehaviour>.GetScoped(out var buffer);
            go.GetComponents(buffer);
            
            for (var i = 0; i < buffer.Count; i++)
            {
                var monoBehaviour = buffer[i];
                if (monoBehaviour is IRegistrable registrable)
                {
                    DoRegister(builder, registrable);
                }
            }
        }

        private void DoRegister(IBaseDIService builder, IRegistrable registrable)
        {
            _autoRegistrables.Add(registrable);
            builder.RegisterInstanceAsSelf(registrable);
        }

        protected void AutoResolveAll()
        {
            if (_autoResolveGameObjects == null)
            {
                return;
            }

            foreach (var target in _autoResolveGameObjects)
            {
                if (target != null)
                {
                    ResolveGameObject(target);
                }
            }
        }

        private void ResolveGameObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            using var bufferScope = ObjectListBuffer<MonoBehaviour>.GetScoped(out var buffer);
            go.GetComponents(buffer);
            
            for (var i = 0; i < buffer.Count; i++)
            {
                var monoBehaviour = buffer[i];
                DIContainer.ResolveObject(monoBehaviour);
            }
        }

        protected void OnDestroy()
        {
            if (_autoRegistrables == null)
            {
                return;
            }
            
            foreach (var autoRegistrable in _autoRegistrables)
            {
                DIContainer.Release(autoRegistrable.GetType());
            }
        }
    }
}
