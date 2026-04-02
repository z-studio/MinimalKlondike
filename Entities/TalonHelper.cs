using System;
using System.Runtime.CompilerServices;

namespace Klondike.Entities {
    public sealed class TalonHelper {
        public readonly Card[] StockWaste;
        public readonly int[] CardsDrawn;
        
        private readonly bool[] m_StockUsed;

        public TalonHelper(int talonSize) {
            StockWaste = new Card[talonSize];
            CardsDrawn = new int[talonSize];
            m_StockUsed = new bool[talonSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int Calculate(int drawCount, Pile wastePile, Pile stockPile) {
            var size = 0;
            Array.Fill(m_StockUsed, false);

            //Check waste
            int wasteSize = wastePile.Size;

            if (wasteSize > 0) {
                StockWaste[size] = wastePile.BottomNoCheck;
                CardsDrawn[size++] = 0;
            }

            //Check cards waiting to be turned over from stock
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

            //Check cards already turned over in the waste, meaning we have to "redeal" the deck to get to it
            int amountToDraw = stockSize + 1;
            wasteSize--;

            for (position = drawCount - 1; position < wasteSize; position += drawCount) {
                StockWaste[size] = wastePile[position];
                CardsDrawn[size++] = -amountToDraw - position;
            }

            //Check cards in stock after a "redeal". Only happens when draw count > 1 and you have access to more cards in the talon
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