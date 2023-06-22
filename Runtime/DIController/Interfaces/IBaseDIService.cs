namespace MiniContainer
{
    public interface IBaseDIService
    {
        T Register<T>(T registration) where T : IRegistration;
    }
}