using System;
using UnityEngine;

namespace MiniContainer
{
    internal static class Logs
    {
        public static void InvalidOperation(string message)
        {
            throw new InvalidOperationException(message);
        }
        
        public static void Log(string message)
        {
            Debug.Log(message);
        }
    }
}