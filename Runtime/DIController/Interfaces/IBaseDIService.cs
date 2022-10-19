namespace ArcCore
{
    public interface IBaseDIService
    {
        void Register<T>(T dependencyObject) where T : DependencyObject;
    }
}