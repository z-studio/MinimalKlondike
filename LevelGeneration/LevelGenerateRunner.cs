using System;
using System.Globalization;
using System.IO;
using System.Text;
using Klondike.Entities;

namespace Klondike.LevelGeneration {
    /// <summary>
    /// 批量随机发牌 → 求解 → 仅当 <see cref="ESolveResult.Solved"/> 或 <see cref="ESolveResult.Minimal"/> 且通过筛选时写入输出文件。
    /// 每局四行：牌局串一行、<see cref="Board.GetKeyRankDeckIndexSummary"/>（m_Deck 中 A/2/K 的 0-based 下标）一行、<see cref="Board.MovesMadeOutput"/> 走法一行、空行分隔；不修改 <see cref="Board"/> 核心逻辑，仅调用其公开 API。
    /// </summary>
    public static class LevelGenerateRunner {
        public static void Run(string[] args, int startIndex) {
            var p = LevelGenerationRunParameters.CreateDefault();
            var i = startIndex;

            while (i < args.Length && args[i] == "--config") {
                if (i + 1 >= args.Length) {
                    Console.WriteLine("--config 需要指定 YAML 配置文件路径（.yaml 或 .yml）。");
                    PrintHelp();
                    return;
                }

                string path = args[++i];

                try {
                    var cfg = LevelGenerationConfig.Load(path);
                    p.ApplyYaml(cfg);
                    Console.WriteLine($"已载入关卡生成配置: {Path.GetFullPath(path)}");
                } catch (Exception ex) {
                    Console.WriteLine($"读取配置失败: {path}");
                    Console.WriteLine(ex.Message);
                    return;
                }

                i++;
            }

            if (!TryParseCliArgs(args, i, p)) {
                return;
            }

            Execute(p);
        }

        /// <summary>
        /// 解析关卡生成命令行；筛选类参数值为 <c>L,R</c>，语义见 <see cref="IntRangeFilter"/>，与 YAML <c>filters</c> 字段一一对应。
        /// </summary>
        static bool TryParseCliArgs(string[] args, int startIndex, LevelGenerationRunParameters p) {
            IntRangeFilter parsed;

            for (var i = startIndex; i < args.Length; i++) {
                string a = args[i];

                if (a == "--attempts" && i + 1 < args.Length && int.TryParse(args[++i], out int n)) {
                    p.Attempts = Math.Max(1, n);
                } else if ((a == "--out" || a == "-O") && i + 1 < args.Length) {
                    p.OutPath = args[++i];
                } else if (a == "-D" && i + 1 < args.Length && int.TryParse(args[++i], out int d)) {
                    p.DrawCount = d;
                } else if (a == "-S" && i + 1 < args.Length && int.TryParse(args[++i], out int s)) {
                    p.MaxStates = s;
                } else if (a == "--max-rounds" && i + 1 < args.Length && int.TryParse(args[++i], out int mr)) {
                    p.MaxRounds = mr;
                } 
                // —— 筛选（下一参数均为 "L,R"，写入 p.Filters，最终由 LevelGenerationFilters.Passes 判定）——
                else if (a == "--filter-key-ace-cover-depth"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.KeyAceCoverDepth = parsed;
                } else if (a == "--filter-key-ace-cover-count"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.KeyAceCoverCount = parsed;
                } else if (a == "--filter-key-two-cover-depth"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.KeyTwoCoverDepth = parsed;
                } else if (a == "--filter-key-two-cover-count"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.KeyTwoCoverCount = parsed;
                } else if (a == "--filter-key-king-cover-depth"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.KeyKingCoverDepth = parsed;
                } else if (a == "--filter-key-king-cover-count"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.KeyKingCoverCount = parsed;
                } else if (a == "--filter-first-reveal"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.FirstRevealTotalSteps = parsed;
                } else if (a == "--filter-solve-moves"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.SolveMovesMade = parsed;
                } else if (a == "--filter-all-revealed"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.AllTableauFaceUpSteps = parsed;
                } else if (a == "--filter-stock-aces"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.StockAceCount = parsed;
                } else if (a == "--filter-visible-aces"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.TableauVisibleAceCount = parsed;
                } else if (a == "--filter-movable-tableau"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.ImmediatelyMovableFromTableau = parsed;
                } else if (a == "--filter-facedown-triple-samecolor"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.FaceDownTripleSameColorWindowCount = parsed;
                } else if (a == "--filter-facedown-quadruple-samecolor"
                           && i + 1 < args.Length
                           && IntRangeFilter.TryParse(args[++i], out parsed)) {
                    p.Filters.FaceDownQuadrupleSameColorWindowCount = parsed;
                } else if (a == "--help" || a == "-h") {
                    PrintHelp();
                    return false;
                } else {
                    Console.WriteLine($"未知或无效参数: {a}");
                    PrintHelp();
                    return false;
                }
            }

            return true;
        }

        static void Execute(LevelGenerationRunParameters p) {
            string outPath = ResolveLevelGenerationOutputPath(p.OutPath);
            int tried = 0;
            int qualified = 0;
            string dir = Path.GetDirectoryName(outPath);

            if (!string.IsNullOrEmpty(dir)) {
                Directory.CreateDirectory(dir);
            }

            var seed = (int)DateTime.UtcNow.Ticks;
            using var writer = new StreamWriter(outPath, append: true, Encoding.UTF8);

            for (var t = 0; t < p.Attempts; t++) {
                tried++;
                
                Console.WriteLine($"开始第 {tried} 局。");
                
                var board = new Board(p.DrawCount) {
                    AllowFoundationToTableau = true
                };

                if (p.CardWeights != null) {
                    board.ShuffleCardWeight(seed + t, p.CardWeights);
                } else {
                    board.ShuffleCardWeight(seed + t);
                }

                DealStaticMetrics stat = DealAnalyzer.ComputeStatic(board);
                string dealLine = board.GetDeal(false);
                string keyDeckIndicesLine = board.GetKeyRankDeckIndexSummary();

                var maxMoves = p.Filters.SolveMovesMade.Active ? p.Filters.SolveMovesMade.RightInclusive : 250;
                SolveDetail detail = board.Solve(maxMoves, p.MaxRounds, p.MaxStates);

                if (detail.Result != ESolveResult.Solved && detail.Result != ESolveResult.Minimal) {
                    continue;
                }

                if (p.Filters.SolveMovesMade.Active && detail.Moves <= p.Filters.SolveMovesMade.LeftOpen) {
                    continue;
                }

                Move[] solution = board.RecordedMoves.ToArray();

                if (solution.Length == 0) {
                    continue;
                }

                board.Reset();
                DealReplayMetrics replay = DealAnalyzer.ComputeReplay(board, solution);
                // 与「解的操作步数」一致：通关解里 Move 的条数（同 RecordedMoves / solution.Length），非 Board.MovesMade 的折算点击数
                int solutionMoveCount = solution.Length;

                if (!p.Filters.Passes(stat, replay, solutionMoveCount)) {
                    continue;
                }

                qualified++;
                writer.WriteLine(dealLine);
                writer.WriteLine(keyDeckIndicesLine);
                // ComputeReplay 已把 solution 完整 MakeMove，MovesMadeOutput 与 Solve 结束时一致（@=翻库，其后为 Move 字母对）
                writer.WriteLine($"步数: {detail.Moves}  |  执行序列：{board.MovesMadeOutput.TrimEnd()}");
                writer.WriteLine();
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

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string stampedName = $"{stem}_{stamp}{extension}";
            return Path.GetFullPath(Path.Combine(resolvedDirectory, stampedName));
        }

        private static void PrintHelp() {
            Console.WriteLine(
                """
                关卡生成模式（首参数须为 --generate）

                Klondike --generate [--config 路径.yaml|.yml]… [选项…]

                --config PATH         读取 YAML 配置（UTF-8，支持 # 注释）；扩展名须为 .yaml 或 .yml；可多次指定，后读入的覆盖同名字段；再之后的命令行选项仍可覆盖
                --attempts N          尝试局数（默认 1000）
                --out PATH            仅文件名无目录时写到可执行文件同目录；每次运行在主文件名后加 _yyyyMMdd-HHmmss 再扩展名；合格局追加写入（默认 qualified_deals.txt）。每局：牌局串一行、m_Deck 中 A/2/K 下标一行（GetKeyRankDeckIndexSummary）、走法一行、再空一行
                -D #                  每次翻库存张数
                -S #                  求解最大结点数
                --max-rounds #        传入 Board.Solve 的 maxRounds（默认 15）；maxMoves 固定 250（与 Board.Solve 默认一致）

                发牌：始终使用 Board.ShuffleCardWeight；第 t 次尝试（0-based）种子为 t；52 维权重在配置文件的 cardWeights 中设置（省略则全 1）。

                筛选（左开右闭 (L,R]，L==R 表示等于 L；未指定则不筛该项）：
                盖牌 A/2/K（各点数独立）：须同时给出「张数」筛才生效。深度筛可选；未给深度筛时凡该点数的盖牌均计入张数。
                --filter-key-ace-cover-depth L,R    盖牌 A：盖牌深度 first-j-1 落入 (L,R] 才计入张数
                --filter-key-ace-cover-count L,R    计入的盖牌 A 的张数须在 (L,R]
                --filter-key-two-cover-depth L,R    同上，点数为 2
                --filter-key-two-cover-count L,R
                --filter-key-king-cover-depth L,R   同上，点数为 K
                --filter-key-king-cover-count L,R
                --filter-first-reveal L,R        首次由暗变明时，牌桌步数+牌库步数之和
                --filter-solve-moves L,R         通关解序列的 Move 条数（与 Board.RecordedMoves 长度一致）
                --filter-all-revealed L,R        七列盖牌全翻开时的步数（同上）
                --filter-stock-aces L,R          库存 A 的张数
                --filter-visible-aces L,R        桌面明牌区 A 的张数
                --filter-movable-tableau L,R     开局可移动桌面走法条数
                --filter-facedown-triple-samecolor L,R     七列盖牌中「存在连续 3 张同色（全红或全黑）」的滑动窗个数（可重叠）
                --filter-facedown-quadruple-samecolor L,R  同上，连续 4 张同色

                入库条件：仅当 Solve 返回 Solved 或 Minimal 且通过上述筛选。

                走法行格式见 Board.MovesMadeOutput（@ 表示 talon 翻动，其后为各 Move 的字母对）。

                示例：LevelGeneration/levelgen.example.yaml
                """
            );
        }
    }
}
