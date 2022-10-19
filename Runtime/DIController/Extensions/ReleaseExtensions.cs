using System.Runtime.CompilerServices;

namespace ArcCore
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