using System;

namespace Klondike.LevelGeneration {
    /// <summary>
    /// 关卡生成筛选用区间：字符串格式 <c>L,R</c>（逗号分隔、可含空格），语义为左开右闭 <c>(L, R]</c>；
    /// 若 <c>L==R</c> 则退化为「等于 <c>L</c>」。例：<c>0,2</c> 表示 <c>value &gt; 0</c> 且 <c>value ≤ 2</c>；<c>3,3</c> 表示 <c>value == 3</c>。
    /// 未解析或未配置时 <see cref="Active"/> 为 <c>false</c>，<see cref="Matches(int)"/> 恒为通过。
    /// </summary>
    public struct IntRangeFilter {
        /// <summary>左端点（开区间一侧）；<c>value</c> 须严格大于此值（<c>L==R</c> 时由 <see cref="Matches(int)"/> 特判为相等）。</summary>
        public int LeftOpen;

        /// <summary>右端点（闭区间一侧）；<c>value</c> 可等于此值。</summary>
        public int RightInclusive;

        /// <summary>为 <c>true</c> 表示已设置筛选，参与 <see cref="Matches(int)"/>；否则视为不筛此项。</summary>
        public bool Active;

        /// <summary>判断 <paramref name="value"/> 是否落在本筛选范围内；未激活时恒返回 <c>true</c>。</summary>
        public readonly bool Matches(int value) {
            if (!Active) {
                return true;
            }

            if (LeftOpen == RightInclusive) {
                return value == LeftOpen;
            }

            return value > LeftOpen && value <= RightInclusive;
        }

        /// <summary>自 <c>L,R</c> 文本解析；失败时 <paramref name="filter"/> 为默认值且返回 <c>false</c>。</summary>
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
