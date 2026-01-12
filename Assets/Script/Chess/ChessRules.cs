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
            case PieceType.Knight:
                return Knight(piece, toX, toY, board);
            case PieceType.Bishop:
                return Bishop(piece, toX, toY, board);
            case PieceType.Queen:
                return Queen(piece, toX, toY, board);
            case PieceType.King:
                return King(piece, toX, toY, board);
        }

        return false;
    }

    // Return true if the square (x,y) is attacked by any piece of attackerColor
    public static bool IsSquareAttacked(ChessBoard board, int x, int y, PieceColor attackerColor)
    {
        // Iterate all pieces of attackerColor and see if they can move to x,y (using attack rules)
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                var p = board.pieces[i, j];
                if (p == null || p.color != attackerColor) continue;

                // For pawns, only diagonal captures count as attacks
                if (p.type == PieceType.Pawn)
                {
                    // Update direction: White (at 7) moves Up (-1), Black (at 0) moves Down (+1)
                    int dir = p.color == PieceColor.White ? -1 : 1;
                    if ((i + 1 == x || i - 1 == x) && j + dir == y) return true;
                    continue;
                }

                // For other pieces, reuse movement patterns but ignore occupancy by same-color at target
                if (p.type == PieceType.Knight)
                {
                    if (Knight(p, x, y, board)) return true;
                }
                else if (p.type == PieceType.Bishop)
                {
                    if (Bishop(p, x, y, board)) return true;
                }
                else if (p.type == PieceType.Rook)
                {
                    if (Rook(p, x, y, board)) return true;
                }
                else if (p.type == PieceType.Queen)
                {
                    if (Queen(p, x, y, board)) return true;
                }
                else if (p.type == PieceType.King)
                {
                    if (King(p, x, y, board)) return true;
                }
            }
        }
        return false;
    }

    public static bool IsInCheck(ChessBoard board, PieceColor color)
    {
        // find king
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                var p = board.pieces[i, j];
                if (p != null && p.type == PieceType.King && p.color == color)
                    return IsSquareAttacked(board, i, j, Opposite(color));
            }
        return false;
    }

    public static bool HasAnyLegalMoves(ChessBoard board, PieceColor color)
    {
        // Try every move and see if it leaves king in check
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                var p = board.pieces[i, j];
                if (p == null || p.color != color) continue;

                for (int tx = 0; tx < 8; tx++)
                {
                    for (int ty = 0; ty < 8; ty++)
                    {
                        if (!IsLegalMove(p, tx, ty, board)) continue;

                        // simulate
                        var captured = board.pieces[tx, ty];
                        board.pieces[p.x, p.y] = null;
                        board.pieces[tx, ty] = p;
                        int oldX = p.x, oldY = p.y;
                        p.x = tx; p.y = ty;

                        bool inCheck = IsInCheck(board, color);

                        // undo
                        p.x = oldX; p.y = oldY;
                        board.pieces[p.x, p.y] = p;
                        board.pieces[tx, ty] = captured;

                        if (!inCheck) return true;
                    }
                }
            }
        }
        return false;
    }

    static PieceColor Opposite(PieceColor c) => c == PieceColor.White ? PieceColor.Black : PieceColor.White;

    static bool Pawn(ChessPiece p, int x, int y, ChessBoard b)
    {
        // Update direction: White (at 7) moves Up (-1), Black (at 0) moves Down (+1)
        int dir = p.color == PieceColor.White ? -1 : 1;
        int startY = p.color == PieceColor.White ? 6 : 1;

        // đi thẳng 1 ô
        if (x == p.x && y == p.y + dir && b.pieces[x, y] == null)
            return true;

        // đi thẳng 2 ô (nếu ở vị trí xuất phát)
        if (x == p.x && p.y == startY && y == p.y + 2 * dir)
        {
            // Phải trống cả 2 ô
            if (b.pieces[x, p.y + dir] == null && b.pieces[x, y] == null)
                return true;
        }

        // ăn chéo
        if (Mathf.Abs(x - p.x) == 1 && y == p.y + dir)
            return b.pieces[x, y] != null;

        return false;
    }

    static bool Knight(ChessPiece p, int x, int y, ChessBoard b)
    {
        int dx = Mathf.Abs(x - p.x);
        int dy = Mathf.Abs(y - p.y);
        return (dx == 1 && dy == 2) || (dx == 2 && dy == 1);
    }

    static bool Bishop(ChessPiece p, int x, int y, ChessBoard b)
    {
        if (Mathf.Abs(x - p.x) != Mathf.Abs(y - p.y)) return false;

        int dx = x > p.x ? 1 : -1;
        int dy = y > p.y ? 1 : -1;
        int cx = p.x + dx;
        int cy = p.y + dy;
        while (cx != x && cy != y)
        {
            if (b.pieces[cx, cy] != null) return false;
            cx += dx; cy += dy;
        }
        return true;
    }

    static bool Queen(ChessPiece p, int x, int y, ChessBoard b)
    {
        return Rook(p, x, y, b) || Bishop(p, x, y, b);
    }

    static bool King(ChessPiece p, int x, int y, ChessBoard b)
    {
        int dx = Mathf.Abs(x - p.x);
        int dy = Mathf.Abs(y - p.y);
        if (dx <= 1 && dy <= 1)
        {
            // ensure target square is not attacked by opponent
            if (IsSquareAttacked(b, x, y, Opposite(p.color))) return false;
            return true;
        }
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
    public static bool IsInsufficientMaterial(ChessBoard board)
    {
        int whitePieces = 0;
        int blackPieces = 0;
        int whiteKnights = 0;
        int whiteBishops = 0;
        int blackKnights = 0;
        int blackBishops = 0;

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                var p = board.pieces[i, j];
                if (p == null) continue;

                if (p.type == PieceType.Pawn || p.type == PieceType.Rook || p.type == PieceType.Queen)
                    return false; // Major pieces or pawns always mean sufficient material

                if (p.color == PieceColor.White)
                {
                    whitePieces++;
                    if (p.type == PieceType.Knight) whiteKnights++;
                    if (p.type == PieceType.Bishop) whiteBishops++;
                }
                else
                {
                    blackPieces++;
                    if (p.type == PieceType.Knight) blackKnights++;
                    if (p.type == PieceType.Bishop) blackBishops++;
                }
            }
        }

        // King vs King
        if (whitePieces == 1 && blackPieces == 1) return true;

        // King + Knight vs King
        if (whitePieces == 2 && whiteKnights == 1 && blackPieces == 1) return true;
        if (blackPieces == 2 && blackKnights == 1 && whitePieces == 1) return true;

        // King + Bishop vs King
        if (whitePieces == 2 && whiteBishops == 1 && blackPieces == 1) return true;
        if (blackPieces == 2 && blackBishops == 1 && whitePieces == 1) return true;

        return false;
    }
}
