using UnityEngine;

public static class ChessRules
{
    public static bool IsLegalMove(
        ChessPiece piece,
        int toX,
        int toY,
        ChessBoard board)
    {
        if (!board.IsInside(toX, toY))
            return false;

        ChessPiece target = board.pieces[toX, toY];
        if (target != null && target.color == piece.color)
            return false;

        switch (piece.type)
        {
            case PieceType.Pawn:
                return Pawn(piece, toX, toY, board);

            case PieceType.Rook:
                return Rook(piece, toX, toY, board);
        }

        return false;
    }

    static bool Pawn(ChessPiece p, int x, int y, ChessBoard b)
    {
        int dir = p.color == PieceColor.White ? 1 : -1;

        // đi thẳng
        if (x == p.x && y == p.y + dir && b.pieces[x, y] == null)
            return true;

        // ăn chéo
        if (Mathf.Abs(x - p.x) == 1 && y == p.y + dir)
            return b.pieces[x, y] != null;

        return false;
    }

    static bool Rook(ChessPiece p, int x, int y, ChessBoard b)
    {
        if (x != p.x && y != p.y) return false;

        int dx = x == p.x ? 0 : (x > p.x ? 1 : -1);
        int dy = y == p.y ? 0 : (y > p.y ? 1 : -1);

        int cx = p.x + dx;
        int cy = p.y + dy;

        while (cx != x || cy != y)
        {
            if (b.pieces[cx, cy] != null)
                return false;

            cx += dx;
            cy += dy;
        }
        return true;
    }
}
