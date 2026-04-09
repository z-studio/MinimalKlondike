namespace Klondike.LevelGeneration {
    /// <summary>
    /// 开局快照（在 <see cref="Klondike.Entities.Board.Solve"/> 之前计算）。
    /// </summary>
    public struct DealStaticMetrics {
        /// <summary>
        /// 七列盖牌中每张 A 的「上方盖牌张数」<c>first - j - 1</c>（按扫描顺序；无则为空数组）。
        /// </summary>
        public int[] KeyAceCoverDepths;

        /// <summary>同上，点数为 2。</summary>
        public int[] KeyTwoCoverDepths;

        /// <summary>同上，点数为 K。</summary>
        public int[] KeyKingCoverDepths;

        /// <summary>
        /// 库存中 A 的张数。
        /// </summary>
        public int StockAceCount;

        /// <summary>
        /// 七列明牌区（含已翻开顶牌）中 A 的张数。
        /// </summary>
        public int TableauVisibleAceCount;

        /// <summary>
        /// 开局时 <see cref="Klondike.Entities.Board.GetAvailableMoves"/> 中，源摞为桌面列的走法条数。
        /// </summary>
        public int ImmediatelyMovableFromTableauCount;

        /// <summary>
        /// 七列盖牌合计：每列盖牌段内，长度为 3 的滑动窗里「连续 3 张同色（全红或全黑）」出现几次（可重叠）
        /// </summary>
        public int FaceDownTripleSameColorWindowCount;

        /// <summary>
        /// 同上，长度为 4 的滑动窗、连续 4 张同色
        /// </summary>
        public int FaceDownQuadrupleSameColorWindowCount;
    }

    /// <summary>
    /// 沿求解序列重放时得到的动态指标（每步为一次 <see cref="Klondike.Entities.Board.MakeMove"/>）。
    /// </summary>
    public struct DealReplayMetrics {
        /// <summary>
        /// 首次出现「桌面盖牌张数减少」（有盖牌由暗变明）时的累计步数（牌桌步数+牌库步数，含本步）。未发生则为 -1。
        /// </summary>
        public int FirstRevealStepTotal;

        /// <summary>
        /// 同上时刻已执行的「非废牌源」步数：凡 <see cref="Klondike.Entities.Move.From"/> 不是 <see cref="Klondike.Entities.Board.kWastePile"/> 的 <see cref="Klondike.Entities.Board.MakeMove"/> 均计入
        /// （含桌面列、回收位为源的走法；开启 <see cref="Klondike.Entities.Board.AllowFoundationToTableau"/> 时的回收位→桌面亦在此列，与 <see cref="DealAnalyzer.ComputeReplay"/> 一致）。
        /// </summary>
        public int FirstRevealTableauSteps;

        /// <summary>
        /// 同上时刻已执行的废牌源步数：仅当 <see cref="Klondike.Entities.Move.From"/>==<see cref="Klondike.Entities.Board.kWastePile"/>（含从废牌打出与库存翻动编码为废牌源时）。
        /// </summary>
        public int FirstRevealTalonSteps;

        /// <summary>
        /// 七列均无盖牌（<see cref="Klondike.Entities.Pile.First"/>==0 或列空）时的累计步数；未发生则为 -1。
        /// </summary>
        public int AllTableauFaceUpStepTotal;
    }
}
