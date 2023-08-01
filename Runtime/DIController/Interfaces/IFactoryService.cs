using System;

namespace MiniContainer
{
    public interface IFactoryService<T>
    {
        T GetService(Type type);
        TService GetService<TService>() where TService : T;
    }
}
