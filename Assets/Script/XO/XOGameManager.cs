#region old
// using Photon.Pun;
// using Photon.Realtime;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// [RequireComponent(typeof(PhotonView))]
// public class XOGameManager : MonoBehaviourPunCallbacks
// {
//     public static XOGameManager Instance;

//     [Header("References")]
//     public GameObject cellPrefab; // prefab with XOCell component
//     public Transform gridParent; // parent with GridLayoutGroup
//     public XOCell[] cells;
//     public TextMeshProUGUI statusText;
//     public GameObject rematchButton;

//     [Header("Mode")]
//     public int gridSize = 3; // e.g., 3,4,5
//     public int winLength = 3; // e.g., 3,4,5
//     [Header("Debug")]
//     // If true, run a local two-player mode on a single client (no Photon RPCs).
//     public bool localMode = false;

//     int[] board; // length = gridSize * gridSize
//     int currentTurn = 1;
//     bool gameEnded;
//     int rematchVotes;

//     void Awake()
//     {
//         Instance = this;
//         // Ensure a PhotonView is attached; MonoBehaviourPunCallbacks expects one when using photonView
//         if (photonView == null)
//         {
//             var pv = GetComponent<PhotonView>();
//             if (pv == null)
//                 Debug.LogError("XOGameManager: Missing PhotonView component on this GameObject. Please add a PhotonView.");
//         }
//     }

//     public void StartGame()
//     {
//         SetupBoard(gridSize, winLength);
//     }

//     /// <summary>
//     /// Configure mode before starting game (call before StartGame or change at runtime and call StartGame).
//     /// </summary>
//     public void ConfigureMode(int gridSize, int winLength)
//     {
//         this.gridSize = Mathf.Max(3, gridSize);
//         this.winLength = Mathf.Clamp(winLength, 3, this.gridSize);
//     }

//     void SetupBoard(int size, int winLen)
//     {
//         // clean existing
//         if (gridParent != null)
//         {
//             for (int i = gridParent.childCount - 1; i >= 0; i--)
//                 DestroyImmediate(gridParent.GetChild(i).gameObject);

//             var gridLayout = gridParent.GetComponent<GridLayoutGroup>();
//             if (gridLayout != null)
//             {
//                 // use fixed column count so constraintCount is meaningful
//                 gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
//                 gridLayout.constraintCount = size;

//                 // attempt to calculate a cell size so cells evenly fill the parent rect
//                 var rt = gridParent.GetComponent<RectTransform>();
//                 if (rt != null)
//                 {
//                     // Force layout to update sizes first
//                     Canvas.ForceUpdateCanvases();

//                     float parentWidth = rt.rect.width - gridLayout.padding.left - gridLayout.padding.right;
//                     float parentHeight = rt.rect.height - gridLayout.padding.top - gridLayout.padding.bottom;

//                     float totalSpacingX = gridLayout.spacing.x * (size - 1);
//                     float totalSpacingY = gridLayout.spacing.y * (size - 1);

//                     float cellW = (parentWidth - totalSpacingX) / size;
//                     float cellH = (parentHeight - totalSpacingY) / size;

//                     // choose square cell size that fits
//                     float cellSize = Mathf.Floor(Mathf.Min(cellW, cellH));
//                     if (cellSize < 1) cellSize = 1;

//                     gridLayout.cellSize = new Vector2(cellSize, cellSize);

//                     // Force immediate rebuild so instantiated cells get positioned correctly
//                     LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
//                 }
//             }
//         }

//         gridSize = size;
//         winLength = winLen;

//         int total = gridSize * gridSize;
//         board = new int[total];
//         cells = new XOCell[total];

//         if (cellPrefab == null)
//         {
//             Debug.LogError("XOGameManager: cellPrefab is not assigned.");
//             return;
//         }

//         for (int i = 0; i < total; i++)
//         {
//             var go = Instantiate(cellPrefab, gridParent);
//             var cell = go.GetComponent<XOCell>();
//             if (cell == null)
//             {
//                 Debug.LogError("Cell prefab is missing XOCell component.");
//                 continue;
//             }
//             cell.index = i;
//             // ensure references inside cell
//             if (cell.button == null) cell.button = go.GetComponentInChildren<Button>();
//             if (cell.text == null) cell.text = go.GetComponentInChildren<TextMeshProUGUI>();
//             cells[i] = cell;
//         }

//         ResetGame();
//     }

//     bool IsMyTurn()
//     {
//         if (localMode) return true; // allow local single-machine play (both sides)
//         if (PhotonNetwork.IsMasterClient && currentTurn == 1) return true;
//         if (!PhotonNetwork.IsMasterClient && currentTurn == 2) return true;
//         return false;
//     }

//     int MyPlayer()
//     {
//         return PhotonNetwork.IsMasterClient ? 1 : 2;
//     }

//     public void MakeMove(int index)
//     {
//         if (!IsMyTurn()) return;
//         if (gameEnded) return;
//         if (board == null)
//         {
//             Debug.LogError("XOGameManager.MakeMove: board is null. Did you call StartGame()?");
//             return;
//         }
//         if (index < 0 || index >= board.Length)
//         {
//             Debug.LogError($"XOGameManager.MakeMove: index out of range: {index}");
//             return;
//         }
//         if (board[index] != 0) return;

//         if (localMode)
//         {
//             // Apply move locally for both players (single-client testing)
//             ApplyMove(index, currentTurn);
//             return;
//         }

//         if (photonView == null)
//         {
//             Debug.LogError($"XOGameManager.MakeMove: photonView is null. Cannot send RPC for index {index}. Ensure a PhotonView component is attached to the XOGameManager GameObject.");
//             return;
//         }

//         photonView.RPC("RPC_MakeMove", RpcTarget.All, index, currentTurn);
//     }

//     [PunRPC]
//     void RPC_MakeMove(int index, int player)
//     {
//         // Delegate to shared apply method so localMode can reuse logic
//         ApplyMove(index, player);
//     }

//     // Shared move application used by both RPC and localMode
//     void ApplyMove(int index, int player)
//     {
//         if (board == null) { Debug.LogError("ApplyMove: board is null"); return; }
//         if (cells == null) { Debug.LogError("ApplyMove: cells array is null"); return; }
//         if (index < 0 || index >= board.Length) { Debug.LogError($"ApplyMove: index out of range: {index}"); return; }
//         if (cells[index] == null) { Debug.LogError($"ApplyMove: cells[{index}] is null"); return; }

//         board[index] = player;
//         cells[index].SetValue(player);

//         if (CheckWin(player))
//         {
//             gameEnded = true;
//             if (statusText != null)
//                 statusText.text = player == MyPlayer() ? "YOU WIN 🎉" : "YOU LOSE ❌";
//             if (rematchButton != null) rematchButton.SetActive(true);
//             return;
//         }

//         if (IsDraw())
//         {
//             gameEnded = true;
//             if (statusText != null) statusText.text = "DRAW 🤝";
//             if (rematchButton != null) rematchButton.SetActive(true);
//             return;
//         }

//         currentTurn = currentTurn == 1 ? 2 : 1;
//         if (statusText != null) statusText.text = IsMyTurn() ? "Your Turn" : "Opponent Turn";
//     }

//     bool CheckWin(int p)
//     {
//         int[,] dirs = new int[,] {
//         { 1, 0 },   // →
//         { 0, 1 },   // ↓
//         { 1, 1 },   // ↘
//         { 1, -1 }   // ↗
//     };

//         for (int i = 0; i < board.Length; i++)
//         {
//             if (board[i] != p) continue;

//             int r = i / gridSize;
//             int c = i % gridSize;

//             for (int d = 0; d < 4; d++)
//             {
//                 int count = 1;

//                 count += CountDir(r, c, dirs[d, 0], dirs[d, 1], p);
//                 count += CountDir(r, c, -dirs[d, 0], -dirs[d, 1], p);

//                 if (count >= winLength)
//                     return true;
//             }
//         }
//         return false;
//     }

//     int CountDir(int r, int c, int dr, int dc, int p)
//     {
//         int cnt = 0;

//         for (int step = 1; step < winLength; step++)
//         {
//             int nr = r + dr * step;
//             int nc = c + dc * step;

//             if (nr < 0 || nr >= gridSize || nc < 0 || nc >= gridSize)
//                 break;

//             if (board[nr * gridSize + nc] != p)
//                 break;

//             cnt++;
//         }
//         return cnt;
//     }

//     bool IsDraw()
//     {
//         if (board == null) return false;
//         foreach (var b in board)
//             if (b == 0) return false;
//         return true;
//     }

//     // -------- REMATCH --------
//     public void OnRematchClick()
//     {
//         if (photonView != null)
//             photonView.RPC("RPC_Rematch", RpcTarget.All);
//     }

//     [PunRPC]
//     void RPC_Rematch()
//     {
//         rematchVotes++;
//         if (rematchVotes >= 2)
//             ResetGame();
//     }

//     void ResetGame()
//     {
//         rematchVotes = 0;
//         gameEnded = false;
//         currentTurn = 1;

//         if (board == null) board = new int[gridSize * gridSize];

//         for (int i = 0; i < board.Length; i++)
//         {
//             board[i] = 0;
//             if (cells != null && i < cells.Length && cells[i] != null)
//                 cells[i].ResetCell();
//         }

//         if (rematchButton != null) rematchButton.SetActive(false);
//         if (statusText != null) statusText.text = IsMyTurn() ? "Your Turn" : "Opponent Turn";
//     }

//     public override void OnPlayerLeftRoom(Player other)
//     {
//         if (statusText != null) statusText.text = "Opponent Left 😢";
//         gameEnded = true;
//         if (rematchButton != null) rematchButton.SetActive(false);
//     }
// }
#endregion
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class XOGameManager : MonoBehaviourPunCallbacks
{
    [Header("Config")]
    [SerializeField] int gridSize = 3;
    [SerializeField] int winLength = 3;
    [SerializeField] public bool localMode = false;
    [Header("References")]
    public GameObject cellPrefab; // prefab with XOCell component
    public Transform gridParent; // parent with GridLayoutGroup
    public XOCell[] cells;
    IBoard board;
    IWinChecker winChecker;
    IGameMode gameMode;

    public XOUIController ui;

    [Header("Win Line")]
    // assign a thin UI Image prefab (RectTransform) that will be stretched/rotated to show the winning line
    public GameObject winLinePrefab;
    public float winLineThickness = 8f;
    // optional container to hold the win line so GridLayoutGroup won't re-layout it
    // If null, a sibling container will be created at runtime as a sibling of gridParent.
    public RectTransform winLineContainer;
    GameObject activeWinLine;
    Coroutine winLineAnim;

    int currentTurn = 1;
    bool ended;
    public static XOGameManager Instance;

    void Start()
    {
        StartGame();
    }
    public void CreateBoard(int size)
    {
        Clear();
        var gridLayout = gridParent.GetComponent<GridLayoutGroup>();
        // ===== GRID CONFIG =====
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = size;

        ResizeCells(size);

        // ===== SPAWN CELLS =====
        int total = size * size;
        cells = new XOCell[total];

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
            if (cell.button == null)
                cell.button = go.GetComponentInChildren<Button>();
            if (cell.text == null)
                cell.text = go.GetComponentInChildren<TextMeshProUGUI>();
            cells[i] = cell;
        }


    }
    // =========================
    void ResizeCells(int size)
    {
        RectTransform rt = gridParent as RectTransform;
        var gridLayout = gridParent.GetComponent<GridLayoutGroup>();
        if (rt == null) return;

        Canvas.ForceUpdateCanvases();

        float parentWidth = rt.rect.width - gridLayout.padding.left - gridLayout.padding.right;
        float parentHeight = rt.rect.height - gridLayout.padding.top - gridLayout.padding.bottom;

        float totalSpacingX = gridLayout.spacing.x * (size - 1);
        float totalSpacingY = gridLayout.spacing.y * (size - 1);

        float cellW = (parentWidth - totalSpacingX) / size;
        float cellH = (parentHeight - totalSpacingY) / size;

        float cellSize = Mathf.Floor(Mathf.Min(cellW, cellH));
        if (cellSize < 1) cellSize = 1;

        gridLayout.cellSize = new Vector2(cellSize, cellSize);

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    void Clear()
    {
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(gridParent.GetChild(i).gameObject);
    }
    public void StartGame()
    {
        InitGame();
    }

    void InitGame()
    {
        CreateBoard(gridSize);
        board = new XOBoard(gridSize);
        winChecker = new LineWinChecker(winLength);

        gameMode = localMode
            ? new LocalGameMode()
            : new PhotonGameMode(photonView);

        ResetGame();
    }


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ConfigureMode(int gridSize, int winLength)
    {
        this.gridSize = Mathf.Max(3, gridSize);
        this.winLength = Mathf.Clamp(winLength, 3, this.gridSize);
    }

    public void MakeMove(int index)
    {
        if (ended) return;
        if (!gameMode.IsMyTurn(currentTurn)) return;
        if (!board.IsCellEmpty(index)) return;

        gameMode.SendMove(index, currentTurn);
        photonView.RPC("RPC_MakeMove", RpcTarget.All, index, currentTurn);
    }

    [PunRPC]
    void RPC_MakeMove(int index, int player)
    {
        board.SetCell(index, player);
        cells[index].SetValue(player);

        if (winChecker.CheckWin(board, player))
        {
            ended = true;
            ui.SetStatus(player == gameMode.MyPlayer() ? "YOU WIN" : "YOU LOSE");
            ui.ShowRematch(true);
            // draw animated win line between first and last winning cells
            var winIndices = WinLineFinder.GetWinningLine(board, player, winLength);
            if (winIndices != null && winIndices.Count > 0)
                DrawWinLine(winIndices);
            return;
        }

        currentTurn = currentTurn == 1 ? 2 : 1;
        ui.SetStatus(gameMode.IsMyTurn(currentTurn) ? "Your Turn" : "Opponent Turn");
    }

    void ResetGame()
    {
        ClearWinLine();

        ended = false;
        currentTurn = 1;
        board.Reset();

        foreach (var c in cells)
            c.ResetCell();

        ui.ShowRematch(false);
        ui.SetStatus("Your Turn");
    }

    void DrawWinLine(System.Collections.Generic.List<int> winIndices)
    {
        if (winLinePrefab == null || gridParent == null || cells == null) return;
        if (winIndices == null || winIndices.Count == 0) return;

        int first = winIndices[0];
        int last = winIndices[winIndices.Count - 1];
        if (first < 0 || last < 0 || first >= cells.Length || last >= cells.Length) return;

        var rtA = cells[first].GetComponent<RectTransform>();
        var rtB = cells[last].GetComponent<RectTransform>();
        if (rtA == null || rtB == null) return;

        // world centers
        Vector3 worldA = rtA.TransformPoint(rtA.rect.center);
        Vector3 worldB = rtB.TransformPoint(rtB.rect.center);

        Canvas canvas = gridParent.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        Vector2 screenA = RectTransformUtility.WorldToScreenPoint(cam, worldA);
        Vector2 screenB = RectTransformUtility.WorldToScreenPoint(cam, worldB);

        // use a container that is NOT affected by GridLayoutGroup (so the line won't be re-laid out)
        RectTransform containerRect = winLineContainer != null ? winLineContainer : EnsureWinLineContainer();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, screenA, cam, out Vector2 localA)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, screenB, cam, out Vector2 localB)) return;

        Vector2 dir = localB - localA;
        Vector2 dirNorm = dir.normalized;
        // try to read cell size from GridLayoutGroup so we can start/end outside the cell
        var gridLayout = gridParent != null ? gridParent.GetComponent<GridLayoutGroup>() : null;
        float halfCell = (gridLayout != null && gridLayout.cellSize.x > 0f) ? gridLayout.cellSize.x * 0.5f : Mathf.Max(8f, winLineThickness);

        // start a bit outside the first cell and end a bit outside the last cell
        Vector2 startAtEdge = localA - dirNorm * halfCell;
        Vector2 endAtEdge = localB + dirNorm * halfCell;
        float distance = (endAtEdge - startAtEdge).magnitude;
        float angle = Mathf.Atan2(dirNorm.y, dirNorm.x) * Mathf.Rad2Deg;

        if (activeWinLine == null)
        {
            activeWinLine = Instantiate(winLinePrefab, containerRect);
            var r = activeWinLine.GetComponent<RectTransform>();
            if (r != null)
            {
                // set pivot to left so the line starts at the first cell (anchored at localA)
                r.pivot = new Vector2(0f, 0.5f);
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            }
        }

        var winRt = activeWinLine.GetComponent<RectTransform>();
        if (winRt == null) return;

        // reset
        if (winLineAnim != null) StopCoroutine(winLineAnim);

        // Place the left end at startAtEdge so the line grows from outside the first cell toward outside the last cell
        winRt.sizeDelta = new Vector2(0f, Mathf.Max(1f, winLineThickness));
        winRt.anchoredPosition = startAtEdge;
        winRt.localEulerAngles = new Vector3(0, 0, angle);
        activeWinLine.SetActive(true);

        winLineAnim = StartCoroutine(AnimateWinLine(winRt, distance));
    }

    System.Collections.IEnumerator AnimateWinLine(RectTransform winRt, float targetWidth)
    {
        float t = 0f;
        float duration = 0.25f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float w = Mathf.Lerp(0f, targetWidth, Mathf.SmoothStep(0f, 1f, t));
            winRt.sizeDelta = new Vector2(w, Mathf.Max(1f, winLineThickness));
            yield return null;
        }
        winRt.sizeDelta = new Vector2(targetWidth, Mathf.Max(1f, winLineThickness));
        winLineAnim = null;
    }

    void ClearWinLine()
    {
        if (winLineAnim != null) { StopCoroutine(winLineAnim); winLineAnim = null; }
        if (activeWinLine != null) activeWinLine.SetActive(false);
    }

    RectTransform EnsureWinLineContainer()
    {
        if (winLineContainer != null) return winLineContainer;

        Transform parentT = gridParent != null ? gridParent.parent : null;
        if (parentT == null) parentT = gridParent;

        var go = new GameObject("WinLineContainer", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parentT, false);
        rt.SetAsLastSibling();
        // stretch to cover parent area
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        winLineContainer = rt;
        return rt;
    }
}
