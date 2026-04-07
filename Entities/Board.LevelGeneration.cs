using System;
using System.Runtime.CompilerServices;

namespace Klondike.Entities {
    /// <summary>
    /// 关卡生成 / 分析用扩展。
    /// </summary>
    public unsafe sealed partial class Board {
        /// <summary>
        /// 当前已记录的走法（<see cref="Solve"/> / <see cref="PlayMoves"/> 等之后）；供关卡分析重放，勿修改底层数组。
        /// </summary>
        public ReadOnlySpan<Move> RecordedMoves =>
            m_MovesTotal <= 0
                ? ReadOnlySpan<Move>.Empty
                : new ReadOnlySpan<Move>(m_MovesMade, 0, m_MovesTotal);

        /// <summary>
        /// 只读访问指定摞；<paramref name="pileIndex"/> 与内部常量 <c>kWastePile</c>、<c>kTableauStart</c> 等一致。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pile GetPile(int pileIndex) => m_Piles[pileIndex];
    }
}