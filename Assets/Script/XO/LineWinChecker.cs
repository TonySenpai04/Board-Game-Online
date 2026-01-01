public class LineWinChecker : IWinChecker
{
    private int winLength;

    public LineWinChecker(int winLength)
    {
        this.winLength = winLength;
    }

    public bool CheckWin(IBoard board, int p)
    {
        int size = board.Size;
        int[] b = board.Data;

        int[,] dirs = { {1,0},{0,1},{1,1},{1,-1} };

        for (int i = 0; i < b.Length; i++)
        {
            if (b[i] != p) continue;

            int r = i / size;
            int c = i % size;

            for (int d = 0; d < 4; d++)
            {
                int count = 1;
                count += Count(r, c, dirs[d,0], dirs[d,1], p, board);
                count += Count(r, c, -dirs[d,0], -dirs[d,1], p, board);

                if (count >= winLength)
                    return true;
            }
        }
        return false;
    }

    int Count(int r, int c, int dr, int dc, int p, IBoard board)
    {
        int cnt = 0;
        for (int i = 1; i < winLength; i++)
        {
            int nr = r + dr * i;
            int nc = c + dc * i;
            if (nr < 0 || nc < 0 || nr >= board.Size || nc >= board.Size)
                break;

            if (board.Data[nr * board.Size + nc] != p)
                break;

            cnt++;
        }
        return cnt;
    }
}

