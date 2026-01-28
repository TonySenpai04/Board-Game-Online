public interface IBoard
{
    int Size { get; }
    int[] Data { get; }

    bool IsCellEmpty(int index);
    bool IsFull();
    void SetCell(int index, int player);
    void Reset();
}
