
using System;

namespace ArcCore
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    public class ResolveAttribute : Attribute
    {
    }
}

