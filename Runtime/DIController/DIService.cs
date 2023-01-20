using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MiniContainer
{
    public class DIService : IDIService
    {
        private readonly List<Registration> _registrations;

        public DIService()
        {
            _registrations = new List<Registration>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Register<T>(T registration) where T : Registration
        {
            _registrations.Add(registration);
            return registration;
        }

        public DIContainer GenerateContainer()
        {
            return new DIContainer(_registrations);
        }
    }
}
