using System;
using System.Runtime.CompilerServices;

namespace Klondike.Entities {
    /// <summary>
    /// 一摞牌在共享大数组 <see cref="m_Cards"/> 里的一段连续窗口，由 <see cref="Board"/> 为每列/堆分配固定槽位。
    /// 逻辑下标：<c>m_Index + 0</c> 为牌列**最底下**（先发入列的那张），<c>m_Index + Size - 1</c> 为最顶上可动区。
    /// 属性命名：<see cref="Bottom"/> 指「数组末尾那张」即**画面上摞顶**、常作为出牌/接龙参考；
    /// <see cref="Top"/> 指明牌区从 <see cref="First"/> 起，是**明牌串的底端**（紧贴暗牌的第一张明牌）。
    /// </summary>
    public unsafe struct Pile : IComparable<Pile> {
        /// <summary>
        /// 当前摞中牌的张数。
        /// </summary>
        public int Size;

        /// <summary>
        /// 列内下标：从该下标到 <c>Size-1</c> 为**明牌**；<c>0..First-1</c> 为暗牌（仅 tableau 等场景使用）。
        /// <c>-1</c> 表示空摞或未初始化分界。翻牌时由 <see cref="Flip"/> 把分界移到「多露出若干张」的位置。
        /// </summary>
        public int First;

        /// <summary>
        /// 本摞在 <see cref="m_Cards"/> 中的起始偏移（由 Board 布局计算，构造后不变）。
        /// </summary>
        private readonly int m_Index;

        /// <summary>
        /// 全桌共用的一张牌大数组；各 <see cref="Pile"/> 只操作自己的 <c>[m_Index .. m_Index+容量)</c> 子区间。
        /// </summary>
        private readonly Card[] m_Cards;

        /// <summary>
        /// 在共享缓冲上为本摞登记起始偏移；容量由 Board 按堆类型预留。
        /// </summary>
        public Pile(Card[] cards, int index) {
            m_Cards = cards;
            m_Index = index;
            Size = 0;
            First = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() {
            Size = 0;
            First = -1;
        }

        /// <summary>
        /// 调整明牌起点：新明牌为「顶上 <paramref name="count"/> 张」，即 <c>First = Size - count</c>。
        /// 默认 <paramref name="count"/> 为 1 时等价于翻开最顶一张。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flip(int count = 1) {
            First = Size - count;
        }

        /// <summary>
        /// 从列顶压入一张牌（写在 <c>m_Index + Size</c>，再自增 <see cref="Size"/>）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Card card) {
            m_Cards[m_Index + Size++] = card;
        }

        /// <summary>
        /// 将本摞**最顶**一张（<see cref="Bottom"/> 位置）移到 <paramref name="to"/> 摞顶；本摞 <see cref="Size"/> 减 1。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(ref Pile to) {
            to.Add(m_Cards[m_Index + --Size]);
        }

        /// <summary>
        /// 将本摞顶上连续 <paramref name="count"/> 张**原顺序**复制到 <paramref name="to"/> 摞顶，再从本摞删除。
        /// 两摞必须同一块 <see cref="m_Cards"/>（Board 布局保证区间不重叠或调用方负责不重叠）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(ref Pile to, int count) {
            int fromIndex = m_Index + Size - count;
            int toIndex = to.m_Index + to.Size;
            var source = new Span<Card>(m_Cards, fromIndex, count);
            var destination = new Span<Card>(m_Cards, toIndex, count);
            source.CopyTo(destination);
            Size -= count;
            to.Size += count;
        }

        /// <summary>
        /// 与 <see cref="Remove(ref Pile, int)"/> 相同，但在目标区间上 <see cref="Span{T}.Reverse"/>，
        /// 用于发牌堆/废牌堆整段翻转（顺序与物理翻牌一致）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveFlip(ref Pile to, int count) {
            int fromIndex = m_Index + Size - count;
            int toIndex = to.m_Index + to.Size;
            var source = new Span<Card>(m_Cards, fromIndex, count);
            var destination = new Span<Card>(m_Cards, toIndex, count);
            source.CopyTo(destination);
            destination.Reverse();
            Size -= count;
            to.Size += count;
        }

        /// <summary>
        /// 列内第 <paramref name="index"/> 张牌，<c>0</c> 为最底。
        /// </summary>
        public Card this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Cards[m_Index + index];
        }

        /// <summary>
        /// 最顶一张；空摞为 <see cref="Card.Empty"/>。
        /// </summary>
        public Card Bottom {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Size > 0 ? m_Cards[m_Index + Size - 1] : Card.Empty;
        }

        /// <summary>
        /// 同 <see cref="Bottom"/>，调用方保证 <see cref="Size"/> &gt; 0。
        /// </summary>
        public Card BottomNoCheck {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Cards[m_Index + Size - 1];
        }

        /// <summary>
        /// 明牌区底端一张（下标 <see cref="First"/>）；空摞为 <see cref="Card.Empty"/>。
        /// </summary>
        public Card Top {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Size > 0 ? m_Cards[m_Index + First] : Card.Empty;
        }

        /// <summary>
        /// 同 <see cref="Top"/>，调用方保证 <see cref="Size"/> &gt; 0。
        /// </summary>
        public Card TopNoCheck {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Cards[m_Index + First];
        }

        /// <summary>
        /// 从 <see cref="Bottom"/>（列顶）向**列底**方向数第 <paramref name="size"/> 张：数组下标 <c>Size - size - 1</c>。
        /// <c>Up(1)</c> 即紧贴列顶下面的那张；Board 里用 <c>j = 1 .. UpSize-1</c> 沿明牌串校验红黑交替。
        /// 越界返回 <see cref="Card.Empty"/>。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Card Up(int size) {
            int index = Size - size - 1;
            return index >= 0 ? m_Cards[m_Index + index] : Card.Empty;
        }

        /// <summary>
        /// 同 <see cref="Up"/>，调用方保证下标合法。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Card UpNoCheck(int size) {
            return m_Cards[m_Index + Size - size - 1];
        }

        /// <summary>
        /// 当前明牌张数，即 <c>Size - First</c>。
        /// </summary>
        public int UpSize {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Size - First;
        }

        public override string ToString() {
            return $"Max: {m_Cards.Length} Size: {Size} First: {First}";
        }

        /// <summary>
        /// 用于可排序集合时的次序：先按 <see cref="UpSize"/> **降序**（明牌多者优先）；
        /// 相同则按 <see cref="m_Index"/> **降序**（下标大者优先）。与具体搜索逻辑解耦时可作稳定 tie-break。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Pile other) {
            int upCompare = other.UpSize.CompareTo(UpSize);

            if (upCompare != 0) {
                return upCompare;
            }

            return other.m_Index.CompareTo(m_Index);
        }
    }
}