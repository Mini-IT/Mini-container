using System;

namespace MiniContainer
{
    public class Listeners
    {
        internal WeakReference<IContainerUpdateListener> ContainerUpdate { get; set; }
        internal WeakReference<IContainerSceneLoadedListener> ContainerSceneLoaded{ get; set; }
        internal WeakReference<IContainerSceneUnloadedListener> ContainerSceneUnloaded { get; set; }
        internal WeakReference<IContainerApplicationFocusListener> ContainerApplicationFocus { get; set; }
        internal WeakReference<IContainerApplicationPauseListener> ContainerApplicationPause { get; set; }
    }
}