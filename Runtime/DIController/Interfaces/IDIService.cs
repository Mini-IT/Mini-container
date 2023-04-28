using System;

namespace MiniContainer
{
    public interface IDIService : IBaseDIService
    {
        void IgnoreType<T>(T type) where T : Type;
        DIContainer GenerateContainer();
    }
}