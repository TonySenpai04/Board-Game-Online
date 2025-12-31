using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(PhotonView))]
public class XOGameManager : MonoBehaviourPunCallbacks
{
    public static XOGameManager Instance;

    [Header("References")]
    public GameObject cellPrefab; // prefab with XOCell component
    public Transform gridParent; // parent with GridLayoutGroup
    public XOCell[] cells;
    public TextMeshProUGUI statusText;
    public GameObject rematchButton;

    [Header("Mode")]
    public int gridSize = 3; // e.g., 3,4,5
    public int winLength = 3; // e.g., 3,4,5
    [Header("Debug")]
    // If true, run a local two-player mode on a single client (no Photon RPCs).
    public bool localMode = false;

    int[] board; // length = gridSize * gridSize
    int currentTurn = 1;
    bool gameEnded;
    int rematchVotes;

    void Awake()
    {
        Instance = this;
        // Ensure a PhotonView is attached; MonoBehaviourPunCallbacks expects one when using photonView
        if (photonView == null)
        {
            var pv = GetComponent<PhotonView>();
            if (pv == null)
                Debug.LogError("XOGameManager: Missing PhotonView component on this GameObject. Please add a PhotonView.");
        }
    }

    public void StartGame()
    {
        SetupBoard(gridSize, winLength);
    }

    /// <summary>
    /// Configure mode before starting game (call before StartGame or change at runtime and call StartGame).
    /// </summary>
    public void ConfigureMode(int gridSize, int winLength)
    {
        this.gridSize = Mathf.Max(3, gridSize);
        this.winLength = Mathf.Clamp(winLength, 3, this.gridSize);
    }

    void SetupBoard(int size, int winLen)
    {
        // clean existing
        if (gridParent != null)
        {
            for (int i = gridParent.childCount - 1; i >= 0; i--)
                DestroyImmediate(gridParent.GetChild(i).gameObject);

            var gridLayout = gridParent.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                // use fixed column count so constraintCount is meaningful
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = size;

                // attempt to calculate a cell size so cells evenly fill the parent rect
                var rt = gridParent.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Force layout to update sizes first
                    Canvas.ForceUpdateCanvases();

                    float parentWidth = rt.rect.width - gridLayout.padding.left - gridLayout.padding.right;
                    float parentHeight = rt.rect.height - gridLayout.padding.top - gridLayout.padding.bottom;

                    float totalSpacingX = gridLayout.spacing.x * (size - 1);
                    float totalSpacingY = gridLayout.spacing.y * (size - 1);

                    float cellW = (parentWidth - totalSpacingX) / size;
                    float cellH = (parentHeight - totalSpacingY) / size;

                    // choose square cell size that fits
                    float cellSize = Mathf.Floor(Mathf.Min(cellW, cellH));
                    if (cellSize < 1) cellSize = 1;

                    gridLayout.cellSize = new Vector2(cellSize, cellSize);

                    // Force immediate rebuild so instantiated cells get positioned correctly
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                }
            }
        }

        gridSize = size;
        winLength = winLen;

        int total = gridSize * gridSize;
        board = new int[total];
        cells = new XOCell[total];

        if (cellPrefab == null)
        {
            Debug.LogError("XOGameManager: cellPrefab is not assigned.");
            return;
        }

        for (int i = 0; i < total; i++)
        {
            var go = Instantiate(cellPrefab, gridParent);
            var cell = go.GetComponent<XOCell>();
            if (cell == null)
            {
                Debug.LogError("Cell prefab is missing XOCell component.");
                continue;
            }
            cell.index = i;
            // ensure references inside cell
            if (cell.button == null) cell.button = go.GetComponentInChildren<Button>();
            if (cell.text == null) cell.text = go.GetComponentInChildren<TextMeshProUGUI>();
            cells[i] = cell;
        }

        ResetGame();
    }

    bool IsMyTurn()
    {
        if (localMode) return true; // allow local single-machine play (both sides)
        if (PhotonNetwork.IsMasterClient && currentTurn == 1) return true;
        if (!PhotonNetwork.IsMasterClient && currentTurn == 2) return true;
        return false;
    }

    int MyPlayer()
    {
        return PhotonNetwork.IsMasterClient ? 1 : 2;
    }

    public void MakeMove(int index)
    {
        if (!IsMyTurn()) return;
        if (gameEnded) return;
        if (board == null)
        {
            Debug.LogError("XOGameManager.MakeMove: board is null. Did you call StartGame()?");
            return;
        }
        if (index < 0 || index >= board.Length)
        {
            Debug.LogError($"XOGameManager.MakeMove: index out of range: {index}");
            return;
        }
        if (board[index] != 0) return;

        if (localMode)
        {
            // Apply move locally for both players (single-client testing)
            ApplyMove(index, currentTurn);
            return;
        }

        if (photonView == null)
        {
            Debug.LogError($"XOGameManager.MakeMove: photonView is null. Cannot send RPC for index {index}. Ensure a PhotonView component is attached to the XOGameManager GameObject.");
            return;
        }

        photonView.RPC("RPC_MakeMove", RpcTarget.All, index, currentTurn);
    }

    [PunRPC]
    void RPC_MakeMove(int index, int player)
    {
        // Delegate to shared apply method so localMode can reuse logic
        ApplyMove(index, player);
    }

    // Shared move application used by both RPC and localMode
    void ApplyMove(int index, int player)
    {
        if (board == null) { Debug.LogError("ApplyMove: board is null"); return; }
        if (cells == null) { Debug.LogError("ApplyMove: cells array is null"); return; }
        if (index < 0 || index >= board.Length) { Debug.LogError($"ApplyMove: index out of range: {index}"); return; }
        if (cells[index] == null) { Debug.LogError($"ApplyMove: cells[{index}] is null"); return; }

        board[index] = player;
        cells[index].SetValue(player);

        if (CheckWin(player))
        {
            gameEnded = true;
            if (statusText != null)
                statusText.text = player == MyPlayer() ? "YOU WIN 🎉" : "YOU LOSE ❌";
            if (rematchButton != null) rematchButton.SetActive(true);
            return;
        }

        if (IsDraw())
        {
            gameEnded = true;
            if (statusText != null) statusText.text = "DRAW 🤝";
            if (rematchButton != null) rematchButton.SetActive(true);
            return;
        }

        currentTurn = currentTurn == 1 ? 2 : 1;
        if (statusText != null) statusText.text = IsMyTurn() ? "Your Turn" : "Opponent Turn";
    }

    bool CheckWin(int p)
    {
        int[,] dirs = new int[,] {
        { 1, 0 },   // →
        { 0, 1 },   // ↓
        { 1, 1 },   // ↘
        { 1, -1 }   // ↗
    };

        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] != p) continue;

            int r = i / gridSize;
            int c = i % gridSize;

            for (int d = 0; d < 4; d++)
            {
                int count = 1;

                count += CountDir(r, c, dirs[d, 0], dirs[d, 1], p);
                count += CountDir(r, c, -dirs[d, 0], -dirs[d, 1], p);

                if (count >= winLength)
                    return true;
            }
        }
        return false;
    }

    int CountDir(int r, int c, int dr, int dc, int p)
    {
        int cnt = 0;

        for (int step = 1; step < winLength; step++)
        {
            int nr = r + dr * step;
            int nc = c + dc * step;

            if (nr < 0 || nr >= gridSize || nc < 0 || nc >= gridSize)
                break;

            if (board[nr * gridSize + nc] != p)
                break;

            cnt++;
        }
        return cnt;
    }

    bool IsDraw()
    {
        if (board == null) return false;
        foreach (var b in board)
            if (b == 0) return false;
        return true;
    }

    // -------- REMATCH --------
    public void OnRematchClick()
    {
        if (photonView != null)
            photonView.RPC("RPC_Rematch", RpcTarget.All);
    }

    [PunRPC]
    void RPC_Rematch()
    {
        rematchVotes++;
        if (rematchVotes >= 2)
            ResetGame();
    }

    void ResetGame()
    {
        rematchVotes = 0;
        gameEnded = false;
        currentTurn = 1;

        if (board == null) board = new int[gridSize * gridSize];

        for (int i = 0; i < board.Length; i++)
        {
            board[i] = 0;
            if (cells != null && i < cells.Length && cells[i] != null)
                cells[i].ResetCell();
        }

        if (rematchButton != null) rematchButton.SetActive(false);
        if (statusText != null) statusText.text = IsMyTurn() ? "Your Turn" : "Opponent Turn";
    }

    public override void OnPlayerLeftRoom(Player other)
    {
        if (statusText != null) statusText.text = "Opponent Left 😢";
        gameEnded = true;
        if (rematchButton != null) rematchButton.SetActive(false);
    }
}
