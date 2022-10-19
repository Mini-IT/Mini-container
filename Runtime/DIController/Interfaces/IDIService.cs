namespace ArcCore
{
    public interface IDIService : IBaseDIService
    {
        DIContainer GenerateContainer();
    }
}