using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(PhotonView))]
public class XOGameManager : MonoBehaviourPunCallbacks
{
    public static XOGameManager Instance;

    public XOCell[] cells;
    public TextMeshProUGUI statusText;
    public GameObject rematchButton;

    int[] board = new int[9]; // 0 empty | 1 X | 2 O
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
    ResetGame();
    }


    bool IsMyTurn()
    {
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
        if (board[index] != 0) return;

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
        board[index] = player;
        cells[index].SetValue(player);

        if (CheckWin(player))
        {
            gameEnded = true;
            statusText.text = player == MyPlayer() ? "YOU WIN 🎉" : "YOU LOSE ❌";
            rematchButton.SetActive(true);
            return;
        }

        if (IsDraw())
        {
            gameEnded = true;
            statusText.text = "DRAW 🤝";
            rematchButton.SetActive(true);
            return;
        }

        currentTurn = currentTurn == 1 ? 2 : 1;
        statusText.text = IsMyTurn() ? "Your Turn" : "Opponent Turn";
    }

    bool CheckWin(int p)
    {
        int[,] w = {
            {0,1,2},{3,4,5},{6,7,8},
            {0,3,6},{1,4,7},{2,5,8},
            {0,4,8},{2,4,6}
        };

        for (int i = 0; i < 8; i++)
            if (board[w[i,0]] == p &&
                board[w[i,1]] == p &&
                board[w[i,2]] == p)
                return true;

        return false;
    }

    bool IsDraw()
    {
        foreach (var b in board)
            if (b == 0) return false;
        return true;
    }

    // -------- REMATCH --------
    public void OnRematchClick()
    {
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

        for (int i = 0; i < board.Length; i++)
        {
            board[i] = 0;
            cells[i].ResetCell();
        }

        rematchButton.SetActive(false);
        statusText.text = IsMyTurn() ? "Your Turn" : "Opponent Turn";
    }

    public override void OnPlayerLeftRoom(Player other)
    {
        statusText.text = "Opponent Left 😢";
        gameEnded = true;
        rematchButton.SetActive(false);
    }
}
