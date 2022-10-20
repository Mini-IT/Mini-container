using System.Runtime.CompilerServices;

namespace MiniContainer
{
    public static class ReleaseExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Release<T>(this IContainer container)
        {
            container.Release(typeof(T));
        }
    }
}