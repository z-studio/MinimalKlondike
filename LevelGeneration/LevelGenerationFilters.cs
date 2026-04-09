using System;

namespace Klondike.LevelGeneration {
    /// <summary>
    /// 关卡生成入库前的可选筛选；与 YAML <c>filters</c> 及命令行 <c>--filter-*</c> 一一对应。
    /// 未设置（<see cref="IntRangeFilter.Active"/> 为 <c>false</c>）的项不参与过滤。
    /// </summary>
    public sealed class LevelGenerationFilters {
        /// <summary>
        /// 盖牌中 A：仅当 <see cref="KeyAceCoverCount"/> 激活时参与筛选。
        /// 深度区间 <c>(L,R]</c> 判定单张 A 的盖牌深度 <c>first-j-1</c> 是否计入；张数区间约束「计入的张数」。
        /// </summary>
        public IntRangeFilter KeyAceCoverDepth;

        /// <summary>满足 <see cref="KeyAceCoverDepth"/> 的盖牌 A 的张数须落在此区间（未激活则不对 A 做该组筛选）。</summary>
        public IntRangeFilter KeyAceCoverCount;

        public IntRangeFilter KeyTwoCoverDepth;
        public IntRangeFilter KeyTwoCoverCount;

        public IntRangeFilter KeyKingCoverDepth;
        public IntRangeFilter KeyKingCoverCount;

        /// <summary>对应 <c>firstRevealTotalSteps</c> / <c>--filter-first-reveal</c>；指标见 <see cref="DealReplayMetrics.FirstRevealStepTotal"/>（与 <see cref="Klondike.Entities.Board.MovesMade"/> 累计口径一致；未翻明过盖牌时为 -1，若筛激活则不合格）。</summary>
        public IntRangeFilter FirstRevealTotalSteps;

        /// <summary>对应 <c>solveMovesMade</c> / <c>--filter-solve-moves</c>；比较 <see cref="Klondike.Entities.SolveDetail.Moves"/>（与 <see cref="Klondike.Entities.Board.MovesMade"/> 一致，即执行序列折算步数，非 <see cref="Klondike.Entities.Board.RecordedMoves"/> 条数）。</summary>
        public IntRangeFilter SolveMovesMade;

        /// <summary>对应 <c>allTableauFaceUpSteps</c> / <c>--filter-all-revealed</c>；指标见 <see cref="DealReplayMetrics.AllTableauFaceUpStepTotal"/>（<see cref="Klondike.Entities.Board.MovesMade"/> 口径；七列从未全明则为 -1，若筛激活则不合格）。</summary>
        public IntRangeFilter AllTableauFaceUpSteps;

        /// <summary>对应 <c>stockAceCount</c> / <c>--filter-stock-aces</c>；指标见 <see cref="DealStaticMetrics.StockAceCount"/>。</summary>
        public IntRangeFilter StockAceCount;

        /// <summary>对应 <c>tableauVisibleAceCount</c> / <c>--filter-visible-aces</c>；指标见 <see cref="DealStaticMetrics.TableauVisibleAceCount"/>。</summary>
        public IntRangeFilter TableauVisibleAceCount;

        /// <summary>对应 <c>immediatelyMovableFromTableau</c> / <c>--filter-movable-tableau</c>；指标见 <see cref="DealStaticMetrics.ImmediatelyMovableFromTableauCount"/>。</summary>
        public IntRangeFilter ImmediatelyMovableFromTableau;

        /// <summary>对应 <c>faceDownTripleSameColorWindowCount</c> / <c>--filter-facedown-triple-samecolor</c>；指标见 <see cref="DealStaticMetrics.FaceDownTripleSameColorWindowCount"/>。</summary>
        public IntRangeFilter FaceDownTripleSameColorWindowCount;

        /// <summary>对应 <c>faceDownQuadrupleSameColorWindowCount</c> / <c>--filter-facedown-quadruple-samecolor</c>；指标见 <see cref="DealStaticMetrics.FaceDownQuadrupleSameColorWindowCount"/>。</summary>
        public IntRangeFilter FaceDownQuadrupleSameColorWindowCount;

        /// <summary>
        /// 静态指标 <paramref name="s"/>、沿通关解重放得到的 <paramref name="r"/>、以及通关执行序列步数 <paramref name="solveExecutionSteps"/>（<see cref="Klondike.Entities.SolveDetail.Moves"/> / <see cref="Klondike.Entities.Board.MovesMade"/>）是否同时满足所有已激活筛选。
        /// </summary>
        public bool Passes(DealStaticMetrics s, DealReplayMetrics r, int solveExecutionSteps) {
            if (!PassesKeyRankCoverBucket(s.KeyAceCoverDepths, KeyAceCoverDepth, KeyAceCoverCount)) {
                return false;
            }

            if (!PassesKeyRankCoverBucket(s.KeyTwoCoverDepths, KeyTwoCoverDepth, KeyTwoCoverCount)) {
                return false;
            }

            if (!PassesKeyRankCoverBucket(s.KeyKingCoverDepths, KeyKingCoverDepth, KeyKingCoverCount)) {
                return false;
            }

            if (FirstRevealTotalSteps.Active && r.FirstRevealStepTotal < 0) {
                return false;
            }

            if (!FirstRevealTotalSteps.Matches(r.FirstRevealStepTotal)) {
                return false;
            }

            if (!SolveMovesMade.Matches(solveExecutionSteps)) {
                return false;
            }

            if (AllTableauFaceUpSteps.Active && r.AllTableauFaceUpStepTotal < 0) {
                return false;
            }

            if (!AllTableauFaceUpSteps.Matches(r.AllTableauFaceUpStepTotal)) {
                return false;
            }

            if (!StockAceCount.Matches(s.StockAceCount)) {
                return false;
            }

            if (!TableauVisibleAceCount.Matches(s.TableauVisibleAceCount)) {
                return false;
            }

            if (!ImmediatelyMovableFromTableau.Matches(s.ImmediatelyMovableFromTableauCount)) {
                return false;
            }

            if (!FaceDownTripleSameColorWindowCount.Matches(s.FaceDownTripleSameColorWindowCount)) {
                return false;
            }

            if (!FaceDownQuadrupleSameColorWindowCount.Matches(s.FaceDownQuadrupleSameColorWindowCount)) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// <paramref name="countFilter"/> 未激活：本组不筛选。
        /// 已激活：统计 <paramref name="depths"/> 中满足「<paramref name="depthFilter"/> 未激活或深度落入该区间」的个数 <c>N</c>，要求 <c>countFilter.Matches(N)</c>。
        /// </summary>
        static bool PassesKeyRankCoverBucket(
            ReadOnlySpan<int> depths,
            IntRangeFilter depthFilter,
            IntRangeFilter countFilter
        ) {
            if (!countFilter.Active) {
                return true;
            }

            int n = 0;

            for (var i = 0; i < depths.Length; i++) {
                int d = depths[i];

                if (!depthFilter.Active || depthFilter.Matches(d)) {
                    n++;
                }
            }

            return countFilter.Matches(n);
        }
    }
}
