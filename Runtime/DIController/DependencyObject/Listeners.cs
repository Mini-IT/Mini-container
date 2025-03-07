using System;
using System.Runtime.CompilerServices;

namespace MiniContainer
{
    public class Listeners : IDisposable
    {
        private IContainerUpdateListener _containerUpdate;
        private IContainerSceneLoadedListener _containerSceneLoaded;
        private IContainerSceneUnloadedListener _containerSceneUnloaded;
        private IContainerApplicationFocusListener _containerApplicationFocus;
        private IContainerApplicationPauseListener _containerApplicationPause;
        private IContainerListener _containerListener;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_containerSceneLoaded != null)
                _containerListener.OnContainerSceneLoaded -= _containerSceneLoaded.OnSceneLoaded;
            if (_containerUpdate != null)
                _containerListener.OnContainerUpdate -= _containerUpdate.Update;
            if (_containerSceneUnloaded != null)
                _containerListener.OnContainerSceneUnloaded -= _containerSceneUnloaded.OnSceneUnloaded;
            if (_containerApplicationFocus != null)
                _containerListener.OnContainerApplicationFocus -= _containerApplicationFocus.OnApplicationFocus;
            if (_containerApplicationPause != null)
                _containerListener.OnContainerApplicationPause -= _containerApplicationPause.OnApplicationPause;
            
            _containerUpdate = null;
            _containerSceneLoaded = null;
            _containerSceneUnloaded = null;
            _containerApplicationFocus = null;
            _containerApplicationPause = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetListeners(IContainerListener containerListener, object implementation)
        {
            _containerListener = containerListener;
            if (implementation is IContainerSceneLoadedListener containerSceneLoaded)
            {
                _containerSceneLoaded = containerSceneLoaded;
                _containerListener.OnContainerSceneLoaded += _containerSceneLoaded.OnSceneLoaded;
            }
            
            if (implementation is IContainerUpdateListener containerUpdate)
            {
                _containerUpdate = containerUpdate;
                _containerListener.OnContainerUpdate += _containerUpdate.Update;
            }
            
            if (implementation is IContainerSceneUnloadedListener containerSceneUnloaded)
            {
                _containerSceneUnloaded = containerSceneUnloaded;
                _containerListener.OnContainerSceneUnloaded += _containerSceneUnloaded.OnSceneUnloaded;
            }

            if (implementation is IContainerApplicationFocusListener containerApplicationFocus)
            {
                _containerApplicationFocus = containerApplicationFocus;
                _containerListener.OnContainerApplicationFocus += _containerApplicationFocus.OnApplicationFocus;
            }

            if (implementation is IContainerApplicationPauseListener containerApplicationPause)
            {
                _containerApplicationPause = containerApplicationPause;
                _containerListener.OnContainerApplicationPause += _containerApplicationPause.OnApplicationPause;
            }
        }
    }
}