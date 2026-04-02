using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Klondike.Entities {
    /// <summary>
    /// 一步走法，固定 2 字节：源/目的牌堆编号、张数、是否翻牌。
    /// Value1：低 4 位 From，高 4 位 To（均为 Board 上牌堆下标 0～15）。
    /// Value2：低 7 位 Count，bit7（最高位） 为 Flip。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 2, Pack = 1)]
    public struct Move {
        /// <summary>
        /// 打包的 From（低 4 位）与 To（高 4 位）。
        /// </summary>
        public byte Value1;

        /// <summary>
        /// 打包的 Count（低 7 位）与 Flip（0x80）。
        /// </summary>
        public byte Value2;

        public Move(byte from, byte to, byte count = 1, bool flip = false) {
            Value1 = (byte)(from | (to << 4));
            Value2 = (byte)(count | (flip ? 0x80 : 0x00));
        }

        /// <summary>
        /// 字母牌堆名：'A'→0，与 <see cref="Display"/> 一致。
        /// </summary>
        public Move(char from, char to, int count = 1, bool flip = false) {
            Value1 = (byte)(((byte)from - (byte)'A') | (((byte)to - (byte)'A') << 4));
            Value2 = (byte)(count | (flip ? 0x80 : 0x00));
        }

        /// <summary>
        /// Value1==0 作哨兵（如 MoveNode 链结尾），非「业务空步」语义。
        /// </summary>
        public bool IsNull {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value1 == 0;
        }

        public byte From {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)(Value1 & 0x0f);
        }

        public byte To {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)(Value1 >> 4);
        }

        public byte Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte)(Value2 & 0x7f);
        }

        public bool Flip {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Value2 & 0x80) != 0;
        }

        public string Display => string.Concat((char)((byte)'A' + From), (char)((byte)'A' + To));

        public override string ToString() {
            return string.Concat(

                // 废牌堆相关走法在显示时带上 |Count| 前缀（与发/翻多张有关）
                From == Board.kWastePile && Count != 0 ? Math.Abs(Count).ToString() : string.Empty,
                Display
            );
        }

        /// <summary>
        /// 仅比较 Value1（From/To）；同起点终点不同 Count/Flip 也会相等。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Move left, Move right) {
            return left.Value1 == right.Value1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Move left, Move right) {
            return left.Value1 != right.Value1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) {
            return obj is Move other && other.Value1 == Value1 && other.Value2 == Value2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() {
            return Value1 | (Value2 << 8);
        }
    }
}