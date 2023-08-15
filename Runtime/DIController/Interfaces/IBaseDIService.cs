using System;

namespace MiniContainer
{
    public interface IBaseDIService
    {
        T Register<T>(T registration) where T : IRegistration;
        void IgnoreType<T>(T type) where T : Type;
    }
}