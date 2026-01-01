using System.Collections.Generic;

public static class WinLineFinder
{
    // Return list of winning indices (first-to-last) for given player, or empty list
    public static List<int> GetWinningLine(IBoard board, int player, int winLength)
    {
        var result = new List<int>();
        int size = board.Size;
        var data = board.Data;
        int[,] dirs = new int[,] { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };

        for (int idx = 0; idx < data.Length; idx++)
        {
            if (data[idx] != player) continue;
            int r = idx / size;
            int c = idx % size;

            for (int d = 0; d < dirs.GetLength(0); d++)
            {
                int dr = dirs[d, 0];
                int dc = dirs[d, 1];
                int count = 1;

                // forward
                int rr = r + dr;
                int cc = c + dc;
                while (rr >= 0 && rr < size && cc >= 0 && cc < size && data[rr * size + cc] == player)
                {
                    count++; rr += dr; cc += dc;
                }

                // backward
                rr = r - dr; cc = c - dc;
                while (rr >= 0 && rr < size && cc >= 0 && cc < size && data[rr * size + cc] == player)
                {
                    count++; rr -= dr; cc -= dc;
                }

                if (count >= winLength)
                {
                    // find start
                    int startR = r, startC = c;
                    while (startR - dr >= 0 && startR - dr < size && startC - dc >= 0 && startC - dc < size && data[(startR - dr) * size + (startC - dc)] == player)
                    {
                        startR -= dr; startC -= dc;
                    }

                    int cr = startR, cc2 = startC;
                    int taken = 0;
                    while (cr >= 0 && cr < size && cc2 >= 0 && cc2 < size && data[cr * size + cc2] == player && taken < count)
                    {
                        result.Add(cr * size + cc2);
                        taken++; cr += dr; cc2 += dc;
                    }

                    return result;
                }
            }
        }

        return result;
    }
}
