namespace MiniContainer
{
    public interface IBaseDIService
    {
        void Register<T>(T dependencyObject) where T : DependencyObject;
    }
}