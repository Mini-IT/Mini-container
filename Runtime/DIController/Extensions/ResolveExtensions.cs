﻿using System.Runtime.CompilerServices;

namespace ArcCore
{
    public static class ResolveExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Resolve<T>(this IContainer container)
        {
            return (T)container.Resolve(typeof(T));
        }
    }
}