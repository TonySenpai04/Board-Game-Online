using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
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
    // If true, run in local test mode where this single client controls both sides (no network RPCs)
    public bool localMode = false;

    ChessPiece selectedPiece;
    bool myTurn;
    // UI: show turn/check status
    public TMPro.TextMeshProUGUI statusText;
    bool gameEnded = false;
    // highlighted cells for the currently selected piece
    System.Collections.Generic.List<ChessCell> highlightedCells = new System.Collections.Generic.List<ChessCell>();

    [Header("Promotion UI")]
    public ChessPromotionUI promotionUI;
    bool waitingForPromotion = false;
    ChessPiece pieceToPromote; // temporary storage while waiting for UI selection

    // 50-move rule counter
   [SerializeField] int halfMoveClock = 0;

    void Awake()
    {
        Instance = this;
        // ensure a PhotonView exists so RPCs work
        if (GetComponent<PhotonView>() == null)
        {
            Debug.LogError("ChessGameManager: PhotonView missing on this GameObject. RPCs will fail!");
        }
    }

    void Start()
    {
        // Board may be created here in a single-player scene. Networked games call StartGame() from the launcher.
        // Keep Start logic minimal; StartGame will be safe-guarded against double-creation.
        myTurn = localMode ? true : PhotonNetwork.IsMasterClient; // white đi trước; localMode allows both sides
    }

    // Safe start method called by PhotonLauncher when the room is ready.
    public void StartGame()
    {
        if (board == null)
        {
            Debug.LogError("StartGame: board is null");
            return;
        }

        // Create board cells if not already created
        bool needCreate = false;
        try { needCreate = board.cells[0, 0] == null; } catch { needCreate = true; }
        if (needCreate)
            CreateBoard();

        // Spawn pieces if not already present
        bool needSpawn = false;
        try { needSpawn = board.pieces[0, 0] == null; } catch { needSpawn = true; }
        if (needSpawn)
            SpawnPieces();

        myTurn = PhotonNetwork.IsMasterClient;
        gameEnded = false;
        // set initial status
        if (statusText != null)
        {
            string init = myTurn ? "Your Turn (White)" : "Opponent Turn (White)";
            SetStatusText(init);
        }
       
    }

    public PieceColor MyColor =>
        PhotonNetwork.IsMasterClient ? PieceColor.White : PieceColor.Black;

    // Helper to access board pieces respecting transposeBoard flag
    ChessPiece GetPieceAt(int x, int y)
    {
        if (board == null) return null;
        // ALWAYS use logical coordinates for storage access
        if (x < 0 || x >= 8 || y < 0 || y >= 8) return null;
        return board.pieces[x, y];
    }

    void SetPieceAt(int x, int y, ChessPiece p)
    {
        if (board == null) return;
        // ALWAYS use logical coordinates for storage access
        if (x < 0 || x >= 8 || y < 0 || y >= 8) return;
        board.pieces[x, y] = p;
    }

    void CreateBoard()
    {
        // Instantiate cells row-major (y rows, x columns) so visual layout is horizontal
        int total = 64;
        for (int i = 0; i < total; i++)
        {
            int x = i % 8;
            int y = i / 8;

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

    void SpawnPieces()
    {
        // Pawns
        for (int x = 0; x < 8; x++)
        {
            // White pawns start at rank 6 (from top=0), black pawns at rank 1
            Spawn(PieceType.Pawn, PieceColor.White, x, 6);
            Spawn(PieceType.Pawn, PieceColor.Black, x, 1);
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
            // White back rank at 7 (bottom), black back rank at 0 (top)
            Spawn(backRank[x], PieceColor.White, x, 7);
            Spawn(backRank[x], PieceColor.Black, x, 0);
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
        // ALWAYS store in logical coordinates
        board.pieces[x, y] = p;
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
        // In localMode the single client controls both sides; otherwise only act on your turn
        if (!myTurn && !localMode) return;
        if (waitingForPromotion) return; // Block input while promoting
        // Only attempt a move to this cell if we have a selected piece
        if (selectedPiece == null) return;

        ClearHighlights();

        // Map visual cell coordinates back to logical coordinates
        int targetX = cell.x;
        int targetY = cell.y;

        if (transposeBoard)
        {
            // If transposed, visual X is logical Y, visual Y is logical X
            targetX = cell.y;
            targetY = cell.x;
        }

        TryMove(selectedPiece, targetX, targetY);
    }

    // Called when a piece UI is clicked. Select/deselect or change selection.
    public void OnPieceClicked(ChessPiece piece)
    {
        if (gameEnded) return;
        if (!myTurn && !localMode ) return;
        if (waitingForPromotion) return; // Block input while promoting
        if (piece == null) return;

        // Check color permission
        bool isMyPiece = false;
        if (localMode)
        {
            PieceColor turnColor = myTurn ? PieceColor.White : PieceColor.Black;
            isMyPiece = (piece.color == turnColor);
        }
        else
        {
            isMyPiece = (piece.color == MyColor);
        }

        if (!isMyPiece)
        {
            // If we have a selected piece, clicking an opponent piece might be a capture attempt
            if (selectedPiece != null)
            {
                // Try to capture
                TryMove(selectedPiece, piece.x, piece.y);
            }
            return;
        }

        // If already selected, deselect
        if (selectedPiece == piece)
        {
            selectedPiece = null;
            ClearHighlights();
            return;
        }

        // select new piece
        selectedPiece = piece;
        Debug.Log($"Selected piece: {piece.type} at {piece.x},{piece.y}");
        ClearHighlights();
        ShowLegalMoves(selectedPiece);
    }

    void ShowLegalMoves(ChessPiece p)
    {
        if (p == null) return;
        highlightedCells.Clear();
        for (int tx = 0; tx < 8; tx++)
        {
            for (int ty = 0; ty < 8; ty++)
            {
                if (!ChessRules.IsLegalMove(p, tx, ty, board)) continue;

                // simulate to ensure move doesn't leave own king in check
                var captured = board.pieces[tx, ty];
                int oldX = p.x, oldY = p.y;
                board.pieces[oldX, oldY] = null;
                board.pieces[tx, ty] = p;
                p.x = tx; p.y = ty;

                bool leavesInCheck = ChessRules.IsInCheck(board, p.color);

                // undo
                p.x = oldX; p.y = oldY;
                board.pieces[oldX, oldY] = p;
                board.pieces[tx, ty] = captured;

                if (leavesInCheck) continue;

                var cell = GetCell(tx, ty);
                if (cell != null)
                {
                    cell.SetHighlight(Color.red);
                    highlightedCells.Add(cell);
                }
            }
        }
    }

    void ClearHighlights()
    {
        if (highlightedCells == null) return;
        foreach (var c in highlightedCells)
            if (c != null) c.ClearHighlight();
        highlightedCells.Clear();
    }

    void TryMove(ChessPiece p, int x, int y)
    {
        if (!ChessRules.IsLegalMove(p, x, y, board))
            return;

        // Prevent moves that would leave own king in check
        var captured = board.pieces[x, y];
        int oldX = p.x, oldY = p.y;
        board.pieces[oldX, oldY] = null;
        board.pieces[x, y] = p;
        p.x = x; p.y = y;

        bool leavesInCheck = ChessRules.IsInCheck(board, p.color);

        // undo simulation
        p.x = oldX; p.y = oldY;
        board.pieces[oldX, oldY] = p;
        board.pieces[x, y] = captured;

        if (leavesInCheck) return;

        if (localMode)
        {
            // Apply move locally for both sides (single-client testing)
            RPC_Move(p.x, p.y, x, y);
            return;
        }

        if (photonView == null)
        {
            Debug.LogError("TryMove: photonView is null. Cannot send RPC.");
            return;
        }

        photonView.RPC(
            "RPC_Move",
            RpcTarget.All,
            p.x, p.y, x, y
        );
    }

    [PunRPC]
    void RPC_Move(int fx, int fy, int tx, int ty)
    {
        if (gameEnded) return;

        ChessPiece p = board.pieces[fx, fy];
        if (p == null) return;

        // 50-move rule logic
        bool isPawn = p.type == PieceType.Pawn;
        bool isCapture = board.pieces[tx, ty] != null;

        if (isPawn || isCapture)
            halfMoveClock = 0;
        else
            halfMoveClock++;

        Debug.Log($"HalfMoveClock: {halfMoveClock}");

        // capture
        if (board.pieces[tx, ty] != null)
        {
            Destroy(board.pieces[tx, ty].gameObject);
        }

        board.pieces[fx, fy] = null;
        board.pieces[tx, ty] = p;

        p.SetPosition(tx, ty);
        
        // Update visual position
        // GetCell handles the transposeBoard logic to find the correct visual cell
        var targetCell = GetCell(tx, ty);
        if (targetCell != null)
        {
            // Reparent to the new cell to ensure correct rendering order (on top of the cell)
            p.transform.SetParent(targetCell.transform);
            p.transform.localPosition = Vector3.zero;
        }

        // Check for Pawn Promotion
        // White pawns reach y=0, Black pawns reach y=7 (logical coords)
        // Wait, let's check the board orientation.
        // White starts at 6, moves -1 -> reaches 0.
        // Black starts at 1, moves +1 -> reaches 7.
        // bool isPawn = p.type == PieceType.Pawn; // Already defined above
        bool reachedEnd = (p.color == PieceColor.White && ty == 0) || (p.color == PieceColor.Black && ty == 7);

        if (isPawn && reachedEnd)
        {
            // If it's my turn (or local mode), show UI
            // Note: RPC_Move is called on all clients. We only want the owner to see the UI.
            // But wait, RPC_Move is called AFTER the move is validated and sent.
            // So if I am the owner of this piece, I should see the UI.
            
            bool isMyPiece = (localMode && ((myTurn && p.color == PieceColor.White) || (!myTurn && p.color == PieceColor.Black))) 
                             || (!localMode && p.color == MyColor);

            if (isMyPiece)
            {
                waitingForPromotion = true;
                pieceToPromote = p;
                if (promotionUI != null) promotionUI.Show();
            }
            
            // Do NOT switch turn yet. Wait for promotion selection.
            ClearHighlights();
            return;
        }

        // After move, determine check / checkmate
        CheckGameState(p.color);
        
        // clear any highlights after move
        ClearHighlights();
    }

    void CheckGameState(PieceColor moverColor)
    {
        PieceColor opponent = moverColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

        bool opponentInCheck = ChessRules.IsInCheck(board, opponent);
        bool opponentHasMoves = ChessRules.HasAnyLegalMoves(board, opponent);

        if (!opponentHasMoves && opponentInCheck)
        {
            // checkmate - mover wins
            gameEnded = true;
            SetStatusText(moverColor.ToString() + " wins by checkmate!");
        }
        else if (!opponentHasMoves && !opponentInCheck)
        {
            // stalemate
            gameEnded = true;
            SetStatusText("Stalemate");
        }
        else if (ChessRules.IsInsufficientMaterial(board))
        {
            gameEnded = true;
            SetStatusText("Draw (Insufficient Material)");
        }
        else if (halfMoveClock >= 100)
        {
            gameEnded = true;
            SetStatusText("Draw (50-Move Rule)");
        }
        else
        {
            // normal case: toggle turn
            selectedPiece = null;
            myTurn = !myTurn;
            string turnStr = myTurn ? (MyColor == PieceColor.White ? "Your Turn (White)" : "Your Turn (Black)") : (MyColor == PieceColor.White ? "Opponent Turn (Black)" : "Opponent Turn (White)");
            if (opponentInCheck)
                turnStr += " - Check!";
            SetStatusText(turnStr);
        }
    }

    public void PromotePawn(PieceType newType)
    {
        if (!waitingForPromotion || pieceToPromote == null) return;

        // Send RPC to replace piece
        if (localMode)
        {
            RPC_Promote(pieceToPromote.x, pieceToPromote.y, (int)newType);
        }
        else
        {
            photonView.RPC("RPC_Promote", RpcTarget.All, pieceToPromote.x, pieceToPromote.y, (int)newType);
        }

        // Hide UI
        if (promotionUI != null) promotionUI.Hide();
        waitingForPromotion = false;
        pieceToPromote = null;
    }

    [PunRPC]
    void RPC_Promote(int x, int y, int typeInt)
    {
        ChessPiece p = board.pieces[x, y];
        if (p == null) return;

        PieceType newType = (PieceType)typeInt;
        p.type = newType;
        
        // Update sprite
        if (ChessSprites.Instance != null)
            p.SetSprite(ChessSprites.Instance.GetSprite(newType, p.color));

        // Now switch turn
        CheckGameState(p.color);
    }

    void SetStatusText(string s)
    {
        if (statusText != null)
            statusText.text = s;
        else
            Debug.Log(s);
    }

    // =================================================================================================
    // SURRENDER / RESIGN
    // =================================================================================================

    public void Surrender()
    {
        if (gameEnded) return;

        // Local player surrenders
        if (localMode)
        {
            // In local mode, we just say the current turn player surrendered? 
            // Or if it's a single player testing both sides, maybe just end game.
            // Let's assume the "current turn" player surrenders, or just White if it's ambiguous.
            // Better: pass the color of who clicked the button. 
            // But usually the button is for "Me".
            // In local mode, let's just say "White" surrenders if it's White's turn, etc?
            // Actually, let's just use MyColor logic.
            RPC_Surrender((int)PieceColor.White); // Just for testing
        }
        else
        {
            photonView.RPC("RPC_Surrender", RpcTarget.All, (int)MyColor);
        }
    }

    [PunRPC]
    void RPC_Surrender(int colorInt)
    {
        if (gameEnded) return;

        PieceColor surrenderColor = (PieceColor)colorInt;
        gameEnded = true;
        
        string winner = (surrenderColor == PieceColor.White) ? "Black" : "White";
        SetStatusText($"{surrenderColor} Surrendered. {winner} Wins!");
    }

    // =================================================================================================
    // DRAW OFFER
    // =================================================================================================

    [Header("Draw UI")]
    public GameObject drawOfferUI; // Assign a Panel that has "Accept" and "Decline" buttons

    public void OfferDraw()
    {
        if (gameEnded) return;

        if (localMode)
        {
            Debug.Log("Draw Offered (Local Mode)");
            // In local mode, maybe just show the UI immediately to test it?
            if (drawOfferUI != null) drawOfferUI.SetActive(true);
        }
        else
        {
            photonView.RPC("RPC_OfferDraw", RpcTarget.All, (int)MyColor);
        }
    }

    [PunRPC]
    void RPC_OfferDraw(int colorInt)
    {
        if (gameEnded) return;

        PieceColor offerColor = (PieceColor)colorInt;
        
        // If I am NOT the one who offered, show the UI
        if (!localMode && offerColor != MyColor)
        {
            if (drawOfferUI != null) drawOfferUI.SetActive(true);
            SetStatusText($"Opponent ({offerColor}) offers a Draw.");
        }
        else if (localMode)
        {
             // Local mode testing
             SetStatusText($"Player ({offerColor}) offers a Draw.");
        }
    }

    public void AcceptDraw()
    {
        if (gameEnded) return;

        // Hide UI
        if (drawOfferUI != null) drawOfferUI.SetActive(false);

        if (localMode)
        {
            RPC_DrawAccepted();
        }
        else
        {
            photonView.RPC("RPC_DrawAccepted", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_DrawAccepted()
    {
        if (gameEnded) return;

        gameEnded = true;
        if (drawOfferUI != null) drawOfferUI.SetActive(false);
        SetStatusText("Draw Agreed.");
    }

    public void DeclineDraw()
    {
        if (gameEnded) return;

        // Hide UI
        if (drawOfferUI != null) drawOfferUI.SetActive(false);

        if (localMode)
        {
            RPC_DrawDeclined();
        }
        else
        {
            photonView.RPC("RPC_DrawDeclined", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_DrawDeclined()
    {
        if (gameEnded) return; // Should not happen usually

        // Just notify
        SetStatusText("Draw Offer Declined. Game Continues.");
    }
     public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }



    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (gameEnded) return;
        gameEnded = true;
        SetStatusText("YOU WIN (Opponent Left)");
        if (drawOfferUI != null) drawOfferUI.SetActive(false);
        if (promotionUI != null) promotionUI.Hide();
    }
}
