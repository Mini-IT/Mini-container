
using System;

namespace ArcCore
{
    public interface IServiceProvider: IDisposable
    {
        T GetService<T>();
    }
}
