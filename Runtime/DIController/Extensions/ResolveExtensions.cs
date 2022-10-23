using System.Runtime.CompilerServices;

namespace MiniContainer
{
    public static class ResolveExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Resolve<T>(this IContainer container)
        {
            return (T)container.Resolve(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetOrResolve<T>(this IContainer container)
        {
            var impl = (T)container.GetInstance(typeof(T)) ?? (T)container.Resolve(typeof(T));
            return impl;
        }
    }
}