using System;
using System.Collections.Generic;

namespace Knightworld.Core
{
    public enum CellFeature
    {
        None = 0,
        Tree = 1,
        Wall = 2
    }

    public sealed class GridCell
    {
        public bool Walkable { get; set; } = true;
        public bool BlocksSight { get; set; }
        public CellFeature Feature { get; set; }
        public int Height { get; set; }
        public CoverLevel North { get; set; }
        public CoverLevel East { get; set; }
        public CoverLevel South { get; set; }
        public CoverLevel West { get; set; }

        public CoverLevel Get(Cardinal dir)
        {
            switch (dir)
            {
                case Cardinal.North: return North;
                case Cardinal.East: return East;
                case Cardinal.South: return South;
                default: return West;
            }
        }

        public void Set(Cardinal dir, CoverLevel level)
        {
            switch (dir)
            {
                case Cardinal.North: North = level; break;
                case Cardinal.East: East = level; break;
                case Cardinal.South: South = level; break;
                default: West = level; break;
            }
        }
    }

    public sealed class GridMap
    {
        public const int FeetPerSquare = 5;

        public int Width { get; }
        public int Height { get; }

        private readonly GridCell[] _cells;

        public GridMap(int width, int height)
        {
            if (width < 1 || height < 1)
                throw new ArgumentOutOfRangeException(nameof(width), "Map must be at least 1x1.");
            Width = width;
            Height = height;
            _cells = new GridCell[width * height];
            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = new GridCell();
        }

        public GridCell this[GridPos pos] => _cells[Index(pos)];

        public GridCell this[int x, int y] => this[new GridPos(x, y)];

        public bool InBounds(GridPos pos) =>
            pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height;

        public bool IsWalkable(GridPos pos) => InBounds(pos) && this[pos].Walkable;

        public bool BlocksLineOfSight(GridPos pos)
        {
            if (!InBounds(pos))
                return true;
            var cell = this[pos];
            if (cell.Feature == CellFeature.Tree)
                return false;
            return cell.BlocksSight || !cell.Walkable;
        }

        public void PlaceWall(GridPos pos)
        {
            if (!InBounds(pos))
                return;
            var cell = this[pos];
            cell.Walkable = false;
            cell.BlocksSight = true;
            cell.Feature = CellFeature.Wall;
        }

        public void PlaceTree(GridPos pos)
        {
            if (!InBounds(pos))
                return;
            var cell = this[pos];
            cell.Walkable = false;
            cell.BlocksSight = false;
            cell.Feature = CellFeature.Tree;
        }

        public int Index(GridPos pos) => pos.Y * Width + pos.X;

        public void SetCover(GridPos pos, Cardinal dir, CoverLevel level)
        {
            if (!InBounds(pos))
                return;
            this[pos].Set(dir, CoverRules.Max(this[pos].Get(dir), level));
            var neighbor = pos.Offset(CoverRules.Step(dir).X, CoverRules.Step(dir).Y);
            if (InBounds(neighbor))
                this[neighbor].Set(CoverRules.Opposite(dir), CoverRules.Max(this[neighbor].Get(CoverRules.Opposite(dir)), level));
        }

        public CoverLevel EdgeCoverBetween(GridPos from, GridPos to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            if (Math.Abs(dx) + Math.Abs(dy) != 1)
            {
                if (Math.Abs(dx) == 1 && Math.Abs(dy) == 1)
                    return CoverRules.Max(OrthogonalEdge(from, from.Offset(dx, 0)), OrthogonalEdge(from, from.Offset(0, dy)));
                return CoverLevel.None;
            }

            return OrthogonalEdge(from, to);
        }

        private CoverLevel OrthogonalEdge(GridPos from, GridPos to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            Cardinal dir;
            if (dx == 1) dir = Cardinal.East;
            else if (dx == -1) dir = Cardinal.West;
            else if (dy == 1) dir = Cardinal.North;
            else dir = Cardinal.South;

            var a = InBounds(from) ? this[from].Get(dir) : CoverLevel.None;
            var b = InBounds(to) ? this[to].Get(CoverRules.Opposite(dir)) : CoverLevel.None;
            return CoverRules.Max(a, b);
        }

        public bool CanStep(GridPos from, GridPos to)
        {
            if (!IsWalkable(to))
                return false;
            int dx = Math.Abs(to.X - from.X);
            int dy = Math.Abs(to.Y - from.Y);
            if (dx > 1 || dy > 1 || dx + dy == 0)
                return false;
            if (EdgeCoverBetween(from, to) == CoverLevel.Wall)
                return false;
            if (dx == 1 && dy == 1)
            {
                var orthoA = from.Offset(to.X - from.X, 0);
                var orthoB = from.Offset(0, to.Y - from.Y);
                if (!CanStepOrthogonal(from, orthoA) || !CanStepOrthogonal(from, orthoB))
                    return false;
            }

            return true;
        }

        private bool CanStepOrthogonal(GridPos from, GridPos to)
        {
            if (!IsWalkable(to))
                return false;
            return EdgeCoverBetween(from, to) != CoverLevel.Wall;
        }

        public CoverLevel GetFacingCover(GridPos attacker, GridPos defender)
        {
            if (!InBounds(defender))
                return CoverLevel.None;
            var cell = this[defender];
            var cover = CoverLevel.None;
            int dx = attacker.X - defender.X;
            int dy = attacker.Y - defender.Y;
            if (dx < 0) cover = CoverRules.Max(cover, cell.West);
            if (dx > 0) cover = CoverRules.Max(cover, cell.East);
            if (dy < 0) cover = CoverRules.Max(cover, cell.South);
            if (dy > 0) cover = CoverRules.Max(cover, cell.North);
            return cover;
        }

        public bool HasLineOfSight(GridPos from, GridPos to)
        {
            if (from == to)
                return false;
            var cells = GridLine.Cells(from, to);
            for (int i = 0; i < cells.Count - 1; i++)
            {
                var a = cells[i];
                var b = cells[i + 1];
                if (EdgeCoverBetween(a, b) == CoverLevel.Wall)
                    return false;
                if (!b.Equals(to) && BlocksLineOfSight(b))
                    return false;
            }

            return true;
        }

        public CoverLevel GetCoverAgainst(GridPos attacker, GridPos defender)
        {
            if (!HasLineOfSight(attacker, defender))
                return CoverLevel.Wall;

            var cover = GetFacingCover(attacker, defender);
            var cells = GridLine.Cells(attacker, defender);
            for (int i = 0; i < cells.Count - 1; i++)
            {
                var a = cells[i];
                var b = cells[i + 1];
                var edge = EdgeCoverBetween(a, b);
                if (edge == CoverLevel.Wall)
                    return CoverLevel.Wall;
                if (i > 0)
                    cover = CoverRules.Max(cover, edge);
            }

            return cover;
        }
    }
}
