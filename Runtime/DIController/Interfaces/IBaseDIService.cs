using System;

namespace MiniContainer
{
    public interface IBaseDIService
    {
        T Register<T>(T registration) where T : Registration;
        void IgnoreType<T>(T type) where T : Type;
    }
}