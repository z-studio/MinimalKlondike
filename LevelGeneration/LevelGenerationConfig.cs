using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Klondike.LevelGeneration {
    /// <summary>
    /// 关卡生成 YAML 配置（UTF-8）。字段均可选；未写则保持默认或由命令行覆盖。
    /// 键名使用 camelCase（与 YamlDotNet 的 <see cref="CamelCaseNamingConvention"/> 一致）。
    /// </summary>
    public sealed class LevelGenerationConfig {
        public int? Attempts { get; set; }

        /// <summary>输出路径。</summary>
        public string OutPath { get; set; }

        public int? DrawCount { get; set; }

        public int? MaxStates { get; set; }
        

        public int? MaxRounds { get; set; }

        /// <summary>第 t 局 <c>ShuffleCardWeight(seed+t)</c> 的基准种子。</summary>
        public int? LevelGenSpecSeed { get; set; }

        /// <summary>
        /// 可选。长度须为 52，与 <c>Card.ID</c> 0～51 一致；省略等价于全权重 1。
        /// </summary>
        public int[] CardWeights { get; set; }

        /// <summary>可选；与 <see cref="LevelGenerationFilters"/> 字段同名（camelCase），值为 <c>L,R</c> 字符串。</summary>
        public LevelGenerationConfigFilters Filters { get; set; }

        /// <summary>从 <c>.yaml</c> / <c>.yml</c> 文件加载。</summary>
        public static LevelGenerationConfig Load(string path) {
            string ext = Path.GetExtension(path).ToLowerInvariant();

            if (ext != ".yaml" && ext != ".yml") {
                throw new ArgumentException("关卡生成配置仅支持 YAML，请使用扩展名 .yaml 或 .yml。", nameof(path));
            }

            string fullPath = Path.GetFullPath(path);
            string text = File.ReadAllText(fullPath);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var cfg = deserializer.Deserialize<LevelGenerationConfig>(text);

            if (cfg == null) {
                throw new InvalidOperationException($"无法解析 YAML（空文档或根节点缺失）：{path}");
            }

            return cfg;
        }
    }

    /// <summary>
    /// YAML 中 <c>filters</c> 下的筛选项；值为 <c>L,R</c> 字符串，语义同 <see cref="IntRangeFilter"/>（左开右闭，<c>L==R</c> 为精确值）。
    /// 省略或空字符串表示不筛该项。键名与 <see cref="LevelGenerationFilters"/> 字段对应。
    /// </summary>
    public sealed class LevelGenerationConfigFilters {
        /// <summary>关键牌深度：盖牌中 A/2/K 上方盖牌张数的全局最大值；同 <c>--filter-key-depth</c>。</summary>
        public string KeyDepthMax { get; set; }

        /// <summary>首次由暗翻明时，牌桌步数与牌库步数之和；同 <c>--filter-first-reveal</c>。</summary>
        public string FirstRevealTotalSteps { get; set; }

        /// <summary>通关解中 Move 的条数（与 RecordedMoves 长度一致）；同 <c>--filter-solve-moves</c>。</summary>
        public string SolveMovesMade { get; set; }

        /// <summary>七列盖牌全部翻开时的累计步数；同 <c>--filter-all-revealed</c>。</summary>
        public string AllTableauFaceUpSteps { get; set; }

        /// <summary>库存 pile 中 A 的张数；同 <c>--filter-stock-aces</c>。</summary>
        public string StockAceCount { get; set; }

        /// <summary>七列明牌区可见 A 的张数；同 <c>--filter-visible-aces</c>。</summary>
        public string TableauVisibleAceCount { get; set; }

        /// <summary>开局可执行的、源摞为桌面列的走法条数；同 <c>--filter-movable-tableau</c>。</summary>
        public string ImmediatelyMovableFromTableau { get; set; }

        /// <summary>盖牌段内连续 3 张同色（全红或全黑）滑动窗个数；同 <c>--filter-facedown-triple-samecolor</c>。</summary>
        public string FaceDownTripleSameColorWindowCount { get; set; }

        /// <summary>盖牌段内连续 4 张同色滑动窗个数；同 <c>--filter-facedown-quadruple-samecolor</c>。</summary>
        public string FaceDownQuadrupleSameColorWindowCount { get; set; }
    }
}
