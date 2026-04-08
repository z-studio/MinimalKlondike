using System;

namespace Klondike.LevelGeneration {
    /// <summary>关卡生成一次运行所需的参数（默认值与 YAML/命令行合并结果）。</summary>
    internal sealed class LevelGenerationRunParameters {
        /// <summary>入库前筛选；由 YAML <c>filters</c> 与命令行 <c>--filter-*</c> 合并写入。</summary>
        public LevelGenerationFilters Filters = new();

        public int Attempts = 1000;
        public string OutPath = "qualified_deals.txt";
        public int DrawCount = 1;
        public int MaxStates = 50_000_000;
        public int MaxRounds = 15;
        public int LevelGenSpecSeed;

        /// <summary>
        /// 与 <see cref="Klondike.Entities.Board.ShuffleCardWeight"/> 一致：长度 52，下标即 <c>Card.ID</c>（0～51）。
        /// 为 <c>null</c> 时表示省略，运行时使用均匀权重（全 1）。
        /// </summary>
        public int[] CardWeights;

        public static LevelGenerationRunParameters CreateDefault() => new();

        /// <summary>将 YAML 配置中非空项写入参数（不重置已有字段）。</summary>
        public void ApplyYaml(LevelGenerationConfig cfg) {
            if (cfg == null) {
                return;
            }

            if (cfg.Attempts is { } a) {
                Attempts = System.Math.Max(1, a);
            }

            if (!string.IsNullOrWhiteSpace(cfg.OutPath)) {
                OutPath = cfg.OutPath.Trim();
            }

            if (cfg.DrawCount is { } d) {
                DrawCount = d;
            }

            if (cfg.MaxStates is { } s) {
                MaxStates = s;
            }

            if (cfg.MaxRounds is { } mr) {
                MaxRounds = mr;
            }

            if (cfg.LevelGenSpecSeed is { } seed) {
                LevelGenSpecSeed = seed;
            }

            if (cfg.CardWeights != null) {
                if (cfg.CardWeights.Length != 52) {
                    throw new ArgumentException("YAML 字段 cardWeights 须恰好 52 个整数，与 Card.ID 0～51 一一对应。");
                }

                CardWeights = (int[])cfg.CardWeights.Clone();
            }

            // filters：字符串 "L,R" → IntRangeFilter，写入运行时 Filters（与 CLI --filter-* 共用同一套字段）
            if (cfg.Filters != null) {
                ApplyFilterString(cfg.Filters.KeyDepthMax, ref Filters.KeyDepthMax);
                ApplyFilterString(cfg.Filters.FirstRevealTotalSteps, ref Filters.FirstRevealTotalSteps);
                ApplyFilterString(cfg.Filters.SolveMovesMade, ref Filters.SolveMovesMade);
                ApplyFilterString(cfg.Filters.AllTableauFaceUpSteps, ref Filters.AllTableauFaceUpSteps);
                ApplyFilterString(cfg.Filters.StockAceCount, ref Filters.StockAceCount);
                ApplyFilterString(cfg.Filters.TableauVisibleAceCount, ref Filters.TableauVisibleAceCount);
                ApplyFilterString(cfg.Filters.ImmediatelyMovableFromTableau, ref Filters.ImmediatelyMovableFromTableau);

                ApplyFilterString(
                    cfg.Filters.FaceDownTripleSameColorWindowCount,
                    ref Filters.FaceDownTripleSameColorWindowCount
                );

                ApplyFilterString(
                    cfg.Filters.FaceDownQuadrupleSameColorWindowCount,
                    ref Filters.FaceDownQuadrupleSameColorWindowCount
                );
            }
        }

        /// <summary>将 YAML/文本形式的 <c>L,R</c> 解析为 <see cref="IntRangeFilter"/>；空或无效则保持 <paramref name="slot"/> 不变。</summary>
        static void ApplyFilterString(string text, ref IntRangeFilter slot) {
            if (string.IsNullOrWhiteSpace(text)) {
                return;
            }

            if (IntRangeFilter.TryParse(text.Trim(), out IntRangeFilter f)) {
                slot = f;
            }
        }
    }
}