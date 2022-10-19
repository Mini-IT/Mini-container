using System;

namespace ArcCore
{
    public interface ILazyService<out T> : IDisposable
    {
        T GetService();
    }
}
