using System;
using System.Collections.Generic;

namespace ArcCore
{
    public static class ObjectListBuffer<T> where T : UnityEngine.Object
    {
        private const int DEFAULT_CAPACITY = 32;

        [ThreadStatic]
        private static List<T> _instance;

        public static List<T> Get()
        {
            if (_instance == null)
            {
                _instance = new List<T>(DEFAULT_CAPACITY);
            }

            _instance.Clear();
            return _instance;
        }
    }
}