using System;
using System.Collections.Generic;
using Klondike.Entities;

namespace Klondike.LevelGeneration {
    /// <summary>
    /// 在不改动 <see cref="Board"/> 规则的前提下，从当前局面提取关卡特征（须先处于要分析的局面，必要时先 <see cref="Board.Reset"/>）。
    /// </summary>
    public static class DealAnalyzer {
        /// <summary>
        /// 七列盖牌张数之和（各列 <see cref="Pile.First"/>）。仅当某张盖牌因翻牌或移牌变为明牌时，该和才会下降。
        /// </summary>
        private static int SumTableauFaceDownCount(Board board) {
            var sum = 0;

            for (var i = Board.kTableauStart; i <= Board.kTableauEnd; i++) {
                Pile p = board.GetPile(i);

                if (p.Size <= 0) {
                    continue;
                }

                int f = p.First;
                sum += f < 0 ? 0 : f;
            }

            return sum;
        }

        private static bool AllTableauWithoutFaceDown(Board board) {
            for (var i = Board.kTableauStart; i <= Board.kTableauEnd; i++) {
                Pile p = board.GetPile(i);

                if (p.Size > 0 && p.First > 0) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>与 <see cref="DealReplayMetrics.FirstRevealTalonSteps"/> 一致：仅废牌堆为源计为「牌库/废牌」；回收位→桌面等其余源均计为 <see cref="DealReplayMetrics.FirstRevealTableauSteps"/>。</summary>
        private static bool IsTalonMove(Move move) {
            return move.From == Board.kWastePile;
        }

        /// <summary>
        /// 开局静态指标；调用时 <paramref name="board"/> 须为已发好牌的初始局面。
        /// </summary>
        public static DealStaticMetrics ComputeStatic(Board board) {
            var aceCovers = new List<int>(8);
            var twoCovers = new List<int>(8);
            var kingCovers = new List<int>(8);
            var tripleWindows = 0;
            var quadrupleWindows = 0;

            for (var col = Board.kTableauStart; col <= Board.kTableauEnd; col++) {
                Pile pile = board.GetPile(col);
                int first = pile.First;

                for (var j = 0; j < first; j++) {
                    Card c = pile[j];
                    int coverCount = first - j - 1;

                    if (c.Rank == ECardRank.Ace) {
                        aceCovers.Add(coverCount);
                    } else if (c.Rank == ECardRank.Two) {
                        twoCovers.Add(coverCount);
                    } else if (c.Rank == ECardRank.King) {
                        kingCovers.Add(coverCount);
                    }
                }

                CountFaceDownSameColorWindows(pile, first, ref tripleWindows, ref quadrupleWindows);
            }

            var m = new DealStaticMetrics {
                KeyAceCoverDepths = aceCovers.Count > 0 ? aceCovers.ToArray() : System.Array.Empty<int>(),
                KeyTwoCoverDepths = twoCovers.Count > 0 ? twoCovers.ToArray() : System.Array.Empty<int>(),
                KeyKingCoverDepths = kingCovers.Count > 0 ? kingCovers.ToArray() : System.Array.Empty<int>(),
                FaceDownTripleSameColorWindowCount = tripleWindows,
                FaceDownQuadrupleSameColorWindowCount = quadrupleWindows,
            };

            Pile stock = board.GetPile(Board.kStockPile);

            for (var i = 0; i < stock.Size; i++) {
                if (stock[i].Rank == ECardRank.Ace) {
                    m.StockAceCount++;
                }
            }

            for (var col = Board.kTableauStart; col <= Board.kTableauEnd; col++) {
                Pile pile = board.GetPile(col);

                for (var j = pile.First; j < pile.Size; j++) {
                    if (pile[j].Rank == ECardRank.Ace) {
                        m.TableauVisibleAceCount++;
                    }
                }
            }

            var moves = new List<Move>(128);
            board.GetAvailableMoves(moves, allMoves: true);

            for (var i = 0; i < moves.Count; i++) {
                Move mv = moves[i];

                if (mv.From >= Board.kTableauStart && mv.From <= Board.kTableauEnd) {
                    m.ImmediatelyMovableFromTableauCount++;
                }
            }

            return m;
        }

        private static void CountFaceDownSameColorWindows(
            Pile pile,
            int first,
            ref int tripleWindows,
            ref int quadrupleWindows
        ) {
            if (first < 3) {
                return;
            }

            for (var start = 0; start <= first - 3; start++) {
                byte r0 = pile[start].IsRed;
                byte r1 = pile[start + 1].IsRed;
                byte r2 = pile[start + 2].IsRed;

                if (r0 == r1 && r1 == r2) {
                    tripleWindows++;
                }

                if (start <= first - 4) {
                    byte r3 = pile[start + 3].IsRed;

                    if (r0 == r1 && r1 == r2 && r2 == r3) {
                        quadrupleWindows++;
                    }
                }
            }
        }

        /// <summary>
        /// 从初始局面重放 <paramref name="solution"/>；调用前请 <see cref="Board.Reset"/> 并勿改动 <paramref name="solution"/>。
        /// </summary>
        public static DealReplayMetrics ComputeReplay(Board board, ReadOnlySpan<Move> solution) {
            var r = new DealReplayMetrics {
                FirstRevealStepTotal = -1,
                FirstRevealTableauSteps = -1,
                FirstRevealTalonSteps = -1,
                AllTableauFaceUpStepTotal = -1
            };

            int tableauSteps = 0;
            int talonSteps = 0;
            int prevHidden = SumTableauFaceDownCount(board);

            for (var i = 0; i < solution.Length; i++) {
                Move mv = solution[i];

                if (IsTalonMove(mv)) {
                    talonSteps++;
                } else {
                    tableauSteps++;
                }

                board.MakeMove(mv);
                int nextHidden = SumTableauFaceDownCount(board);

                if (r.FirstRevealStepTotal < 0 && nextHidden < prevHidden) {
                    r.FirstRevealStepTotal = tableauSteps + talonSteps;
                    r.FirstRevealTableauSteps = tableauSteps;
                    r.FirstRevealTalonSteps = talonSteps;
                }

                prevHidden = nextHidden;

                if (r.AllTableauFaceUpStepTotal < 0 && AllTableauWithoutFaceDown(board)) {
                    r.AllTableauFaceUpStepTotal = tableauSteps + talonSteps;
                }
            }

            return r;
        }
    }
}