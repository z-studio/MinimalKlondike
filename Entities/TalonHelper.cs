using System;
using System.Runtime.CompilerServices;

namespace Klondike.Entities {
    /// <summary>
    /// 为「从废牌堆出牌」生成合法走法时，枚举**当前局面下**所有**可能作为出牌对象**的牌，
    /// 以及为够到该牌应在 <see cref="Move"/> 里携带的 <c>Count</c> / <c>Flip</c> 参数（由 <see cref="Board.CheckStockAndWaste"/> 消费）。
    /// </summary>
    /// <remarks>
    /// <para>库存（stock）与废牌（waste）在 <see cref="Pile"/> 中：下标 0 为叠底，<see cref="Pile.Size"/>-1 为叠顶；
    /// 玩家从库存顶按配置的 drawCount 张一批翻到废牌顶。一张牌能否被打出，取决于它是否已成为废牌顶，
    /// 或能否通过若干次「翻库存」或「整叠回收再发」后成为废牌顶。</para>
    /// <para><see cref="CardsDrawn"/> 与 <see cref="Board.MakeMove"/> 的约定：</para>
    /// <list type="bullet">
    /// <item><description><b>≥ 0</b>：不先执行回收（<see cref="Move.Flip"/> 为 false）。数值会写入 <see cref="Move.Count"/>，
    /// 表示从库存翻到废牌所需累计张数（与每轮翻 <c>drawCount</c> 张的步数换算在 <see cref="Board.MovesAdded"/> 等处处理）。</description></item>
    /// <item><description><b>&lt; 0</b>：需要先走「废牌→库存」的回收流程（<see cref="Move.Flip"/> 为 true），
    /// 取绝对值后仍作为 <see cref="Move.Count"/> 参与同样逻辑；具体编码由本类与 <see cref="Board"/> 共同约定。</description></item>
    /// </list>
    /// <para>返回的 <see cref="Calculate"/> 结果为条目数；第 <c>j</c> 条对应 <see cref="StockWaste"/>[j] 与 <see cref="CardsDrawn"/>[j]。</para>
    /// </remarks>
    public sealed class TalonHelper {
        /// <summary>
        /// 与 <see cref="CardsDrawn"/> 同下标：第 j 种 talon 出牌情形所指的牌面。
        /// </summary>
        public readonly Card[] StockWaste;

        /// <summary>
        /// 与 <see cref="StockWaste"/> 同下标：生成对应 <see cref="Move"/> 时使用的 Count 符号与数值（负表示需 Flip）。
        /// </summary>
        public readonly int[] CardsDrawn;

        /// <summary>
        /// 在「从库存直接可翻出」阶段已登记过的库存下标，避免与第四段逻辑重复枚举同一格。
        /// </summary>
        private readonly bool[] m_StockUsed;

        /// <summary>
        /// 为最多 <paramref name="talonSize"/> 种 talon 情形分配并行数组（与 Board 中 <c>kTalonSize</c> 一致）。
        /// </summary>
        public TalonHelper(int talonSize) {
            StockWaste = new Card[talonSize];
            CardsDrawn = new int[talonSize];
            m_StockUsed = new bool[talonSize];
        }

        /// <summary>
        /// 根据当前废牌摞、库存摞以及每次翻牌张数，填满 <see cref="StockWaste"/> / <see cref="CardsDrawn"/> 前缀，并返回条数。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int Calculate(int drawCount, Pile wastePile, Pile stockPile) {
            var size = 0;
            Array.Fill(m_StockUsed, false);

            // Check waste
            // --- 1) 废牌顶：已可见，无需再翻库存；Move.Count 为 0 ---
            int wasteSize = wastePile.Size;

            if (wasteSize > 0) {
                StockWaste[size] = wastePile.BottomNoCheck;
                CardsDrawn[size++] = 0;
            }

            // Check cards waiting to be turned over from stock
            // --- 2) 库存中「仅通过继续从库存顶翻牌（不先回收）」即可依次露在废牌顶的牌 ---
            // 这些下标从「当前库存顶往回数，每隔 drawCount 张对齐一批的底张」起，向叠底每隔 drawCount 取样。
            int stockSize = stockPile.Size;
            int position = stockSize - drawCount;

            if (position < 0) {
                position = stockSize > 0 ? 0 : -1;
            }

            for (int i = position; i >= 0; i -= drawCount) {
                StockWaste[size] = stockPile[i];
                CardsDrawn[size++] = stockSize - i;
                m_StockUsed[i] = true;
            }

            // Check cards already turned over in the waste, meaning we have to "redeal" the deck to get to it
            // --- 3) 废牌堆内部：被后来翻上的牌压在下面的「历史顶牌」；要够到需先回收再发（CardsDrawn 为负）---
            // waste 下标 0 为叠底；每隔 drawCount 取一层，表示与发牌批次对齐的埋深位置。
            int amountToDraw = stockSize + 1;
            wasteSize--;

            for (position = drawCount - 1; position < wasteSize; position += drawCount) {
                StockWaste[size] = wastePile[position];
                CardsDrawn[size++] = -amountToDraw - position;
            }

            // Check cards in stock after a "redeal". Only happens when draw count > 1 and you have access to more cards in the talon
            // --- 4) drawCount > 1 时：整副回收再发后，库存里还会出现新的「首批可见」对齐点；排除已在第 2 步记过的下标 ---
            if (position > wasteSize && wasteSize >= 0) {
                amountToDraw += stockSize + wasteSize;
                position = stockSize - position + wasteSize;

                for (int i = position; i > 0; i -= drawCount) {
                    if (m_StockUsed[i]) {
                        break;
                    }

                    StockWaste[size] = stockPile[i];
                    CardsDrawn[size++] = i - amountToDraw;
                }
            }

            return size;
        }
    }
}