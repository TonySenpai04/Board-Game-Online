public class XOBoard : IBoard
{
    public int Size { get; private set; }
    public int[] Data { get; private set; }

    public XOBoard(int size)
    {
        Size = size;
        Data = new int[size * size];
    }

    public bool IsCellEmpty(int index) => Data[index] == 0;

    public void SetCell(int index, int player)
    {
        Data[index] = player;
    }

    public bool IsFull()
    {
        foreach (var val in Data)
            if (val == 0) return false;
        return true;
    }

    public void Reset()
    {
        for (int i = 0; i < Data.Length; i++)
            Data[i] = 0;
    }
}
