namespace Klondike {
    /// <summary>
    /// 关卡生成相关入口与帮助文案；与 <see cref="Program"/> 主流程分文件，便于区分。
    /// </summary>
    public partial class Program {
        private const string k_LevelGenerationHelpText =
            """

            
            Level generation (Solved/Minimal only, optional filters; see --help):
            Klondike.exe --generate --attempts 100 --out wins.txt -D 1
            """;

        private static bool TryHandleLevelGenerationArgs(string[] args) {
            if (args == null || args.Length == 0) {
                return false;
            }

            if (args[0] != "--generate") {
                return false;
            }

            LevelGeneration.LevelGenerateRunner.Run(args, 1);
            return true;
        }
    }
}