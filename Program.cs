using Klondike.Entities;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Klondike {
    public partial class Program {
        public static void Main(string[] args) {
            args ??= Array.Empty<string>();

            if (args.Length == 1 && IsMainHelpSwitch(args[0])) {
                PrintMainHelp();
                return;
            }

            args = ExpandArgsFromConfigIfPresent(args);

            if (TryHandleLevelGenerationArgs(args)) {
                return;
            }

            if (args != null && args.Length > 0 && ((args.Length - 1) & 1) == 1) {
                Console.WriteLine($"Invalid argument count.");
                args = null;
            }

            if (args == null || args.Length == 0) {
                PrintMainHelp();
                return;
            }

            var cardSet = args[^1].Replace("\"", "");
            var drawCount = 1;
            string moveSet = null;
            var maxStates = 50_000_000;

            for (var i = 0; i < args.Length - 1; i++) {
                if (args[i] == "-D" && i + 1 < args.Length) {
                    if (!int.TryParse(args[i + 1], out drawCount)) {
                        Console.WriteLine($"Invalid DrawCount argument {args[i + 1]}. Defaulting to 1.");
                        drawCount = 1;
                    }

                    i++;
                } else if (args[i] == "-S" && i + 1 < args.Length) {
                    if (!int.TryParse(args[i + 1], NumberStyles.AllowThousands, null, out maxStates)) {
                        Console.WriteLine($"Invalid MaxStates argument {args[i + 1]}. Defaulting to 50,000,000.");
                        maxStates = 50_000_000;
                    }

                    i++;
                } else if (args[i] == "-M" && i + 1 < args.Length) {
                    moveSet = args[i + 1];
                    i++;
                }
            }

            var sw = new Stopwatch();
            sw.Start();

            if (cardSet.Length < 11) {
                uint.TryParse(cardSet, out uint seed);
                SolveGame(seed, drawCount, moveSet, maxStates);
            } else {
                SolveGame(cardSet, drawCount, moveSet, maxStates);
            }

            sw.Stop();
            Console.WriteLine($"Done {sw.Elapsed}");
        }

        private static bool IsMainHelpSwitch(string s) {
            return s.Equals("--help", StringComparison.OrdinalIgnoreCase)
                || s.Equals("-h", StringComparison.OrdinalIgnoreCase)
                || s.Equals("-?", StringComparison.OrdinalIgnoreCase)
                || s.Equals("/?", StringComparison.OrdinalIgnoreCase);
        }

        private static void PrintMainHelp() {
            Console.WriteLine(
                $"""
                Minimal Klondike
                Klondike.exe [Options] [CardSet]

                分发他人使用时：将 {kSidecarLaunchArgsFileName} 放在与本程序同一目录；用户直接双击或运行程序且不带参数时，会自动读取该文件（UTF-8）作为命令行参数。
                若同目录没有该文件、或文件解析后为空，则显示本帮助。
                仍可在命令行直接写参数（不会读取 {kSidecarLaunchArgsFileName}）；仅输入 --help / -h / -? / /? 可查看本帮助且不执行 {kSidecarLaunchArgsFileName}。
                参数文件语法：行内 # 仅在双引号外为注释；含空格的值用双引号。

                DrawCount (Default=1)
                -D #

                Initial Moves
                -M "Moves To Play Initially"

                Max States (Default=50,000,000) (About 1GB RAM Per 22 Million)
                -S #

                Solve Seed 123 from GreenFelt:
                Klondike.exe 123

                Solve Given CardSet With Initial Moves:
                Klondike.exe -D 1 -M "HE KE @@@@AD GD LJ @@AH @@AJ GJ @@@@AG @AB" 081054022072134033082024052064053012061013042093084124092122062031083121113023043074051114091014103044131063041102101133011111071073034123104112021132032094
                """ + k_LevelGenerationHelpText
            );
        }

        private static SolveDetail SolveGame(
            uint deal,
            int drawCount = 1,
            string movesMade = null,
            int maxStates = 50_000_000
        ) {
            var board = new Board(drawCount);
            board.ShuffleGreenFelt(deal);

            if (!string.IsNullOrEmpty(movesMade)) {
                board.PlayMoves(movesMade);
            }

            board.AllowFoundationToTableau = false;
            return SolveGame(board, maxStates);
        }

        private static SolveDetail SolveGame(
            string deal,
            int drawCount = 1,
            string movesMade = null,
            int maxStates = 50_000_000
        ) {
            var board = new Board(drawCount);
            board.SetDeal(deal);

            if (!string.IsNullOrEmpty(movesMade)) {
                board.PlayMoves(movesMade);
            }

            board.AllowFoundationToTableau = false;
            return SolveGame(board, maxStates);
        }

        private static SolveDetail SolveGame(Board board, int maxStates) {
            Console.WriteLine($"Deal: {board.GetDeal()}");
            Console.WriteLine();
            Console.WriteLine(board);

            SolveDetail result = board.Solve(250, 15, maxStates);

            Console.WriteLine($"Moves: {board.MovesMadeOutput}");
            Console.WriteLine();

            Console.WriteLine(
                $"(Deal Result: {
                    result.Result
                } Foundation: {
                    board.CardsInFoundation
                } Moves: {
                    board.MovesMade
                } Rounds: {
                    board.TimesThroughDeck
                } States: {
                    result.States
                } Took: {
                    result.Time
                })"
            );

            return result;
        }
    }
}