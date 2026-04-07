using System;
using System.Globalization;
using System.IO;
using System.Text;
using Klondike.Entities;

namespace Klondike.LevelGeneration {
    /// <summary>
    /// 批量随机发牌 → 求解 → 仅当 <see cref="ESolveResult.Solved"/> 或 <see cref="ESolveResult.Minimal"/> 且通过筛选时写入牌串。
    /// 不修改 <see cref="Board"/> 核心逻辑，仅调用其公开 API。
    /// </summary>
    public static class LevelGenerateRunner {
        public static void Run(string[] args, int startIndex) {
            var filters = new LevelGenerationFilters();
            int attempts = 1000;
            string outPath = "qualified_deals.txt";
            int drawCount = 1;
            int maxStates = 50_000_000;
            int maxMoves = 250;
            int maxRounds = 15;
            bool useGreenFelt = false;
            var random = new Random();
            IntRangeFilter parsed;

            for (var i = startIndex; i < args.Length; i++) {
                string a = args[i];

                if (a == "--attempts" && i + 1 < args.Length && int.TryParse(args[++i], out int n)) {
                    attempts = Math.Max(1, n);
                } else if ((a == "--out" || a == "-O") && i + 1 < args.Length) {
                    outPath = args[++i];
                } else if (a == "-D" && i + 1 < args.Length && int.TryParse(args[++i], out int d)) {
                    drawCount = d;
                } else if (a == "-S" && i + 1 < args.Length && int.TryParse(args[++i], out int s)) {
                    maxStates = s;
                } else if (a == "--max-moves" && i + 1 < args.Length && int.TryParse(args[++i], out int mm)) {
                    maxMoves = mm;
                } else if (a == "--max-rounds" && i + 1 < args.Length && int.TryParse(args[++i], out int mr)) {
                    maxRounds = mr;
                } else if (a == "--green-felt") {
                    useGreenFelt = true;
                } else if (a == "--filter-key-depth"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    filters.KeyDepthMax = parsed;
                } else if (a == "--filter-first-reveal"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    filters.FirstRevealTotalSteps = parsed;
                } else if (a == "--filter-solve-moves"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    filters.SolveMovesMade = parsed;
                } else if (a == "--filter-all-revealed"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    filters.AllTableauFaceUpSteps = parsed;
                } else if (a == "--filter-stock-aces"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    filters.StockAceCount = parsed;
                } else if (a == "--filter-visible-aces"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    filters.TableauVisibleAceCount = parsed;
                } else if (a == "--filter-movable-tableau"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    filters.ImmediatelyMovableFromTableau = parsed;
                } else if (a == "--filter-facedown-triple-samecolor"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    filters.FaceDownTripleSameColorWindowCount = parsed;
                } else if (a == "--filter-facedown-quadruple-samecolor"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    filters.FaceDownQuadrupleSameColorWindowCount = parsed;
                } else if (a == "--help" || a == "-h") {
                    PrintHelp();
                    return;
                } else {
                    Console.WriteLine($"未知或无效参数: {a}");
                    PrintHelp();
                    return;
                }
            }

            outPath = ResolveLevelGenerationOutputPath(outPath);

            int tried = 0;
            int qualified = 0;
            string dir = Path.GetDirectoryName(outPath);

            if (!string.IsNullOrEmpty(dir)) {
                Directory.CreateDirectory(dir);
            }

            using var writer = new StreamWriter(outPath, append: true, Encoding.UTF8);

            for (var t = 0; t < attempts; t++) {
                tried++;
                var board = new Board(drawCount);
                board.AllowFoundationToTableau = false;

                if (useGreenFelt) {
                    board.ShuffleGreenFelt(unchecked((uint)random.Next()));
                } else {
                    board.Shuffle(random.Next());
                }

                DealStaticMetrics stat = DealAnalyzer.ComputeStatic(board);
                string dealLine = board.GetDeal(false);

                SolveDetail detail = board.Solve(maxMoves, maxRounds, maxStates);

                if (detail.Result != ESolveResult.Solved && detail.Result != ESolveResult.Minimal) {
                    continue;
                }

                Move[] solution = board.RecordedMoves.ToArray();

                if (solution.Length == 0) {
                    continue;
                }

                board.Reset();
                DealReplayMetrics replay = DealAnalyzer.ComputeReplay(board, solution);
                int solveMovesMetric = board.MovesMade;

                if (!filters.Passes(stat, replay, solveMovesMetric)) {
                    continue;
                }

                qualified++;
                writer.WriteLine(dealLine);
                writer.Flush();
            }

            Console.WriteLine($"已尝试 {tried} 局；求解为 Solved/Minimal 且通过筛选并写入: {qualified} 局。");
            Console.WriteLine($"输出文件: {outPath}");
        }

        /// <summary>
        /// <paramref name="userPath"/> 仅含文件名、无目录时，目录为 <see cref="AppContext.BaseDirectory"/>；
        /// 最终文件名在主名与扩展名之间插入 <c>_yyyyMMdd-HHmmss-fff</c>，每次运行不同，避免互相覆盖。
        /// </summary>
        private static string ResolveLevelGenerationOutputPath(string userPath) {
            if (string.IsNullOrWhiteSpace(userPath)) {
                userPath = "qualified_deals.txt";
            }

            userPath = userPath.Trim();
            string fileName = Path.GetFileName(userPath);
            string directoryPart = Path.GetDirectoryName(userPath);

            string resolvedDirectory;

            if (string.IsNullOrEmpty(directoryPart)) {
                resolvedDirectory = AppContext.BaseDirectory;
            } else {
                resolvedDirectory = Path.GetFullPath(directoryPart);
            }

            if (string.IsNullOrEmpty(fileName)) {
                fileName = "qualified_deals.txt";
            }

            string stem = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            if (string.IsNullOrEmpty(stem)) {
                stem = "qualified_deals";
            }

            if (string.IsNullOrEmpty(extension)) {
                extension = ".txt";
            }

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            string stampedName = $"{stem}_{stamp}{extension}";
            return Path.GetFullPath(Path.Combine(resolvedDirectory, stampedName));
        }

        private static void PrintHelp() {
            Console.WriteLine(
                """
                关卡生成模式（首参数须为 --generate）

                Klondike --generate [选项…]

                --attempts N          尝试局数（默认 1000）
                --out PATH            仅文件名无目录时写到可执行文件同目录；每次运行在主文件名后加 _yyyyMMdd-HHmmss-fff 再扩展名；本局结果追加写入该文件（默认 qualified_deals.txt）
                -D #                  每次翻库存张数
                -S #                  求解最大结点数
                --max-moves #         传入 Board.Solve（默认 250）
                --max-rounds #        传入 Board.Solve（默认 15）
                --green-felt          使用 ShuffleGreenFelt 随机（否则 Shuffle）

                筛选（左开右闭 (L,R]，L==R 表示等于 L；未指定则不筛该项）：
                --filter-key-depth L,R           关键牌（盖牌中 A/2/K）上方盖牌张数（全局最大）
                --filter-first-reveal L,R        首次由暗变明时，牌桌步数+牌库步数之和
                --filter-solve-moves L,R         通关序列的 MovesMade（与引擎一致）
                --filter-all-revealed L,R        七列盖牌全翻开时的步数（同上）
                --filter-stock-aces L,R          库存 A 的张数
                --filter-visible-aces L,R        桌面明牌区 A 的张数
                --filter-movable-tableau L,R     开局可移动桌面走法条数
                --filter-facedown-triple-samecolor L,R     七列盖牌中「存在连续 3 张同色（全红或全黑）」的滑动窗个数（可重叠）
                --filter-facedown-quadruple-samecolor L,R  同上，连续 4 张同色

                入库条件：仅当 Solve 返回 Solved 或 Minimal 且通过上述筛选。
                """
            );
        }

        public static void PrintHelpOnly() => PrintHelp();
    }
}