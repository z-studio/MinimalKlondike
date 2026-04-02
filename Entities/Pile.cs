using System;
using System.Runtime.CompilerServices;

namespace Klondike.Entities {
    public unsafe struct Pile : IComparable<Pile> {
        public int Size;
        public int First;
        
        private readonly int m_Index;
        private readonly Card[] m_Cards;

        public Pile(Card[] cards, int index) {
            m_Cards = cards;
            m_Index = index;
            Size = 0;
            First = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() {
            Size = 0;
            First = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flip(int count = 1) {
            First = Size - count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Card card) {
            m_Cards[m_Index + Size++] = card;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(ref Pile to) {
            to.Add(m_Cards[m_Index + --Size]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(ref Pile to, int count) {
            int fromIndex = m_Index + Size - count;
            int toIndex = to.m_Index + to.Size;
            var source = new Span<Card>(m_Cards, fromIndex, count);
            var destination = new Span<Card>(m_Cards, toIndex, count);
            source.CopyTo(destination);
            Size -= count;
            to.Size += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveFlip(ref Pile to, int count) {
            int fromIndex = m_Index + Size - count;
            int toIndex = to.m_Index + to.Size;
            var source = new Span<Card>(m_Cards, fromIndex, count);
            var destination = new Span<Card>(m_Cards, toIndex, count);
            source.CopyTo(destination);
            destination.Reverse();
            Size -= count;
            to.Size += count;
        }

        public Card this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Cards[m_Index + index];
        }

        public Card Bottom {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Size > 0 ? m_Cards[m_Index + Size - 1] : Card.Empty;
        }

        public Card BottomNoCheck {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Cards[m_Index + Size - 1];
        }

        public Card Top {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Size > 0 ? m_Cards[m_Index + First] : Card.Empty;
        }

        public Card TopNoCheck {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Cards[m_Index + First];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Card Up(int size) {
            int index = Size - size - 1;
            return index >= 0 ? m_Cards[m_Index + index] : Card.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Card UpNoCheck(int size) {
            return m_Cards[m_Index + Size - size - 1];
        }

        public int UpSize {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Size - First;
        }

        public override string ToString() {
            return $"Max: {m_Cards.Length} Size: {Size} First: {First}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Pile other) {
            int upCompare = other.UpSize.CompareTo(UpSize);

            if (upCompare != 0) {
                return upCompare;
            }

            return other.m_Index.CompareTo(m_Index);
        }
    }
}