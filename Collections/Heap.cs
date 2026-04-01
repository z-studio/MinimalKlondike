using System;
using System.Runtime.CompilerServices;

namespace Klondike.Collections {
    /// <summary>
    /// 二叉最小堆：始终保证堆顶（下标 0）是「最小」元素（按 T.CompareTo）。
    /// 用一维数组存完全二叉树：下标 i 的父结点为 (i-1)/2，左子为 2i+1，右子为 2i+2。
    /// </summary>
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

        /// <summary>
        /// 入队：把新元素放到数组末尾，再「向上冒泡」直到满足父 ≤ 子（最小堆性质）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T node) {
            // 新结点先插在尾部；current 表示这一轮要比较的「空洞」下标（从下往上移动）
            int current = m_Size++;

            while (current > 0) {
                int child = current;

                // 父结点下标（与 (current - 1) / 2 等价，位运算略快）
                current = (current - 1) >> 1;
                T value = m_Nodes[current];

                // 新元素已经 ≥ 父结点，可以放在 child 处，堆性质成立
                if (node.CompareTo(value) >= 0) {
                    current = child;
                    break;
                }

                // 新元素比父结点更小：父的值不该留在孩子上方，把父结点的值「拉下来」写到 child，空洞上移到父下标（下一轮循环里 current 即该下标）
                m_Nodes[child] = value;
            }

            m_Nodes[current] = node;
        }

        /// <summary>
        /// 出队：取出最小元（根），用最后一个元素填根，再「向下沉降」恢复堆。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public T Dequeue() {
            T result = m_Nodes[0];
            int current = 0;

            // 末尾元素暂时当作要下沉的值；堆大小先减 1
            T end = m_Nodes[--m_Size];

            do {
                int last = current;

                // 左孩子下标：current * 2 + 1
                int child = (current << 1) + 1;

                if (m_Size > child && end.CompareTo(m_Nodes[child]) > 0) {
                    current = child;

                    // 若右孩子存在且比左孩子更小，则沿更小的那个孩子下沉
                    if (m_Size > ++child && m_Nodes[current].CompareTo(m_Nodes[child]) > 0) {
                        current = child;
                    }
                } else if (m_Size > ++child && end.CompareTo(m_Nodes[child]) > 0) {
                    // 左孩子不存在或不必走左子树时，尝试只走右孩子（child 已在上一分支可能被 ++，此处为右孩子下标）
                    current = child;
                } else {
                    break;
                }

                // 把更小的孩子上移到 last，空洞下移到 current
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