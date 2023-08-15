namespace MiniContainer
{
    public class ScopeManager : IScopeManager
    {
        private readonly IContainer _container;

        public ScopeManager(IContainer container)
        {
            _container = container;
        }
        
        public int CreateScope()
        {
            return _container.CreateScope();
        }

        public void ReleaseScope(int scopeID)
        {
            _container.ReleaseScope(scopeID);
        }

        public int GetCurrentScope()
        {
            return _container.GetCurrentScope();
        }

        public void SetCurrentScope(int scopeID)
        {
            _container.SetCurrentScope(scopeID);
        }
    }
}