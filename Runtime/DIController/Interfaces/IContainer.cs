using System;

namespace MiniContainer
{
    public interface IContainer
    {
        object Resolve(Type serviceType);
        void ResolveObject(object implementation);
        void ResolveInstanceRegistered();
        void ReleaseAll();
        void Release(Type type);
        void RunUpdate();
        void RunSceneLoaded(int scene);
        void RunSceneUnloaded(int scene);
        void RunApplicationFocus(bool focus);
        void RunApplicationPause(bool pause);
        int CreateScope();
        void ReleaseScope(int scopeID);
        int GetCurrentScope();
        void SetCurrentScope(int scopeID);
    }
}