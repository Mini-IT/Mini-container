using System;

namespace MiniContainer
{
    public interface IContainerListener
    {
        event Action OnContainerUpdate;
        event Action<int> OnContainerSceneLoaded;
        event Action<int> OnContainerSceneUnloaded;
        event Action<bool> OnContainerApplicationFocus;
        event Action<bool> OnContainerApplicationPause;
    }
}