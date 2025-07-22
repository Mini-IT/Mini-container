namespace MiniContainer
{
    public interface IDIService : IBaseDIService
    {
        DIContainer GenerateContainer();
    }
}