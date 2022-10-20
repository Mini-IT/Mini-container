using System;

namespace MiniContainer
{
    public interface ILazyService<out T> : IDisposable
    {
        T GetService();
    }
}
