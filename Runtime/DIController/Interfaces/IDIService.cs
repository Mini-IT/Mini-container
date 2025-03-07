namespace MiniContainer
{
    public interface IDIService : IBaseDIService
    {
        DIContainer GenerateContainer(bool enablePooling = true, 
            bool enableParallelInitialization = true);
    }
}