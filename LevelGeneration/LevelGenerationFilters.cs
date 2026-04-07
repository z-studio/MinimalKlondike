namespace Klondike.LevelGeneration {
    /// <summary>
    /// 可选筛选条件；未设置（<see cref="IntRangeFilter.Active"/>==false）的项不参与过滤。
    /// </summary>
    public sealed class LevelGenerationFilters {
        public IntRangeFilter KeyDepthMax;
        public IntRangeFilter FirstRevealTotalSteps;
        public IntRangeFilter SolveMovesMade;
        public IntRangeFilter AllTableauFaceUpSteps;
        public IntRangeFilter StockAceCount;
        public IntRangeFilter TableauVisibleAceCount;
        public IntRangeFilter ImmediatelyMovableFromTableau;
        public IntRangeFilter FaceDownTripleSameColorWindowCount;
        public IntRangeFilter FaceDownQuadrupleSameColorWindowCount;

        public bool Passes(DealStaticMetrics s, DealReplayMetrics r, int solveMovesMade) {
            if (!KeyDepthMax.Matches(s.MaxKeyCardCoverCount)) {
                return false;
            }

            if (FirstRevealTotalSteps.Active && r.FirstRevealStepTotal < 0) {
                return false;
            }

            if (!FirstRevealTotalSteps.Matches(r.FirstRevealStepTotal)) {
                return false;
            }

            if (!SolveMovesMade.Matches(solveMovesMade)) {
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
    }
}
