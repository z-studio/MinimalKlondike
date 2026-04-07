using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Klondike {
    public partial class Program {
        /// <summary>
        /// 与可执行文件同目录、无命令行参数时自动读取的文件名（UTF-8）。
        /// </summary>
        public const string kSidecarLaunchArgsFileName = "launch.args";

        /// <summary>
        /// 无参数时尝试加载同目录 <see cref="kSidecarLaunchArgsFileName"/>。
        /// </summary>
        private static string[] ExpandArgsFromConfigIfPresent(string[] args) {
            args ??= Array.Empty<string>();

            if (args.Length != 0) {
                return args;
            }

            string sidecar = Path.Combine(AppContext.BaseDirectory, kSidecarLaunchArgsFileName);

            if (!File.Exists(sidecar)) {
                return args;
            }

            return ReadArgsFromFile(sidecar);
        }

        private static string[] ReadArgsFromFile(string path) {
            string text;

            try {
                text = File.ReadAllText(path, Encoding.UTF8);
            } catch (Exception ex) {
                Console.Error.WriteLine($"无法读取参数文件: {path}");
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
                throw;
            }

            var list = new List<string>();

            using (var reader = new StringReader(text)) {
                string line;

                while ((line = reader.ReadLine()) != null) {
                    int cut = IndexOfCommentOutsideQuotes(line);

                    if (cut >= 0) {
                        line = line.Substring(0, cut);
                    }

                    line = line.Trim();

                    if (line.Length == 0) {
                        continue;
                    }

                    SplitLineIntoArgs(line, list);
                }
            }

            return list.ToArray();
        }

        private static int IndexOfCommentOutsideQuotes(string line) {
            bool inQuotes = false;

            for (var i = 0; i < line.Length; i++) {
                char c = line[i];

                if (c == '"') {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && c == '#') {
                    return i;
                }
            }

            return -1;
        }

        private static void SplitLineIntoArgs(string line, List<string> list) {
            var sb = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++) {
                char c = line[i];

                if (c == '"') {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(c)) {
                    if (sb.Length > 0) {
                        list.Add(sb.ToString());
                        sb.Clear();
                    }
                } else {
                    sb.Append(c);
                }
            }

            if (sb.Length > 0) {
                list.Add(sb.ToString());
            }

            if (inQuotes) {
                Console.Error.WriteLine("警告: 参数文件中存在未闭合的双引号。");
            }
        }
    }
}