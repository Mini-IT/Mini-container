using System;

namespace ArcCore
{
    public interface IFactoryService<out T>: IDisposable
    {
        T GetService(Type type);
    }
}
