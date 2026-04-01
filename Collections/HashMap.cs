using System;
using System.Runtime.CompilerServices;

namespace Klondike.Collections {
    public sealed class HashMap<T> where T : IEquatable<T> {
        private struct HashValue<Q> where Q : IEquatable<Q> {
            public uint Hash;
            public Q Value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(Q value, uint hash) {
                Value = value;
                Hash = hash;
            }
        }

        private readonly HashValue<T>[] m_Table;
        private uint m_Count, m_Capacity;

        public HashMap(int maxCount) {
            m_Capacity = (uint)maxCount;
            m_Count = 0;
            m_Table = new HashValue<T>[m_Capacity];
        }

        public int Count => (int)m_Count;

        public void Clear() {
            m_Count = 0;
            Array.Clear(m_Table, 0, (int)m_Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int Add(T key) {
            var hash = (uint)key.GetHashCode();
            uint i = hash % m_Capacity;
            HashValue<T> node = m_Table[i];

            while (node.Hash != 0) {
                if (node.Hash == hash && key.Equals(node.Value)) {
                    return (int)i;
                }

                if (++i >= m_Capacity) {
                    i = 0;
                }

                node = m_Table[i];
            }

            ++m_Count;
            m_Table[i].Set(key, hash);
            return -1;
        }

        public ref T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_Table[index].Value;
        }
    }
}