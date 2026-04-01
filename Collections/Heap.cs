using System;
using System.Runtime.CompilerServices;

namespace Klondike.Collections {
    public sealed class Heap<T> where T : IComparable<T> {
        private int m_Size;
        private T[] m_Nodes;

        public Heap(int maxNodes) {
            m_Size = 0;
            m_Nodes = new T[maxNodes];
        }

        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            m_Size = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T node) {
            int current = m_Size++;

            while (current > 0) {
                int child = current;
                current = (current - 1) >> 1;
                T value = m_Nodes[current];

                if (node.CompareTo(value) >= 0) {
                    current = child;
                    break;
                }

                m_Nodes[child] = value;
            }

            m_Nodes[current] = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public T Dequeue() {
            T result = m_Nodes[0];
            int current = 0;
            T end = m_Nodes[--m_Size];

            do {
                int last = current;
                int child = (current << 1) + 1;

                if (m_Size > child && end.CompareTo(m_Nodes[child]) > 0) {
                    current = child;

                    if (m_Size > ++child && m_Nodes[current].CompareTo(m_Nodes[child]) > 0) {
                        current = child;
                    }
                } else if (m_Size > ++child && end.CompareTo(m_Nodes[child]) > 0) {
                    current = child;
                } else {
                    break;
                }

                m_Nodes[last] = m_Nodes[current];
            } while (true);

            m_Nodes[current] = end;
            return result;
        }

        private void Resize() {
            var newNodes = new T[(int)(m_Nodes.Length * 1.5)];

            for (var i = 0; i < m_Nodes.Length; i++) {
                newNodes[i] = m_Nodes[i];
            }

            m_Nodes = newNodes;
        }

        public override string ToString() {
            return $"Count = {Count}";
        }
    }
}