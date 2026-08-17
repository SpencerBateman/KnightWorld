using System;

namespace Knightworld.Core
{
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public int X { get; }
        public int Y { get; }

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int Manhattan(GridPos other) =>
            Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        public int Chebyshev(GridPos other) =>
            Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));

        public int DistanceFeet(GridPos other) => Chebyshev(other) * GridMap.FeetPerSquare;

        public GridPos Offset(int dx, int dy) => new GridPos(X + dx, Y + dy);

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is GridPos other && Equals(other);

        public override int GetHashCode() => (X * 397) ^ Y;

        public override string ToString() => $"({X},{Y})";

        public static bool operator ==(GridPos left, GridPos right) => left.Equals(right);

        public static bool operator !=(GridPos left, GridPos right) => !left.Equals(right);
    }
}
