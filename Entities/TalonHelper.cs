using System;
using System.Runtime.CompilerServices;

namespace Klondike.Entities {
    /// <summary>
    /// 帮 <see cref="Board.CheckStockAndWaste"/> 列出：当前局面下，所有「有可能从 talon（库存+废牌）一侧打出去」的牌，
    /// 以及每条对应在 <see cref="Move"/> 里要怎么写「先翻几张 / 要不要先整叠回收」。
    /// </summary>
    public sealed class TalonHelper {
        /// <summary>
        /// 第 j 条方案里，你最终要当「从废牌打出」的那张牌（牌面）。
        /// </summary>
        public readonly Card[] StockWaste;

        /// <summary>
        /// 第 j 条方案里，生成 <see cref="Move"/> 时用的整数；<see cref="Board.CheckStockAndWaste"/> 的规则是：
        /// <list type="number">
        /// <item><description><c>0</c>：废牌顶已经是这张，不用先翻库存。</description></item>
        /// <item><description><c>&gt; 0</c>：<see cref="Move.Flip"/> 为 <c>false</c>，数值写入 <see cref="Move.Count"/>，
        /// 表示在打出前要先从库存向废牌翻过去多少张（具体落子在 <see cref="Board.MakeMove"/>）。</description></item>
        /// <item><description><c>&lt; 0</c>：<see cref="Move.Flip"/> 为 <c>true</c>，要先走「废牌整叠回库存再发」这一类操作，
        /// 绝对值写入 <see cref="Move.Count"/>（与 <see cref="Board.MakeMove"/> 里 <c>move.From == kWastePile &amp;&amp; move.Flip</c> 分支一致）。</description></item>
        /// </list>
        /// </summary>
        public readonly int[] CardsDrawn;

        /// <summary>
        /// 下面第 2 步已经登记过的「库存下标」，第 4 步遇到同一格要停，避免同一张牌列两次。
        /// </summary>
        private readonly bool[] m_StockUsed;

        /// <summary>
        /// 分配缓冲；<paramref name="talonSize"/> 与 Board 里一次最多能列出的 talon 方案数（如 <c>kTalonSize</c>）一致。
        /// </summary>
        public TalonHelper(int talonSize) {
            StockWaste = new Card[talonSize];
            CardsDrawn = new int[talonSize];
            m_StockUsed = new bool[talonSize];
        }

        /// <summary>
        /// 写入 <see cref="StockWaste"/> / <see cref="CardsDrawn"/> 的前缀，返回写了多少条。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int Calculate(int drawCount, Pile wastePile, Pile stockPile) {
            var size = 0;
            Array.Fill(m_StockUsed, false);

            // Check waste
            /*
             * 先统一记一下 Pile 里下标：0 = 这一摞的底，Size-1 = 顶。
             * 库存顶上的牌在 stockPile[stockSize-1]；废牌顶在 wastePile[wasteSize-1]（即 Bottom）。
             * 每次从库存往废牌翻，是一批 drawCount 张（例如 3 张一翻）。
             */

            int wasteSize = wastePile.Size;

            // —— 第 1 步 ——
            // 废牌非空时，顶上的那张现在就能当「从废牌打出」的候选；不需要先翻库存。
            if (wasteSize > 0) {
                StockWaste[size] = wastePile.BottomNoCheck;
                CardsDrawn[size++] = 0;
            }

            // Check cards waiting to be turned over from stock
            // —— 第 2 步 ——
            // 只考虑「一直从库存往废牌翻」，不把废牌整叠收回的情况。
            // 库存里每隔 drawCount 张取一个位置 i（从靠近顶的那一格对齐起，再往底跳）：
            //   这些位置上的牌，就是「再翻若干批之后，会依次轮到成为废牌顶」的牌。
            // CardsDrawn = stockSize - i：与 Board.MakeMove 里「先从库存翻到废牌」的 Count 编码一致。
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
            // —— 第 3 步 ——
            // 废牌里除了当前顶，下面还压着更早翻出来的牌；有的要「先回收整副再发」之后才有机会再露顶。
            // 在废牌里从下标 drawCount-1 开始，每隔 drawCount 取一张（与「按批翻牌」对齐的深度），
            // 这些深度上的牌对应「负的 CardsDrawn」，Board 会把它转成 Flip=true 的 Move。
            // wasteSize--：顶已在第 1 步单独处理，这里只看「顶下面的身体」。
            int amountToDraw = stockSize + 1;
            wasteSize--;

            for (position = drawCount - 1; position < wasteSize; position += drawCount) {
                StockWaste[size] = wastePile[position];
                CardsDrawn[size++] = -amountToDraw - position;
            }

            // Check cards in stock after a "redeal". Only happens when draw count > 1 and you have access to more cards in the talon
            // —— 第 4 步 ——
            // 仅在「每轮翻多张」（drawCount>1）且第 3 步的循环因下标越过废牌长度而结束时才有可能进来：
            // 整副回收再发之后，库存里还会出现新的、与第 2 步不同对齐方式的「可顶牌」候选。
            // 从算出的库存下标 i 往底每隔 drawCount 扫；若 i 已在第 2 步用过（m_StockUsed），说明和第 2 步重复，直接 break。
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