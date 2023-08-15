using System;

namespace MiniContainer
{
    public class Listeners : IDisposable
    {
        internal IContainerUpdateListener ContainerUpdate { get; set; }
        internal IContainerSceneLoadedListener ContainerSceneLoaded{ get; set; }
        internal IContainerSceneUnloadedListener ContainerSceneUnloaded { get; set; }
        internal IContainerApplicationFocusListener ContainerApplicationFocus { get; set; }
        internal IContainerApplicationPauseListener ContainerApplicationPause { get; set; }

        public void Dispose()
        {
            ContainerUpdate = null;
            ContainerSceneLoaded = null;
            ContainerSceneUnloaded = null;
            ContainerApplicationFocus = null;
            ContainerApplicationPause = null;
        }
    }
}