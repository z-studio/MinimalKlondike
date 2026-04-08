namespace Klondike.LevelGeneration {
    /// <summary>
    /// 关卡生成入库前的可选筛选；与 YAML <c>filters</c> 及命令行 <c>--filter-*</c> 一一对应。
    /// 未设置（<see cref="IntRangeFilter.Active"/> 为 <c>false</c>）的项不参与过滤。
    /// </summary>
    public sealed class LevelGenerationFilters {
        /// <summary>对应 YAML <c>keyDepthMax</c> / CLI <c>--filter-key-depth</c>；指标见 <see cref="DealStaticMetrics.MaxKeyCardCoverCount"/>。</summary>
        public IntRangeFilter KeyDepthMax;

        /// <summary>对应 <c>firstRevealTotalSteps</c> / <c>--filter-first-reveal</c>；指标见 <see cref="DealReplayMetrics.FirstRevealStepTotal"/>（未翻明过盖牌时为 -1，若筛激活则不合格）。</summary>
        public IntRangeFilter FirstRevealTotalSteps;

        /// <summary>对应 <c>solveMovesMade</c> / <c>--filter-solve-moves</c>；比较的是通关解中 <see cref="Klondike.Entities.Move"/> 的条数（与 <see cref="Klondike.Entities.Board.RecordedMoves"/> 长度一致），非 <c>Board.MovesMade</c> 属性。</summary>
        public IntRangeFilter SolveMovesMade;

        /// <summary>对应 <c>allTableauFaceUpSteps</c> / <c>--filter-all-revealed</c>；指标见 <see cref="DealReplayMetrics.AllTableauFaceUpStepTotal"/>（七列从未全明则为 -1，若筛激活则不合格）。</summary>
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
        /// 静态指标 <paramref name="s"/>、沿通关解重放得到的 <paramref name="r"/>、以及解序列 Move 条数 <paramref name="solutionMoveCount"/> 是否同时满足所有已激活筛选。
        /// </summary>
        public bool Passes(DealStaticMetrics s, DealReplayMetrics r, int solutionMoveCount) {
            // KeyDepthMax ↔ DealStaticMetrics.MaxKeyCardCoverCount
            if (!KeyDepthMax.Matches(s.MaxKeyCardCoverCount)) {
                return false;
            }

            // FirstRevealTotalSteps ↔ DealReplayMetrics.FirstRevealStepTotal（从未翻明则 -1，筛激活时直接淘汰）
            if (FirstRevealTotalSteps.Active && r.FirstRevealStepTotal < 0) {
                return false;
            }

            if (!FirstRevealTotalSteps.Matches(r.FirstRevealStepTotal)) {
                return false;
            }

            // SolveMovesMade ↔ 通关解的 Move 条数（RecordedMoves 长度）
            if (!SolveMovesMade.Matches(solutionMoveCount)) {
                return false;
            }

            // AllTableauFaceUpSteps ↔ DealReplayMetrics.AllTableauFaceUpStepTotal（七列从未全明则 -1）
            if (AllTableauFaceUpSteps.Active && r.AllTableauFaceUpStepTotal < 0) {
                return false;
            }

            if (!AllTableauFaceUpSteps.Matches(r.AllTableauFaceUpStepTotal)) {
                return false;
            }

            // StockAceCount ↔ DealStaticMetrics.StockAceCount
            if (!StockAceCount.Matches(s.StockAceCount)) {
                return false;
            }

            // TableauVisibleAceCount ↔ DealStaticMetrics.TableauVisibleAceCount
            if (!TableauVisibleAceCount.Matches(s.TableauVisibleAceCount)) {
                return false;
            }

            // ImmediatelyMovableFromTableau ↔ DealStaticMetrics.ImmediatelyMovableFromTableauCount
            if (!ImmediatelyMovableFromTableau.Matches(s.ImmediatelyMovableFromTableauCount)) {
                return false;
            }

            // FaceDownTripleSameColorWindowCount ↔ DealStaticMetrics.FaceDownTripleSameColorWindowCount
            if (!FaceDownTripleSameColorWindowCount.Matches(s.FaceDownTripleSameColorWindowCount)) {
                return false;
            }

            // FaceDownQuadrupleSameColorWindowCount ↔ DealStaticMetrics.FaceDownQuadrupleSameColorWindowCount
            if (!FaceDownQuadrupleSameColorWindowCount.Matches(s.FaceDownQuadrupleSameColorWindowCount)) {
                return false;
            }

            return true;
        }
    }
}
