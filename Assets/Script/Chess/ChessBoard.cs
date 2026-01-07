using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    public ChessCell[,] cells = new ChessCell[8, 8];
    public ChessPiece[,] pieces = new ChessPiece[8, 8];

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < 8 && y >= 0 && y < 8;
    }
}
