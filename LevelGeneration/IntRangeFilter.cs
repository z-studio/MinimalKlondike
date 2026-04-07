using System;

namespace Klondike.LevelGeneration {
    /// <summary>
    /// 左开右闭区间 (L, R]；若 L==R 则退化为「等于 L」。例：(0,2] 即 &gt;0 且 ≤2；[3,3] 即 =3。
    /// </summary>
    public struct IntRangeFilter {
        public int LeftOpen;
        public int RightInclusive;
        public bool Active;

        public readonly bool Matches(int value) {
            if (!Active) {
                return true;
            }

            if (LeftOpen == RightInclusive) {
                return value == LeftOpen;
            }

            return value > LeftOpen && value <= RightInclusive;
        }

        public static bool TryParse(string text, out IntRangeFilter filter) {
            filter = default;

            if (string.IsNullOrWhiteSpace(text)) {
                return false;
            }

            int comma = text.IndexOf(',');

            if (comma < 0) {
                return false;
            }

            if (!int.TryParse(text.AsSpan(0, comma).Trim(), out int a)) {
                return false;
            }

            if (!int.TryParse(text.AsSpan(comma + 1).Trim(), out int b)) {
                return false;
            }

            filter = new IntRangeFilter { LeftOpen = a, RightInclusive = b, Active = true };
            return true;
        }
    }
}
