public interface IBoard
{
    int Size { get; }
    int[] Data { get; }

    bool IsCellEmpty(int index);
    void SetCell(int index, int player);
    void Reset();
}
