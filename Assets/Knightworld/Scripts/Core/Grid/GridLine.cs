using System;
using System.Collections.Generic;

namespace Knightworld.Core
{
    public static class GridLine
    {
        public static List<GridPos> Cells(GridPos from, GridPos to)
        {
            var cells = new List<GridPos>();
            int x0 = from.X;
            int y0 = from.Y;
            int x1 = to.X;
            int y1 = to.Y;
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int n = 1 + dx + dy;
            int xInc = x1 > x0 ? 1 : x1 < x0 ? -1 : 0;
            int yInc = y1 > y0 ? 1 : y1 < y0 ? -1 : 0;
            int error = dx - dy;
            dx *= 2;
            dy *= 2;
            int x = x0;
            int y = y0;
            for (; n > 0; n--)
            {
                cells.Add(new GridPos(x, y));
                if (error > 0)
                {
                    x += xInc;
                    error -= dy;
                }
                else if (error < 0)
                {
                    y += yInc;
                    error += dx;
                }
                else
                {
                    x += xInc;
                    y += yInc;
                    error -= dy;
                    error += dx;
                    n--;
                }
            }

            if (cells.Count == 0 || cells[cells.Count - 1] != to)
                cells.Add(to);
            return cells;
        }
    }
}
