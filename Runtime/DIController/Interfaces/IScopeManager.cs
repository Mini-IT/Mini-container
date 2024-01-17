namespace MiniContainer
{
    public interface IScopeManager
    {
        int CreateScope();
        void ReleaseScope(int scopeID);
        int GetCurrentScope();
        void SetCurrentScope(int scopeID);
    }
}