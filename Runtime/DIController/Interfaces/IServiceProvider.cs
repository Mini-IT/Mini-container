
using System;

namespace MiniContainer
{
    public interface IServiceProvider: IDisposable
    {
        T GetService<T>();
    }
}
