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

        /// <summary>输出路径（与 <c>outPath</c> 二选一）。</summary>
        public string Out { get; set; }

        public string OutPath { get; set; }

        public int? DrawCount { get; set; }

        public int? MaxStates { get; set; }

        public int? MaxMoves { get; set; }

        public int? MaxRounds { get; set; }

        /// <summary>第 t 局 <c>ShuffleCardWeight(seed+t)</c> 的基准种子。</summary>
        public int? LevelGenSpecSeed { get; set; }

        /// <summary>
        /// 可选。长度须为 52，与 <c>Card.ID</c> 0～51 一致；省略等价于全权重 1。
        /// </summary>
        public int[] CardWeights { get; set; }

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

            LevelGenerationConfig cfg = deserializer.Deserialize<LevelGenerationConfig>(text);

            if (cfg == null) {
                throw new InvalidOperationException($"无法解析 YAML（空文档或根节点缺失）：{path}");
            }

            return cfg;
        }
    }

    /// <summary>筛选条件；值为 <c>L,R</c> 字符串，语义同命令行（左开右闭，L==R 为精确值）。</summary>
    public sealed class LevelGenerationConfigFilters {
        public string KeyDepthMax { get; set; }

        public string FirstRevealTotalSteps { get; set; }

        public string SolveMovesMade { get; set; }

        public string AllTableauFaceUpSteps { get; set; }

        public string StockAceCount { get; set; }

        public string TableauVisibleAceCount { get; set; }

        public string ImmediatelyMovableFromTableau { get; set; }

        public string FaceDownTripleSameColorWindowCount { get; set; }

        public string FaceDownQuadrupleSameColorWindowCount { get; set; }
    }
}
