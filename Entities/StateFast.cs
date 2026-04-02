using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Klondike.Entities {
    [StructLayout(LayoutKind.Sequential, Size = 10, Pack = 2)]
    public unsafe struct StateFast : IComparable<StateFast>, IEquatable<StateFast> {
        private const int k_KeyLength = 8;
        
        public fixed byte Key[k_KeyLength];
        public Estimate Moves;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(StateFast other) {
            fixed (byte* key = Key) {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(key, k_KeyLength);
                return span.SequenceCompareTo(new ReadOnlySpan<byte>(other.Key, k_KeyLength));
            }
        }

        public byte this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Key[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Key[index] = value;
        }

        public override string ToString() {
            var sb = new StringBuilder(k_KeyLength * 3);

            for (var i = 0; i < k_KeyLength; i++) {
                sb.Append($"{Key[i]:X2} ");
            }

            sb.Length--;
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public override int GetHashCode() {
            unchecked {
                var hash = 17;

                for (var i = 0; i < k_KeyLength; i++) {
                    hash = (hash * 127) + Key[i];
                }

                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) {
            return obj is State other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(StateFast other) {
            fixed (byte* key = Key) {
                var span = new ReadOnlySpan<byte>(key, k_KeyLength);
                return span.SequenceEqual(new ReadOnlySpan<byte>(other.Key, k_KeyLength));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(StateFast left, StateFast right) {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(StateFast left, StateFast right) {
            return !left.Equals(right);
        }
    }
}