using System;

namespace MiniContainer
{
    internal static class Errors
    {
        public static void InvalidOperation(string message)
        {
            throw new InvalidOperationException(message);
        }
    }
}