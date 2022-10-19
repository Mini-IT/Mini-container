using System;

namespace ArcCore
{
    public interface IContainer
    {
        object Resolve(Type serviceType);
        void ResolveObject(object implementation);
        void ResolveInstanceRegistered(bool onSceneDestroyRelease);
        void Release(Type type);
        void ReleaseAll();
        void ReleaseScene();
        void RunUpdate();
        void RunSceneLoaded(int scene);
        void RunSceneUnloaded(int scene);
        void RunApplicationFocus(bool focus);
        void RunApplicationPause(bool pause);
    }
}