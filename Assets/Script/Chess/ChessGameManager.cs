using Photon.Pun;
using UnityEngine;

public class ChessGameManager : MonoBehaviourPunCallbacks
{
    public static ChessGameManager Instance;

    public ChessBoard board;

    public ChessCell cellPrefab;
    public ChessPiece piecePrefab;
    // sprites for alternating board squares (assign in Inspector)
    public Sprite lightCellSprite;
    public Sprite darkCellSprite;
    // If true, treat the board coordinates as swapped when parenting/spawning pieces.
    // Set this to true if your board's visual layout uses [y,x] ordering so pieces appear horizontal.
    public bool transposeBoard = false;

    ChessPiece selectedPiece;
    bool myTurn;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateBoard();
        SpawnPieces();

        myTurn = PhotonNetwork.IsMasterClient; // white đi trước
    }

    public PieceColor MyColor =>
        PhotonNetwork.IsMasterClient ? PieceColor.White : PieceColor.Black;

    void CreateBoard()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessCell cell = Instantiate(cellPrefab, board.transform);
                cell.x = x;
                cell.y = y;
                board.cells[x, y] = cell;

                // assign alternating background sprite if available
                if (lightCellSprite != null && darkCellSprite != null)
                {
                    Sprite s = ((x + y) % 2 == 0) ? lightCellSprite : darkCellSprite;
                    cell.SetBackground(s);
                }
            }
        }
    }
    void SpawnPieces()
    {
        // Pawns
        for (int x = 0; x < 8; x++)
        {
            Spawn(PieceType.Pawn, PieceColor.White, 1, x);
            Spawn(PieceType.Pawn, PieceColor.Black, 6, x);
        }

        // Back ranks: R N B Q K B N R
        PieceType[] backRank = new PieceType[] {
            PieceType.Rook,
            PieceType.Knight,
            PieceType.Bishop,
            PieceType.Queen,
            PieceType.King,
            PieceType.Bishop,
            PieceType.Knight,
            PieceType.Rook
        };

        for (int x = 0; x < 8; x++)
        {
            Spawn(backRank[x], PieceColor.White, 0, x);
            Spawn(backRank[x], PieceColor.Black, 7, x);
        }
    }

    void Spawn(PieceType type, PieceColor color, int x, int y)
    {
        // Instantiate the piece as a child of the target cell so it automatically sits on the board
        var parentCell = GetCell(x, y);
        if (parentCell == null)
        {
            Debug.LogError($"Spawn: target cell at {x},{y} is null");
            return;
        }

        ChessPiece p = Instantiate(piecePrefab, parentCell.transform);
        p.transform.localPosition = Vector3.zero;
        p.transform.localRotation = Quaternion.identity;
        p.Init(type, color, x, y);
        // assign sprite (assumes ChessSprites exists in scene)
        if (ChessSprites.Instance != null)
            p.SetSprite(ChessSprites.Instance.GetSprite(type, color));
        else
            p.SetSprite(null);

        // store reference on the board model
        // store reference on the board model using the same indexing convention
        if (!transposeBoard)
            board.pieces[x, y] = p;
        else
            board.pieces[y, x] = p;
    }

    ChessCell GetCell(int x, int y)
    {
        if (!transposeBoard)
        {
            if (x < 0 || x >= 8 || y < 0 || y >= 8) return null;
            return board.cells[x, y];
        }

        // swapped indexing
        if (y < 0 || y >= 8 || x < 0 || x >= 8) return null;
        return board.cells[y, x];
    }

    // CLICK Ô
    public void OnCellClicked(ChessCell cell)
    {
        if (!myTurn) return;

        if (selectedPiece == null)
        {
            ChessPiece p = board.pieces[cell.x, cell.y];
            if (p != null && p.color == MyColor)
                selectedPiece = p;

            return;
        }

        TryMove(selectedPiece, cell.x, cell.y);
    }

    void TryMove(ChessPiece p, int x, int y)
    {
        if (!ChessRules.IsLegalMove(p, x, y, board))
            return;

        photonView.RPC(
            "RPC_Move",
            RpcTarget.All,
            p.x, p.y, x, y
        );
    }

    [PunRPC]
    void RPC_Move(int fx, int fy, int tx, int ty)
    {
        ChessPiece p = board.pieces[fx, fy];
        if (p == null) return;

        if (board.pieces[tx, ty] != null)
            Destroy(board.pieces[tx, ty].gameObject);

        board.pieces[fx, fy] = null;
        board.pieces[tx, ty] = p;

        p.SetPosition(tx, ty);
        p.transform.position = board.cells[tx, ty].transform.position;

        selectedPiece = null;
        myTurn = !myTurn;
    }
}
