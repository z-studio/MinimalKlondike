using System.Runtime.InteropServices;

namespace Klondike.Entities {
    /// <summary>
    /// 一张牌（或空位哨兵）的紧凑表示，固定 8 字节。
    /// 标准牌 <see cref="ID"/> 为 0～51：按花色块连续排列，与 <see cref="Cards"/> 下标一致。
    /// 花色顺序为 <see cref="ECardSuit"/>：Clubs(0)、Diamonds(1)、Spades(2)、Hearts(3)；
    /// 每花色 13 张，点数 <see cref="ECardRank"/>：Ace=0 … King=12。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 8, Pack = 1)]
    public struct Card {
        /// <summary>
        /// 空位/哨兵：ID 超出 0～51，对应 <see cref="Cards"/> 最后一项显示为空白；
        /// IsRed/IsEven/RedEven 取与任何真牌不同的组合，避免与合法牌混淆。
        /// </summary>
        public static readonly Card Empty = new() {
            ID = 52,
            ID2 = 0,
            Suit = ECardSuit.None,
            Rank = ECardRank.None,
            IsEven = 1,
            IsRed = 2,
            RedEven = 2,
            Order = 0
        };

        /// <summary>
        /// 与 <see cref="ID"/> 一一对应：0～51 为 52 张牌（花色顺序 C,D,S,H），52 为占位显示。
        /// </summary>
        private static readonly string[] Cards = [
            "AC", "2C", "3C", "4C", "5C", "6C", "7C", "8C", "9C", "TC", "JC", "QC", "KC",
            "AD", "2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "TD", "JD", "QD", "KD",
            "AS", "2S", "3S", "4S", "5S", "6S", "7S", "8S", "9S", "TS", "JS", "QS", "KS",
            "AH", "2H", "3H", "4H", "5H", "6H", "7H", "8H", "9H", "TH", "JH", "QH", "KH",
            "  "
        ];

        /// <summary>
        /// 0～51：id/13 为花色，id%13 为点数；52 表示空哨兵。
        /// </summary>
        public byte ID;

        /// <summary>
        /// 将 <see cref="Rank"/> 与 <see cref="Suit"/> 打包在一个字节：低 2 位为 Suit（0～3），
        /// Rank（0～12）左移 2 位放在其高位，互不重叠。
        /// </summary>
        public byte ID2;

        public ECardSuit Suit;
        public ECardRank Rank;

        /// <summary>
        /// 是否红色：枚举中 Diamonds(1)、Hearts(3) 为奇数，(Suit &amp; 1)==1 为红，0 为黑。
        /// </summary>
        public byte IsRed;

        /// <summary>
        /// 点数枚举值的奇偶：Ace=0 视为偶、Two=1 奇 … 用于与 <see cref="IsRed"/> 组合成 <see cref="RedEven"/>。
        /// </summary>
        public byte IsEven;

        /// <summary>
        /// <see cref="IsRed"/> 与 <see cref="IsEven"/> 的异或。接龙要求红黑交替时，可叠在一起的两张牌此值必不同（见 Board 校验）。
        /// </summary>
        public byte RedEven;

        /// <summary>
        /// Suit &gt;&gt; 1：梅花/方块为 0，黑桃/红桃为 1。用于状态哈希等按「对」区分花色。
        /// </summary>
        public byte Order;

        /// <summary>
        /// 由标准编号构造一张牌，并填充所有派生字段以便热路径上少计算。
        /// </summary>
        /// <param name="id">0～51 为真牌；调用方勿用 52（请用 <see cref="Empty"/>）。</param>
        public Card(int id) {
            ID = (byte)id;
            
            // 52 张牌：每花色 13 张，先按花色分块再按点数
            Rank = (ECardRank)(id % 13);
            Suit = (ECardSuit)(id / 13);
            
            // Rank 左移 2 位后与 Suit（0～3）按位或，单字节同时携带点数与花色
            ID2 = (byte)(((int)Rank << 2) | (int)Suit);
            
            // 奇数花色（D、H）为红
            IsRed = (byte)((int)Suit & 1);
            
            // Rank 枚举数值的最低位：与「点数奇偶」一致，供接龙交替判断
            IsEven = (byte)((int)Rank & 1);
            
            // 异或后：相邻可接龙的两张牌 RedEven 不同（红黑交替 + 点数步长由别处校验）
            RedEven = (byte)(IsRed ^ IsEven);
            
            // 0,1→0；2,3→1
            Order = (byte)((int)Suit >> 1);
        }

        public override string ToString() {
            return Cards[ID];
        }
    }
}
