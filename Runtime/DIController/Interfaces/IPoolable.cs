namespace MiniContainer
{
    /// <summary>
    /// Interface for objects that need to be reset when returned to pool
    /// </summary>
    public interface IPoolable
    {
        void Reset();
    }
} 