using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MiniContainer
{
    /// <summary>
    /// Optimized object list buffer with pooling for Unity Objects
    /// </summary>
    public static class ObjectListBuffer<T> where T : UnityEngine.Object
    {
        private const int DEFAULT_CAPACITY = 32;
        private const int MAX_POOL_SIZE = 8;

        [ThreadStatic]
        private static Stack<List<T>> _pool;
        
        [ThreadStatic]
        private static List<T> _primaryBuffer;

        /// <summary>
        /// Get a list buffer from the pool
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> Get()
        {
            var primary = _primaryBuffer;
            if (primary != null)
            {
                _primaryBuffer = null;
                primary.Clear();
                return primary;
            }
            
            var pool = _pool;
            if (pool?.Count > 0)
            {
                var buffer = pool.Pop();
                buffer.Clear();
                return buffer;
            }

            return new List<T>(DEFAULT_CAPACITY);
        }

        /// <summary>
        /// Return a list buffer to the pool
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(List<T> buffer)
        {
            if (buffer == null) return;

            if (_primaryBuffer == null)
            {
                _primaryBuffer = buffer;
                return;
            }

            var pool = _pool;
            if (pool == null)
            {
                _pool = pool = new Stack<List<T>>(4);
            }

            if (pool.Count < MAX_POOL_SIZE)
            {
                buffer.Clear();
                pool.Push(buffer);
            }
        }

        public readonly struct BufferScope : IDisposable
        {
            private readonly List<T> _buffer;

            public BufferScope(List<T> buffer)
            {
                _buffer = buffer;
            }

            public List<T> Buffer => _buffer;

            public void Dispose()
            {
                Return(_buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferScope GetScoped(out List<T> buffer)
        {
            buffer = Get();
            return new BufferScope(buffer);
        }


    }
}