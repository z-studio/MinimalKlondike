using System;
using System.Collections.Generic;
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

        /// <summary>
        /// 当前 <see cref="m_Deck"/>（与 <see cref="GetDeal"/> 导出顺序一致，下标 0～51）中，点数为 A、2、K 的牌所在下标。
        /// 用于关卡生成等旁路文本输出；格式 <c>A:i,j,…|2:…|K:…</c>，无则段内为空（如 <c>A:</c>）。
        /// </summary>
        public string GetKeyRankDeckIndexSummary() {
            var ace = new List<int>(4);
            var two = new List<int>(4);
            var king = new List<int>(4);

            for (var i = 0; i < kDeckSize; i++) {
                ECardRank r = m_Deck[i].Rank;

                if (r == ECardRank.Ace) {
                    ace.Add(i);
                } else if (r == ECardRank.Two) {
                    two.Add(i);
                } else if (r == ECardRank.King) {
                    king.Add(i);
                }
            }

            return $"A:{string.Join(",", ace)}|2:{string.Join(",", two)}|K:{string.Join(",", king)}";
        }

        #region Level Generation

        /// <summary>
        /// 牌桌按序号先后填入到 <see cref="m_Deck"/> 的映射表。
        /// </summary>
        /// <code>
        /// 牌桌区生成牌的顺序如下：
        /// 1, 8, 14, 19, 23, 26, 28,
        ///    2, 9,  15, 20, 24, 27,
        ///       3,  10, 16, 21, 25,
        ///           4,  11, 17, 22,
        ///               5,  12, 18,
        ///                   6,  13,
        ///                       7
        ///
        /// 牌库区生成牌的顺序（从上往下，即从牌堆顶往下生成）如下：
        /// 29～52
        /// </code>
        private static ReadOnlySpan<int> TableFillOrder2DeckIndex => [
            1, 3, 6, 10, 15, 21, 28,
               2, 5, 9,  14, 20, 27,
                  4, 8,  13, 19, 26,
                     7,  12, 18, 25,
                         11, 17, 24,
                             16, 23,
                                 22,
            52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29
        ];

        /// <summary>
        /// 每一步在剩余牌中按权重无放回随机选一张写入对应 <see cref="m_Deck"/> 位置，再 <see cref="SetupInitial"/>。
        /// 权重下标与 <see cref="Card.ID"/>（0～51，C→D→S→H 块、块内 A…K）一致。
        /// </summary>
        /// <param name="seed">随机种子。</param>
        /// <param name="cardWeights">长度须为 52；小于 1 的项按 1 处理。</param>
        /// <returns>传入的 <paramref name="seed"/>，便于链式记录。</returns>
        public int ShuffleCardWeight(int seed, ReadOnlySpan<int> cardWeights) {
            if (cardWeights.Length != kDeckSize) {
                throw new ArgumentException($"cardWeights 须含 {kDeckSize} 个整数。", nameof(cardWeights));
            }

            if (m_Deck.Length != kDeckSize) {
                throw new InvalidOperationException("仅支持 52 张牌的牌局。");
            }

            m_Random = new Random(seed);
            var rng = m_Random;
            var remaining = new List<Card>(kDeckSize);

            for (var id = 0; id < kDeckSize; id++) {
                remaining.Add(new Card(id));
            }

            foreach (var slotOneBased in TableFillOrder2DeckIndex) {
                if (slotOneBased < 1 || slotOneBased > kDeckSize) {
                    throw new InvalidOperationException(
                        $"TableFillOrder2DeckIndex 含非法项 {slotOneBased}，须为 1～{kDeckSize}。");
                }

                int deckIndex = slotOneBased - 1;
                int pick = PickWeightedIndex(rng, remaining, cardWeights);
                m_Deck[deckIndex] = remaining[pick];
                remaining.RemoveAt(pick);
            }

            if (remaining.Count != 0) {
                throw new InvalidOperationException("LevelGeneration 发牌未清空剩余牌。");
            }

            SetupInitial();
            Reset();
            return seed;
        }

        /// <summary>
        /// 等价于全权重 1 的 <see cref="ShuffleCardWeight"/>。
        /// </summary>
        public int ShuffleCardWeight(int seed) {
            Span<int> w = stackalloc int[kDeckSize];
            w.Fill(1);
            return ShuffleCardWeight(seed, w);
        }

        private static int PickWeightedIndex(Random rng, List<Card> remaining, ReadOnlySpan<int> cardWeights) {
            if (remaining.Count == 1) {
                return 0;
            }

            long total = 0;
            var w = new int[remaining.Count];

            for (var i = 0; i < remaining.Count; i++) {
                int cw = cardWeights[remaining[i].ID];

                if (cw < 1) {
                    cw = 1;
                }

                w[i] = cw;
                total += w[i];
            }

            long roll = rng.NextInt64(total);
            long acc = 0;

            for (var i = 0; i < w.Length; i++) {
                acc += w[i];

                if (roll < acc) {
                    return i;
                }
            }

            return w.Length - 1;
        }

        #endregion
    }
}
