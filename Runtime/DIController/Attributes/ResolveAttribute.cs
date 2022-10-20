
using System;

namespace MiniContainer
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    public class ResolveAttribute : Attribute
    {
    }
}

