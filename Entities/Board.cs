using Klondike.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Klondike.Entities {
    public unsafe sealed class Board {
        internal const int kDeckSize = 52;
        internal const int kFoundationSize = 4;
        internal const int kTableauSize = 7;
        internal const int kPileSize = kFoundationSize + kTableauSize + 2;
        internal const int kTalonSize = 24;
        internal const int kWastePile = 0;
        internal const int kFoundationStart = kWastePile + 1;
        internal const int kFoundationEnd = kFoundationStart + kFoundationSize - 1;
        internal const int kFoundation1 = kFoundationStart;
        internal const int kFoundation2 = kFoundationStart + 1;
        internal const int kFoundation3 = kFoundationStart + 2;
        internal const int kFoundation4 = kFoundationStart + 3;
        internal const int kTableauStart = kFoundationEnd + 1;
        internal const int kTableauEnd = kTableauStart + kTableauSize - 1;
        internal const int kStockPile = kTableauEnd + 1;

        public bool AllowFoundationToTableau { get; set; }
        private readonly Card[] m_State, m_InitialState, m_Deck;
        private readonly Pile[] m_Piles, m_InitialPiles;
        private readonly Move[] m_MovesMade;
        private Random m_Random;
        private readonly TalonHelper m_Helper;
        private Move m_LastMove;
        private int m_FoundationCount, m_FoundationMinimumBlack, m_FoundationMinimumRed;
        private int m_MovesTotal, m_RoundCount, m_DrawCount;

        public int CardsInFoundation {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_FoundationCount;
        }

        public int TimesThroughDeck {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_RoundCount;
        }

        public int DrawCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_DrawCount;
        }

        public bool Solved {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_FoundationCount == kDeckSize;
        }

        public Board(int drawAmount) {
            m_DrawCount = drawAmount;
            m_Random = new Random();
            m_Helper = new TalonHelper(kTalonSize);

            m_Deck = new Card[kDeckSize];
            m_MovesMade = new Move[512];

            m_Piles = new Pile[kPileSize];
            m_InitialPiles = new Pile[kPileSize];

            int stateIndex =
                kTalonSize * 2 + kFoundationSize * 13 + kTableauSize * 13 + kTableauSize * (kTableauSize - 1) / 2;

            m_State = new Card[stateIndex];
            m_InitialState = new Card[stateIndex];

            stateIndex = 0;
            var pileIndex = 0;
            m_InitialPiles[pileIndex++] = new Pile(m_State, stateIndex);
            stateIndex += kTalonSize;

            for (var i = 0; i < kFoundationSize; i++) {
                m_InitialPiles[pileIndex++] = new Pile(m_State, stateIndex);
                stateIndex += 13;
            }

            for (var i = 0; i < kTableauSize; i++) {
                int size = i + 13;
                m_InitialPiles[pileIndex++] = new Pile(m_State, stateIndex);
                stateIndex += size;
            }

            m_InitialPiles[pileIndex++] = new Pile(m_State, stateIndex);

            Shuffle(0);
        }

        public bool CanAutoPlay() {
            return m_Piles[kStockPile].Size == 0
                   && m_Piles[kWastePile].Size == 0
                   && (m_Piles[kTableauStart].Size == 0 || m_Piles[kTableauStart].First == 0)
                   && (m_Piles[kTableauStart + 1].Size == 0 || m_Piles[kTableauStart + 1].First == 0)
                   && (m_Piles[kTableauStart + 2].Size == 0 || m_Piles[kTableauStart + 2].First == 0)
                   && (m_Piles[kTableauStart + 3].Size == 0 || m_Piles[kTableauStart + 3].First == 0)
                   && (m_Piles[kTableauStart + 4].Size == 0 || m_Piles[kTableauStart + 4].First == 0)
                   && (m_Piles[kTableauStart + 5].Size == 0 || m_Piles[kTableauStart + 5].First == 0)
                   && (m_Piles[kTableauStart + 6].Size == 0 || m_Piles[kTableauStart + 6].First == 0);
        }

        public void PlayMoves(string moves) {
            Reset();

            var draws = 0;
            var moveList = new List<Move>();
            var count = moves.Length;

            for (var i = 0; i < count; i++) {
                char c = moves[i];

                if (c == '@') {
                    draws++;
                    continue;
                } else if (char.IsWhiteSpace(c)) {
                    continue;
                }

                if (i + 1 >= count) {
                    break;
                }

                int stockSize = m_Piles[kStockPile].Size;
                int totalCount = draws * m_DrawCount;

                if (totalCount > stockSize) {
                    draws -= (stockSize + m_DrawCount - 1) / m_DrawCount;
                    totalCount = stockSize;

                    if (draws > 0) {
                        totalCount += draws * m_DrawCount;
                    }
                }

                var newMove = new Move(c, moves[++i], totalCount);
                moveList.Clear();
                GetAvailableMoves(moveList, true);
                var foundMove = false;

                for (var k = 0; k < moveList.Count; k++) {
                    Move move = moveList[k];

                    if (move == newMove && (move.From != kWastePile || move.Count == newMove.Count)) {
                        MakeMove(move);
                        foundMove = true;
                        break;
                    }
                }

                if (!foundMove) {
                    break;
                }

                draws = 0;
            }
        }

        public SolveDetail SolveRandom(int randomGamesToTry = 40000, int maxMoves = 250, int maxRounds = 20) {
            var moves = new List<Move>(64);
            var bestCount = 0;
            var bestMoves = maxMoves + 1;
            var solution = new Move[m_MovesMade.Length];
            var solutionCount = 0;

            var timer = new Stopwatch();
            timer.Start();

            var solves = 0;

            for (var i = 0; i < randomGamesToTry; i++) {
                Reset();

                var movesMadeRnd = 0;

                do {
                    moves.Clear();
                    GetAvailableMoves(moves);

                    if (moves.Count == 0) {
                        break;
                    }

                    Move move = GetRandomMove(moves);
                    movesMadeRnd += MovesAdded(move);
                    MakeMove(move);
                } while (movesMadeRnd < bestMoves && m_RoundCount <= maxRounds);

                if (m_FoundationCount >= bestCount) {
                    if (Solved) {
                        if (movesMadeRnd < bestMoves) {
                            bestMoves = MovesMade;
                            solutionCount = m_MovesTotal;
                            Array.Copy(m_MovesMade, solution, m_MovesTotal);
                        }

                        solves++;
                    } else if (m_FoundationCount > bestCount) {
                        solutionCount = m_MovesTotal;
                        Array.Copy(m_MovesMade, solution, m_MovesTotal);
                    }

                    bestCount = m_FoundationCount;
                }
            }

            timer.Stop();

            Reset();

            for (var i = 0; i < solutionCount; i++) {
                MakeMove(solution[i]);
            }

            return new SolveDetail() {
                Result = bestCount == kDeckSize ? ESolveResult.Solved : ESolveResult.Unknown,
                States = solves,
                Time = timer.Elapsed
            };
        }

        public SolveDetail Solve(
            int maxMoves = 250,
            int maxRounds = 20,
            int maxNodes = 10000000,
            bool terminateEarly = false
        ) {
            var open = new Heap<MoveIndex>((int)(maxNodes));
            var closed = new HashMap<State>(FindPrime((int)(maxNodes * 1.1)));

            var nodeStorage = new MoveNode[maxNodes + 1];
            Array.Fill(nodeStorage, new MoveNode() { Parent = -1 });

            var nodeCount = 1;
            var maxFoundationCount = 0;
            var moves = new List<Move>(64);
            var movesStorage = new Move[m_MovesMade.Length];

            //Initialize previous state if there are moves already made
            {
                Array.Copy(m_MovesMade, movesStorage, m_MovesTotal);
                int movesToMake = m_MovesTotal;

                Reset();
                State state = GameState();
                state.Moves = Estimate;
                closed.Add(state);

                for (int i = 0; i < movesToMake; i++) {
                    Move move = movesStorage[i];
                    MakeMove(move);
                    state = GameState();
                    state.Moves = Estimate;
                    nodeStorage[nodeCount] = new MoveNode() { Move = move, Parent = nodeCount - 1 };
                    nodeCount++;
                    closed.Add(state);
                }
            }

            //Add current state
            open.Enqueue(new MoveIndex() { Index = nodeCount - 1, Estimate = Estimate });

            int bestSolutionMoveCount = maxMoves + 1;
            int solutionIndex = -1;

            var timer = new Stopwatch();
            timer.Start();

            while (open.Count > 0 && nodeCount < maxNodes) {
                //Get next state to evaluate
                MoveIndex node = open.Dequeue();

                Estimate estimate = node.Estimate;

                if (estimate.Total >= bestSolutionMoveCount) {
                    continue;
                }

                //Initialize game to the next state
                int movesToMake = nodeStorage[node.Index].Copy(movesStorage, nodeStorage);
                Reset();

                for (int i = movesToMake - 1; i >= 0; --i) {
                    MakeMove(movesStorage[i]);
                }

                //Get any available moves to check
                moves.Clear();
                GetAvailableMoves(moves);

                //Make available moves and add them to be evaulated
                int canAdd = moves.Count;

                for (int i = 0; i < canAdd; ++i) {
                    Move move = moves[i];
                    int movesAdded = MovesAdded(move);
                    MakeMove(move);

                    //Check estimated move count to be less than current best
                    int newCurrent = estimate.Current + movesAdded;

                    if (newCurrent > 255) {
                        newCurrent = 255;
                    }

                    Estimate newEstimate = new Estimate() {
                        Current = (byte)newCurrent, Remaining = (byte)MinimumMovesRemaining(m_RoundCount == maxRounds)
                    };

                    if (newEstimate.Total < bestSolutionMoveCount && m_RoundCount <= maxRounds) {
                        State key = GameState();
                        key.Moves = newEstimate;

                        //Check state doesn't exist or that it used more moves than current
                        int index = closed.Add(key);

                        if (index < 0 || closed[index].Moves.Total > newEstimate.Total) {
                            if (index >= 0) {
                                closed[index].Moves = newEstimate;
                            }

                            nodeStorage[nodeCount] = new MoveNode() { Move = move, Parent = node.Index };

                            //Check for best solution to foundations
                            if (m_FoundationCount > maxFoundationCount || Solved) {
                                solutionIndex = nodeCount;
                                maxFoundationCount = m_FoundationCount;

                                //Save solution
                                if (Solved) {
                                    bestSolutionMoveCount = newEstimate.Total;
                                    nodeCount++;

                                    if (terminateEarly) {
                                        open.Clear();
                                        break;
                                    }
                                }
                            }

                            if (!Solved) {
                                short heuristic = (short)((newEstimate.Total << 1)
                                                          + movesAdded
                                                          + (kDeckSize - m_FoundationCount + (m_RoundCount << 1)));

                                open.Enqueue(
                                    new MoveIndex()
                                        { Index = nodeCount++, Priority = heuristic, Estimate = newEstimate }
                                );

                                if (nodeCount >= maxNodes) {
                                    break;
                                }
                            }
                        }
                    }

                    UndoMove();
                }
            }

            timer.Stop();

            //Reset state to best found solution
            if (solutionIndex >= 0) {
                int movesToMake = nodeStorage[solutionIndex].Copy(movesStorage, nodeStorage);
                Reset();

                for (int i = movesToMake - 1; i >= 0; --i) {
                    MakeMove(movesStorage[i]);
                }
            }

            ESolveResult result = nodeCount < maxNodes
                ? maxFoundationCount == kDeckSize ? !terminateEarly ? ESolveResult.Minimal : ESolveResult.Solved
                    : ESolveResult.Impossible
                : maxFoundationCount == kDeckSize
                    ? ESolveResult.Solved
                    : ESolveResult.Unknown;

            return new SolveDetail() {
                Result = result,
                States = nodeCount,
                Time = timer.Elapsed,
                Moves = result == ESolveResult.Solved || result == ESolveResult.Minimal ? MovesMade : 0
            };
        }

        public SolveDetail SolveFast(int maxMoves = 250, int maxRounds = 20, int maxNodes = 2000000) {
            var open = new Heap<MoveIndex>(maxNodes);
            var closed = new HashMap<StateFast>(FindPrime(maxNodes));

            var nodeStorage = new MoveNode[maxNodes + 1];
            Array.Fill(nodeStorage, new MoveNode() { Parent = -1 });

            var nodeCount = 1;
            var maxFoundationCount = 0;
            var moves = new List<Move>(64);
            Move[] movesStorage = new Move[m_MovesMade.Length];

            //Initialize previous state if there are moves already made
            {
                Array.Copy(m_MovesMade, movesStorage, m_MovesTotal);
                int movesToMake = m_MovesTotal;

                Reset();
                StateFast state = GameStateFast();
                state.Moves = Estimate;
                closed.Add(state);

                for (var i = 0; i < movesToMake; i++) {
                    Move move = movesStorage[i];
                    MakeMove(move);
                    state = GameStateFast();
                    state.Moves = Estimate;
                    nodeStorage[nodeCount] = new MoveNode() { Move = move, Parent = nodeCount - 1 };
                    nodeCount++;
                    closed.Add(state);
                }
            }

            //Add current state
            open.Enqueue(new MoveIndex() { Index = nodeCount - 1, Estimate = Estimate });

            int bestSolutionMoveCount = maxMoves + 1;
            int solutionIndex = -1;

            Stopwatch timer = new Stopwatch();
            timer.Start();

            while (open.Count > 0 && nodeCount < maxNodes) {
                //Get next state to evaluate
                MoveIndex node = open.Dequeue();

                Estimate estimate = node.Estimate;

                if (estimate.Total >= bestSolutionMoveCount) {
                    continue;
                }

                //Initialize game to the next state
                int movesToMake = nodeStorage[node.Index].Copy(movesStorage, nodeStorage);
                Reset();

                for (int i = movesToMake - 1; i >= 0; --i) {
                    MakeMove(movesStorage[i]);
                }

                //Get any available moves to check
                moves.Clear();
                GetAvailableMoves(moves);

                //Make available moves and add them to be evaulated
                int canAdd = moves.Count;

                for (int i = 0; i < canAdd; ++i) {
                    Move move = moves[i];
                    int movesAdded = MovesAdded(move);
                    MakeMove(move);

                    //Check estimated move count to be less than current best
                    int newCurrent = estimate.Current + movesAdded;

                    if (newCurrent > 255) {
                        newCurrent = 255;
                    }

                    Estimate newEstimate = new Estimate() {
                        Current = (byte)newCurrent, Remaining = (byte)MinimumMovesRemaining(m_RoundCount == maxRounds)
                    };

                    if (newEstimate.Total < bestSolutionMoveCount && m_RoundCount <= maxRounds) {
                        StateFast key = GameStateFast();
                        key.Moves = newEstimate;

                        //Check state doesn't exist or that it used more moves than current
                        int index = closed.Add(key);

                        if (index < 0 || closed[index].Moves.Total > newEstimate.Total) {
                            if (index >= 0) {
                                closed[index].Moves = newEstimate;
                            }

                            nodeStorage[nodeCount] = new MoveNode() { Move = move, Parent = node.Index };

                            //Check for best solution to foundations
                            if (m_FoundationCount > maxFoundationCount || Solved) {
                                solutionIndex = nodeCount;
                                maxFoundationCount = m_FoundationCount;

                                //Save solution
                                if (Solved) {
                                    bestSolutionMoveCount = newEstimate.Total;
                                    nodeCount++;
                                    open.Clear();
                                    break;
                                }
                            }

                            if (!Solved) {
                                short heuristic = (short)((newEstimate.Total << 2)
                                                          + (kDeckSize - m_FoundationCount + (m_RoundCount << 1)));

                                open.Enqueue(
                                    new MoveIndex()
                                        { Index = nodeCount++, Priority = heuristic, Estimate = newEstimate }
                                );

                                if (nodeCount >= maxNodes) {
                                    break;
                                }
                            }
                        }
                    }

                    UndoMove();
                }
            }

            timer.Stop();

            //Reset state to best found solution
            if (solutionIndex >= 0) {
                int movesToMake = nodeStorage[solutionIndex].Copy(movesStorage, nodeStorage);
                Reset();

                for (int i = movesToMake - 1; i >= 0; --i) {
                    MakeMove(movesStorage[i]);
                }
            }

            return new SolveDetail() {
                Result = maxFoundationCount == kDeckSize ? ESolveResult.Solved : ESolveResult.Unknown,
                States = nodeCount,
                Time = timer.Elapsed
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void MakeMove(Move move) {
            m_MovesMade[m_MovesTotal++] = move;
            m_LastMove = move;

            if (move.From == kWastePile && move.Count != 0) {
                if (!move.Flip) {
                    m_Piles[kStockPile].RemoveFlip(ref m_Piles[kWastePile], move.Count);
                } else {
                    ++m_RoundCount;
                    int stockSize = m_Piles[kStockPile].Size + m_Piles[kWastePile].Size - move.Count;

                    if (stockSize >= 1) {
                        m_Piles[kWastePile].RemoveFlip(ref m_Piles[kStockPile], stockSize);
                    } else {
                        m_Piles[kStockPile].RemoveFlip(ref m_Piles[kWastePile], -stockSize);
                    }
                }
            }

            if (move.From == kWastePile || move.Count == 1) {
                m_Piles[move.From].Remove(ref m_Piles[move.To]);

                if (move.To <= kFoundationEnd) {
                    ++m_FoundationCount;
                } else if (move.From >= kFoundationStart && move.From <= kFoundationEnd) {
                    --m_FoundationCount;
                }
            } else {
                m_Piles[move.From].Remove(ref m_Piles[move.To], move.Count);
            }

            if (move.Flip) {
                m_Piles[move.From].Flip();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void UndoMove() {
            Move move = m_MovesMade[--m_MovesTotal];
            m_LastMove = m_MovesTotal > 0 ? m_MovesMade[m_MovesTotal - 1] : default;

            if (move.From == kWastePile || move.Count == 1) {
                m_Piles[move.To].Remove(ref m_Piles[move.From]);

                if (move.To <= kFoundationEnd) {
                    --m_FoundationCount;
                } else if (move.From >= kFoundationStart && move.From <= kFoundationEnd) {
                    ++m_FoundationCount;
                }
            } else {
                m_Piles[move.To].Remove(ref m_Piles[move.From], move.Count);
            }

            if (move.Flip) {
                m_Piles[move.From].Flip(move.Count);
            }

            if (move.From == kWastePile && move.Count != 0) {
                if (!move.Flip) {
                    m_Piles[kWastePile].RemoveFlip(ref m_Piles[kStockPile], move.Count);
                } else {
                    --m_RoundCount;
                    int wasteSize = m_Piles[kWastePile].Size + m_Piles[kStockPile].Size - move.Count;

                    if (wasteSize >= 1) {
                        m_Piles[kStockPile].RemoveFlip(ref m_Piles[kWastePile], wasteSize);
                    } else {
                        m_Piles[kWastePile].RemoveFlip(ref m_Piles[kStockPile], -wasteSize);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void GetAvailableMoves(List<Move> moves, bool allMoves = false) {
            SetFoundationMin();

            //Check if last move was to uncover card that could move to foundation
            if (!allMoves && m_LastMove.From >= kTableauStart && m_LastMove.To >= kTableauStart && !m_LastMove.Flip) {
                Pile pileFrom = m_Piles[m_LastMove.From];
                int pileFromSize = pileFrom.Size;

                if (pileFromSize > 0) {
                    Card card = pileFrom.BottomNoCheck;
                    int foundationMinimum = 0;
                    byte cardFoundation = CanMoveToFoundation(card, ref foundationMinimum);

                    if (cardFoundation != 255) {
                        moves.Add(new Move(m_LastMove.From, cardFoundation, 1, pileFromSize > 1 && pileFrom.UpSize == 1));
                        return;
                    }
                }
            }

            if (CheckTableau(moves, allMoves)) {
                return;
            }

            if (CheckStockAndWaste(moves, allMoves)) {
                return;
            }

            if (AllowFoundationToTableau) {
                CheckFoundation(moves);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void SetFoundationMin() {
            int min1 = m_Piles[kFoundation1].Size;
            int min2 = m_Piles[kFoundation3].Size;
            m_FoundationMinimumBlack = (min1 <= min2 ? min1 : min2) + 1;
            min1 = m_Piles[kFoundation2].Size;
            min2 = m_Piles[kFoundation4].Size;
            m_FoundationMinimumRed = (min1 <= min2 ? min1 : min2) + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private bool CheckTableau(List<Move> moves, bool allMoves = false) {
            var emptyPiles = 0;

            for (byte i = kTableauStart; i <= kTableauEnd; ++i) {
                Pile pileFrom = m_Piles[i];

                int pileFromSize = pileFrom.Size;

                if (pileFromSize == 0) {
                    emptyPiles++;
                }
            }

            //Check tableau to foundation, Check tableau to tableau
            for (byte i = kTableauStart; i <= kTableauEnd; ++i) {
                Pile pileFrom = m_Piles[i];

                int pileFromSize = pileFrom.Size;

                if (pileFromSize == 0) {
                    continue;
                }

                Card fromBottom = pileFrom.BottomNoCheck;
                int foundationMinimum = 0;
                byte cardFoundation = CanMoveToFoundation(fromBottom, ref foundationMinimum);

                if (cardFoundation != 255) {
                    Move temp = new Move(i, cardFoundation, 1, pileFromSize > 1 && pileFrom.UpSize == 1);

                    //is this an auto move?
                    if (!allMoves && (int)fromBottom.Rank <= foundationMinimum) {
                        moves.Clear();
                        moves.Add(temp);
                        return true;
                    } else {
                        moves.Add(temp);
                    }
                }

                Card fromTop = pileFrom.TopNoCheck;
                int pileFromLength = fromTop.Rank - fromBottom.Rank + 1;
                bool kingMoved = fromTop.Rank != ECardRank.King;

                for (byte j = kTableauStart; j <= kTableauEnd; ++j) {
                    if (i == j) {
                        continue;
                    }

                    Pile pileTo = m_Piles[j];

                    if (pileTo.Size == 0) {
                        if (!kingMoved && pileFromSize != pileFromLength) {
                            moves.Add(new Move(i, j, (byte)pileFromLength, true));

                            //only create one move for a blank spot
                            kingMoved = !allMoves;
                        }

                        continue;
                    }

                    Card toBottom = pileTo.BottomNoCheck;

                    if ((int)toBottom.Rank - (int)fromTop.Rank > 1
                        || fromBottom.RedEven != toBottom.RedEven
                        || fromBottom.Rank >= toBottom.Rank) {
                        continue;
                    }

                    int pileFromMoved = toBottom.Rank - fromBottom.Rank;

                    if (allMoves
                        || (pileFromMoved == pileFromLength && (pileFromMoved != pileFromSize || emptyPiles == 0))
                        || (pileFromMoved < pileFromLength
                            && CanMoveToFoundation(pileFrom.UpNoCheck(pileFromMoved), ref foundationMinimum) != 255)) {
                        //we are moving all face up cards
                        //or look to see if we are covering a card that can be moved to the foundation
                        moves.Add(
                            new Move(
                                i,
                                j,
                                (byte)pileFromMoved,
                                pileFromSize > pileFromMoved && pileFromMoved == pileFromLength
                            )
                        );
                    }
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private bool CheckStockAndWaste(List<Move> moves, bool allMoves = false) {
            int talonCount = m_Helper.Calculate(m_DrawCount, m_Piles[kWastePile], m_Piles[kStockPile]);

            //Check talon cards
            for (byte j = 0; j < talonCount; ++j) {
                Card talonCard = m_Helper.StockWaste[j];
                int cardsToDraw = m_Helper.CardsDrawn[j];
                int foundationMinimum = 0;
                byte cardFoundation = CanMoveToFoundation(talonCard, ref foundationMinimum);
                bool flip = cardsToDraw < 0;

                if (flip) {
                    cardsToDraw = -cardsToDraw;
                }

                if (cardFoundation != 255) {
                    moves.Add(new Move(kWastePile, cardFoundation, (byte)cardsToDraw, flip));

                    if ((int)talonCard.Rank <= foundationMinimum) {
                        if (m_DrawCount > 1 || allMoves) {
                            continue;
                        }

                        if (cardsToDraw == 0 || moves.Count == 1) {
                            return true;
                        }

                        break;
                    }
                }

                for (byte i = kTableauStart; i <= kTableauEnd; ++i) {
                    Card tableauCard = m_Piles[i].Bottom;

                    if (tableauCard.Rank - talonCard.Rank == 1 && talonCard.IsRed != tableauCard.IsRed) {
                        moves.Add(new Move(kWastePile, i, (byte)cardsToDraw, flip));

                        if (talonCard.Rank == ECardRank.King && !allMoves) {
                            break;
                        }
                    }
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void CheckFoundation(List<Move> moves) {
            //Check foundation to tableau, very rarely needed to solve optimally
            for (byte i = kFoundationStart; i <= kFoundationEnd; ++i) {
                Pile foundPile = m_Piles[i];
                int foundationSize = foundPile.Size;

                int foundationMinimum = m_FoundationMinimumBlack < m_FoundationMinimumRed ? m_FoundationMinimumBlack
                    : m_FoundationMinimumRed;

                if (foundationSize <= foundationMinimum) {
                    continue;
                }

                Card foundCard = foundPile.BottomNoCheck;

                for (byte j = kTableauStart; j <= kTableauEnd; ++j) {
                    Card cardTop = m_Piles[j].Bottom;

                    if (cardTop.Rank - foundCard.Rank == 1 && foundCard.IsRed != cardTop.IsRed) {
                        moves.Add(new Move(i, j));

                        if (foundCard.Rank == ECardRank.King) {
                            break;
                        }
                    }
                }
            }
        }

        public Estimate Estimate {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new() { Current = (byte)MovesMade, Remaining = (byte)MinimumMovesRemaining() };
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private int MinimumMovesRemaining(bool lastRound = false) {
            Pile wastePile = m_Piles[kWastePile];
            int wasteSize = wastePile.Size;
            int moves = m_Piles[kStockPile].Size;
            moves += (moves + m_DrawCount - 1) / m_DrawCount + wasteSize;
            Span<byte> mins = stackalloc byte[4] { byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue };

            if (m_DrawCount == 1 || lastRound) {
                for (byte i = 0; i < wasteSize; ++i) {
                    Card card = wastePile[i];

                    if ((byte)card.Rank < mins[(byte)card.Suit]) {
                        mins[(byte)card.Suit] = (byte)card.Rank;
                    } else {
                        moves++;
                    }
                }
            }

            for (byte i = kTableauStart; i <= kTableauEnd; ++i) {
                mins.Fill(byte.MaxValue);
                Pile pile = m_Piles[i];
                moves += pile.Size;

                for (byte j = 0; j < pile.Size; ++j) {
                    Card card = pile[j];

                    if ((byte)card.Rank < mins[(byte)card.Suit]) {
                        if (j < pile.First) {
                            mins[(byte)card.Suit] = (byte)card.Rank;
                        }
                    } else {
                        moves++;

                        if (j >= pile.First) {
                            break;
                        }
                    }
                }
            }

            return moves;
        }

        public StateFast GameStateFast() {
            var key = new StateFast();
            var z = 0;
            Move lastLastMove = m_MovesTotal > 2 ? m_MovesMade[m_MovesTotal - 2] : default;
            key[z++] = (byte)((m_Piles[kFoundation1].Size << 4) | m_Piles[kFoundation3].Size);
            key[z++] = (byte)((m_Piles[kFoundation2].Size << 4) | m_Piles[kFoundation4].Size);
            key[z++] = (byte)lastLastMove.Value1;
            key[z++] = (byte)m_LastMove.Value1;
            key[z++] = (byte)m_Piles[m_LastMove.From].Bottom.ID;
            key[z++] = (byte)m_Piles[kStockPile].Size;
            key[z++] = (byte)m_Piles[kWastePile].Size;

            return key;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private State GameState() {
            Span<byte> order = stackalloc byte[] {
                kTableauStart, 
                kTableauStart + 1, 
                kTableauStart + 2, 
                kTableauStart + 3, 
                kTableauStart + 4, 
                kTableauStart + 5,
                kTableauStart + 6
            };

            //sort the piles
            for (byte current = 1; current < kTableauSize; ++current) {
                byte search = current;

                do {
                    Pile one = m_Piles[order[search - 1]];
                    Pile two = m_Piles[order[search]];

                    if (one.Top.ID2 > two.Top.ID2) {
                        break;
                    }

                    byte temp = order[--search];
                    order[search] = order[search + 1];
                    order[search + 1] = temp;
                } while (search > 0);
            }

            var key = new State();
            var z = 0;
            key[z++] = (byte)((m_Piles[kFoundation1].Size << 4) | m_Piles[kFoundation3].Size);
            key[z++] = (byte)((m_Piles[kFoundation2].Size << 4) | m_Piles[kFoundation4].Size);

            var bits = 5;
            int mask = (byte)m_Piles[kWastePile].Size;

            for (byte i = 0; i < kTableauSize; ++i) {
                Pile pile = m_Piles[order[i]];
                int upSize = pile.UpSize;

                int added = 10;
                mask <<= 6;

                if (upSize > 0) {
                    mask |= pile.TopNoCheck.ID;
                    added += upSize - 1;
                }

                bits += added;
                mask <<= 4;
                mask |= upSize--;

                for (int j = 0; j < upSize; ++j) {
                    mask <<= 1;
                    mask |= pile.UpNoCheck(j).Order;
                }

                do {
                    bits -= 8;
                    key[z++] = (byte)(mask >> bits);
                } while (bits >= 8);
            }

            if (bits > 0) {
                key[z] = (byte)(mask << (8 - bits));
            }

            return key;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private int MovesAdded(Move move) {
            var movesAdded = 1;

            if (move.From == kWastePile && move.Count != 0) {
                int stockSize = m_Piles[kStockPile].Size;

                if (!move.Flip) {
                    movesAdded += (move.Count + m_DrawCount - 1) / m_DrawCount;
                } else {
                    movesAdded += (stockSize + m_DrawCount - 1) / m_DrawCount;
                    movesAdded += (move.Count - stockSize + m_DrawCount - 1) / m_DrawCount;
                }
            }

            return movesAdded;
        }

        public Move GetRandomMove(List<Move> moves) {
            var drawHit = 0;

            do {
                int index = m_Random.Next() % moves.Count;
                Move move = moves[index];

                if (move.From == kWastePile && move.Count != 0) {
                    drawHit++;

                    if (drawHit >= (move.Count + m_DrawCount - 1) / m_DrawCount) {
                        return move;
                    }
                } else if (move.From >= kFoundationStart && move.From <= kFoundationEnd) {
                    drawHit++;

                    if (drawHit > 1) {
                        return move;
                    }
                } else {
                    return move;
                }
            } while (true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte CanMoveToFoundation(Card card, ref int foundationMinimum) {
            int pile = kFoundationStart + (int)card.Suit;

            foundationMinimum = m_FoundationMinimumBlack < m_FoundationMinimumRed ? m_FoundationMinimumBlack
                : m_FoundationMinimumRed;

            return m_Piles[pile].Size == (int)card.Rank ? (byte)pile : (byte)255;
        }

        public bool SetDeal(string cardSet) {
            if (cardSet.Length < m_Deck.Length * 3 - 1) {
                return false;
            }

            int decks = m_Deck.Length / 52;
            var used = new int[52];

            if (cardSet[2] == ' ') {
                for (var i = 0; i < m_Deck.Length; i++) {
                    var suit = char.ToUpper(cardSet[i * 3 + 1]);

                    switch (suit) {
                        case 'C':
                            suit = (char)ECardSuit.Clubs;
                            break;
                        case 'D':
                            suit = (char)ECardSuit.Diamonds;
                            break;
                        case 'S':
                            suit = (char)ECardSuit.Spades;
                            break;
                        case 'H':
                            suit = (char)ECardSuit.Hearts;
                            break;
                        default:
                            return false;
                    }

                    var rank = char.ToUpper(cardSet[i * 3]);

                    switch (rank) {
                        case 'A':
                            rank = (char)ECardRank.Ace;
                            break;
                        case '2':
                            rank = (char)ECardRank.Two;
                            break;
                        case '3':
                            rank = (char)ECardRank.Three;
                            break;
                        case '4':
                            rank = (char)ECardRank.Four;
                            break;
                        case '5':
                            rank = (char)ECardRank.Five;
                            break;
                        case '6':
                            rank = (char)ECardRank.Six;
                            break;
                        case '7':
                            rank = (char)ECardRank.Seven;
                            break;
                        case '8':
                            rank = (char)ECardRank.Eight;
                            break;
                        case '9':
                            rank = (char)ECardRank.Nine;
                            break;
                        case 'T':
                            rank = (char)ECardRank.Ten;
                            break;
                        case 'J':
                            rank = (char)ECardRank.Jack;
                            break;
                        case 'Q':
                            rank = (char)ECardRank.Queen;
                            break;
                        case 'K':
                            rank = (char)ECardRank.King;
                            break;
                        default:
                            return false;
                    }

                    int id = suit * 13 + rank;

                    if (id < 0 || id >= 52) {
                        return false;
                    }

                    used[id]++;
                    m_Deck[i] = new Card(id);
                }
            } else {
                var index = 0;

                for (int k = 1, m = 0; k <= kTableauSize; k++) {
                    for (int i = k, j = m; i <= kTableauSize; i++) {
                        int id = GetCard(cardSet, index++ * 3);

                        if (id < 0 || id >= 52) {
                            return false;
                        }

                        used[id]++;
                        m_Deck[j] = new Card(id);
                        j += i;
                    }

                    m += k + 1;
                }

                int end = m_Deck.Length - kTalonSize;

                for (int i = m_Deck.Length - 1; i >= end; i--) {
                    int id = GetCard(cardSet, index++ * 3);

                    if (id < 0 || id >= 52) {
                        return false;
                    }

                    used[id]++;
                    m_Deck[i] = new Card(id);
                }
            }

            for (var i = 0; i < 52; i++) {
                if (used[i] != decks) {
                    return false;
                }
            }

            SetupInitial();
            Reset();
            return true;
        }

        private int GetCard(string cardSet, int index) {
            int suit = (cardSet[index + 2] ^ 0x30) - 1;

            if (suit >= 2) {
                suit = (suit == 2) ? 3 : 2;
            }

            int rank = (cardSet[index] ^ 0x30) * 10 + (cardSet[index + 1] ^ 0x30);
            return suit * 13 + rank - 1;
        }

        public string GetDeal(bool numbers = true) {
            var cardSet = new StringBuilder(m_Deck.Length * 3);

            if (!numbers) {
                for (var i = 0; i < m_Deck.Length; i++) {
                    cardSet.Append($"{m_Deck[i]} ");
                }
            } else {
                for (int k = 1, m = 0; k <= kTableauSize; k++) {
                    for (int i = k, j = m; i <= kTableauSize; i++) {
                        AppendCard(cardSet, m_Deck[j]);
                        j += i;
                    }

                    m += k + 1;
                }

                int end = m_Deck.Length - kTalonSize;

                for (int i = m_Deck.Length - 1; i >= end; i--) {
                    AppendCard(cardSet, m_Deck[i]);
                }
            }

            return cardSet.ToString();
        }

        private void AppendCard(StringBuilder cardSet, Card card) {
            var suit = (int)card.Suit;

            if (suit >= 2) {
                suit = (suit == 2) ? 3 : 2;
            }

            suit++;

            cardSet.Append($"{(int)card.Rank + 1:00}{suit}");
        }

        private struct GreenRandom {
            public uint Seed;

            public uint Next() {
                Seed = (uint)(((ulong)Seed * 16807) % 0x7fffffff);
                return Seed;
            }
        }

        public void ShuffleGreenFelt(uint seed) {
            var rnd = new GreenRandom { Seed = seed };

            for (var i = 0; i < 26; i++) {
                m_Deck[i] = new Card(i);
            }

            for (var i = 0; i < 13; i++) {
                m_Deck[i + 26] = new Card(i + 39);
            }

            for (var i = 0; i < 13; i++) {
                m_Deck[i + 39] = new Card(i + 26);
            }

            for (var i = 0; i < 7; i++) {
                for (var j = 0; j < 52; j++) {
                    int k = (int)(rnd.Next() % 52);
                    (m_Deck[j], m_Deck[k]) = (m_Deck[k], m_Deck[j]);
                }
            }

            var tmp = new Card[52];
            Array.Copy(m_Deck, 0, tmp, 28, 24);
            Array.Copy(m_Deck, 24, tmp, 0, 28);
            Array.Copy(tmp, m_Deck, 52);

            var orig = 27;

            for (var i = 0; i < 7; i++) {
                int pos = (i + 1) * (i + 2) / 2 - 1;

                for (var j = 6 - i; j >= 0; j--) {
                    if (j >= i) {
                        (m_Deck[pos], m_Deck[orig]) = (m_Deck[orig], m_Deck[pos]);
                    }

                    orig--;
                    pos += (6 - j + 1);
                }
            }

            SetupInitial();
            Reset();
        }

        public int Shuffle(int dealNumber = -1) {
            if (dealNumber != -1) {
                m_Random = new Random(dealNumber);
            } else {
                dealNumber = m_Random.Next();
                m_Random = new Random(dealNumber);
            }

            for (var i = 0; i < m_Deck.Length; i++) {
                m_Deck[i] = new Card(i % 52);
            }

            int cardLength = m_Deck.Length;

            for (var i = 0; i < 7; i++) {
                for (int x = cardLength - 1; x >= 0; x--) {
                    int k = m_Random.Next() % cardLength;
                    (m_Deck[x], m_Deck[k]) = (m_Deck[k], m_Deck[x]);
                }
            }

            SetupInitial();
            Reset();
            return dealNumber;
        }

        private void SetupInitial() {
            Array.Fill(m_State, Card.EMTPY);
            m_InitialPiles[kWastePile].Reset();

            for (int i = kFoundationStart; i <= kFoundationEnd; i++) {
                m_InitialPiles[i].Reset();
            }

            var m = 0;

            for (int i = kTableauStart, j = 1; i <= kTableauEnd; i++, j++) {
                m_InitialPiles[i].Reset();

                for (var k = 0; k < j; k++) {
                    m_InitialPiles[i].Add(m_Deck[m++]);
                }

                m_InitialPiles[i].Flip();
            }

            m_InitialPiles[kStockPile].Reset();

            while (m < m_Deck.Length) {
                m_InitialPiles[kStockPile].Add(m_Deck[m++]);
            }

            Array.Copy(m_State, m_InitialState, m_State.Length);
        }

        public void Reset() {
            m_FoundationCount = 0;
            m_FoundationMinimumBlack = 0;
            m_FoundationMinimumRed = 0;
            m_MovesTotal = 0;
            m_RoundCount = 1;
            m_LastMove = default;

            Array.Copy(m_InitialState, m_State, m_State.Length);
            Array.Copy(m_InitialPiles, m_Piles, m_Piles.Length);
        }

        public bool VerifyGameState() {
            int count = m_Deck.Length;

            for (var i = 0; i < m_Piles.Length; i++) {
                int size = m_Piles[i].Size;
                count -= size;

                if (size < 0) {
                    return false;
                }
            }

            if (count != 0) {
                return false;
            }

            for (int i = kTableauStart; i <= kTableauEnd; i++) {
                Pile pile = m_Piles[i];

                int upSize = pile.UpSize;

                if (upSize < 0) {
                    return false;
                }

                if (upSize > 1) {
                    int suit = pile.BottomNoCheck.IsRed;

                    for (var j = 1; j < upSize; j++) {
                        int temp = pile.Up(j).IsRed;

                        if (suit == temp) {
                            return false;
                        }

                        suit = temp;
                    }
                }
            }

            return true;
        }

        public int MovesMade {
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            get {
                int stockSize = kTalonSize;
                var wasteSize = 0;
                var moves = 0;

                for (var i = 0; i < m_MovesTotal; i++) {
                    Move move = m_MovesMade[i];

                    if (move.From == kWastePile) {
                        if (!move.Flip) {
                            moves += (move.Count + m_DrawCount - 1) / m_DrawCount;
                            stockSize -= move.Count;
                            wasteSize += move.Count;
                        } else {
                            moves += (stockSize + m_DrawCount - 1) / m_DrawCount;
                            moves += (move.Count - stockSize + m_DrawCount - 1) / m_DrawCount;

                            int times = stockSize + wasteSize - move.Count;
                            wasteSize -= times;
                            stockSize += times;
                        }

                        wasteSize--;
                    }

                    moves++;
                }

                return moves;
            }
        }

        public string MovesMadeOutput {
            get {
                var sb = new StringBuilder();
                int stockSize = kTalonSize;
                var wasteSize = 0;

                for (var i = 0; i < m_MovesTotal; i++) {
                    Move move = m_MovesMade[i];

                    if (move.From == kWastePile) {
                        if (!move.Flip) {
                            sb.Append('@', (move.Count + m_DrawCount - 1) / m_DrawCount);
                            stockSize -= move.Count;
                            wasteSize += move.Count;
                        } else {
                            int times = (stockSize + m_DrawCount - 1) / m_DrawCount;
                            sb.Append('@', times);
                            times = (move.Count - stockSize + m_DrawCount - 1) / m_DrawCount;
                            sb.Append('@', times);
                            times = stockSize + wasteSize - move.Count;
                            wasteSize -= times;
                            stockSize += times;
                        }

                        wasteSize--;
                    }

                    sb.Append($"{move.Display} ");
                }

                return sb.ToString();
            }
        }

        public override string ToString() {
            var sb = new StringBuilder();
            var column = (byte)'A';
            sb.Append($"  {(char)column++}");

            for (var i = 0; i < kTableauSize - kFoundationSize - 1; i++) {
                sb.Append("   ");
            }

            for (var i = 0; i < kFoundationSize; i++) {
                sb.Append($"  {(char)column++}");
            }

            sb.AppendLine();
            sb.Append($" {m_Piles[kWastePile].Bottom}");

            for (var i = 0; i < kTableauSize - kFoundationSize - 1; i++) {
                sb.Append("   ");
            }

            for (int i = kFoundationStart; i <= kFoundationEnd; i++) {
                sb.Append($" {m_Piles[i].Bottom}");
            }

            sb.AppendLine();

            for (int i = 0; i < kTableauSize; i++) {
                sb.Append($"  {(char)column++}");
            }

            sb.AppendLine();

            int maxHeight = kTableauSize + 12;

            for (var j = 0; j < maxHeight; j++) {
                bool added = false;

                for (int i = kTableauStart; i <= kTableauEnd; i++) {
                    Pile pile = m_Piles[i];

                    if (pile.Size > j) {
                        added = true;

                        if (j < pile.First) {
                            sb.Append($" {pile[j]}");
                        } else {
                            sb.Append($"+{pile[j]}");
                        }
                    } else {
                        sb.Append("   ");
                    }
                }

                sb.AppendLine();

                if (!added) {
                    break;
                }
            }

            Pile stock = m_Piles[kStockPile];
            int stockSize = stock.Size;
            var count = 0;

            for (int i = stockSize - 1; i >= 0; i--) {
                sb.Append($" {stock[i]}");

                if (++count > kTableauSize - 1) {
                    sb.AppendLine();
                    count = 0;
                }
            }

            if (count != 0) {
                sb.AppendLine();
            }

            count = 0;
            Pile waste = m_Piles[kWastePile];
            stockSize = waste.Size - 1;

            for (int i = stockSize - 1; i >= 0; i--) {
                sb.Append('+').Append(waste[i].ToString());

                if (++count > kTableauSize - 1) {
                    sb.AppendLine();
                    count = 0;
                }
            }

            if (count != 0) {
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private int FindPrime(int input) {
            int maxValue = input << 1;

            for (int i = input + (input & 1) - 1; i < maxValue; i += 2) {
                if (Miller(i, 20)) {
                    return i;
                }
            }

            return input;
        }

        private int Modulo(int a, int b, int c) {
            long x = 1, y = a;

            while (b > 0) {
                if ((b & 1) == 1) {
                    x = (x * y) % c;
                }

                y = (y * y) % c;
                b >>= 1;
            }

            return (int)(x % c);
        }

        private int Mulmod(int a, int b, int c) {
            long x = 0, y = a % c;

            while (b > 0) {
                if ((b & 1) == 1) {
                    x = (x + y) % c;
                }

                y = (y << 1) % c;
                b >>= 1;
            }

            return (int)(x % c);
        }

        private bool Miller(int p, int iteration) {
            if (p < 2) {
                return false;
            }

            if (p != 2 && (p & 1) == 0) {
                return false;
            }

            int s = p - 1;

            while ((s & 1) == 0) {
                s >>= 1;
            }

            for (var i = 0; i < iteration; i++) {
                int a = m_Random.Next() % (p - 1) + 1, temp = s;
                int mod = Modulo(a, temp, p);

                while (temp != p - 1 && mod != 1 && mod != p - 1) {
                    mod = Mulmod(mod, mod, p);
                    temp <<= 1;
                }

                if (mod != p - 1 && (temp & 1) == 0) {
                    return false;
                }
            }

            return true;
        }
    }
}