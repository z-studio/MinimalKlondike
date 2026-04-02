using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Klondike.Entities {
    /// <summary>
    /// 搜索路径上的一个结点：一步棋 + 父结点在 <see cref="Board"/> 的 nodeStorage 里的下标（-1 表示无父）。
    /// 整条路径通过 Parent 链回溯，避免为每个状态存完整 Move 数组。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 6, Pack = 2)]
    public struct MoveNode {
        /// <summary>
        /// 父结点在同一个 MoveNode[] 中的下标；根为 -1。
        /// </summary>
        public int Parent;
        public Move Move;

        /// <summary>
        /// 从本结点沿 Parent 链把路径上的 Move 写入 destination[0..)。
        /// 顺序为「当前步 → … → 最早一步」；调用方需倒序 MakeMove 才能按时间正序重放。
        /// </summary>
        /// <returns>写入的步数。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int Copy(Move[] destination, MoveNode[] moveList) {
            var index = 0;

            if (Move.IsNull) {
                return 0;
            }

            destination[index++] = Move;
            int parentIndex = Parent;

            while (parentIndex >= 0) {
                MoveNode parent = moveList[parentIndex];

                // 池里用 Move 为空的结点作哨兵，表示链到头
                if (parent.Move.IsNull) {
                    break;
                }

                destination[index++] = parent.Move;
                parentIndex = parent.Parent;
            }

            return index;
        }

        public override string ToString() {
            return $"{Move}";
        }
    }
}
