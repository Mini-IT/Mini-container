using System;

namespace MiniContainer
{
    public interface IFactoryService<out T>: IDisposable
    {
        T GetService(Type type);
    }
}
