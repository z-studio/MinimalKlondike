using System;

namespace Klondike.Entities {
    public enum ESolveResult {
        Unknown,
        Impossible,
        Solved,
        Minimal
    }

    public struct SolveDetail {
        public ESolveResult Result;
        public int States;
        public TimeSpan Time;
        public int Moves;

        public override string ToString() {
            return $"{Result} ({States})";
        }
    }
}