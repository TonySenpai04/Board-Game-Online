using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using ExitGames.Client.Photon;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{
    public static PhotonLauncher Instance;

    [Header("Panels XO")]
    public GameObject lobbyPanel;
    public GameObject gamePanel;

    [Header("Roots")]
    public GameObject xoRoot;
    public GameObject chessRoot;

    [Header("UI XO")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI roomIdText;
    public TMP_InputField roomIdInput;
     [Header("Panels Chess")]
    public GameObject lobbyPanelChess;
    public GameObject gamePanelChess;

    [Header("UI Chess")]
    public TextMeshProUGUI statusTextChess;
    public TextMeshProUGUI roomIdTextChess;
    public TMP_InputField roomIdInputChess;


    // Supported game modes
    public enum GameMode { XO, Chess }

    [Header("Game Mode")]
    // Selected game mode (default XO)
    public GameMode selectedGameMode = GameMode.XO;

    // Selected XO grid settings applied when creating a room. Default: 3x3, win=3
    public int selectedGridSize = 3;
    public int selectedWinLength = 3;

    bool canInteract = false;
    bool isMatching = false;
    System.Action onConnectedAction;

    // Helpers to route UI between XO and Chess
    void SetStatus(string s)
    {
        if (selectedGameMode == GameMode.Chess)
        {
            if (statusTextChess != null) statusTextChess.text = s;
        }
        else
        {
            if (statusText != null) statusText.text = s;
        }
    }

    void SetRoomIdText(string s)
    {
        if (selectedGameMode == GameMode.Chess)
        {
            if (roomIdTextChess != null) roomIdTextChess.text = s;
        }
        else
        {
            if (roomIdText != null) roomIdText.text = s;
        }
    }

    TMP_InputField GetRoomIdInput()
    {
        return selectedGameMode == GameMode.Chess ? roomIdInputChess : roomIdInput;
    }

    GameObject GetLobbyPanel()
    {
        return selectedGameMode == GameMode.Chess ? lobbyPanelChess : lobbyPanel;
    }

    GameObject GetGamePanel()
    {
        return selectedGameMode == GameMode.Chess ? gamePanelChess : gamePanel;
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LogLevel = PunLogLevel.Full;

        PhotonNetwork.ConnectUsingSettings();
    }

    // ================= CONNECT =================

    public override void OnConnectedToMaster()
    {

        canInteract = true;
        // Execute queued action if any
        if (onConnectedAction != null)
        {
            var action = onConnectedAction;
            onConnectedAction = null;
            action.Invoke();
        }
        else
        {
           
        }
    }

    public override void OnJoinedLobby()
    {
        // Not used anymore
    }

    // Mode selection buttons (wire these to UI buttons)
    public void OnMode3Click()
    {
        selectedGridSize = 3;
        selectedWinLength = 3;
        SetStatus("Mode: 3x3 (3 in a row)");
    }

    public void OnMode4Click()
    {
        selectedGridSize = 6;
        selectedWinLength = 4;
        SetStatus("Mode: 6x6 (4 in a row)");
    }

    // Single button handler to select the top-level game mode.
    // Use Button.OnClick with an Int parameter: 0 => XO, 1 => Chess
    public void OnSelectGameMode(int modeIndex)
    {
        if (modeIndex == 0)
            selectedGameMode = GameMode.XO;
        else if (modeIndex == 1)
            selectedGameMode = GameMode.Chess;

     string gameName = selectedGameMode == GameMode.XO ? "Tic Tac Toe" : selectedGameMode.ToString();
        SetStatus( gameName);

        if (xoRoot != null) xoRoot.SetActive(selectedGameMode == GameMode.XO);
        if (chessRoot != null) chessRoot.SetActive(selectedGameMode == GameMode.Chess);
    }

    public void OnMode5Click()
    {
        selectedGridSize = 9;
        selectedWinLength = 5;
        SetStatus("Mode: 9x9 (5 in a row)");
    }

    // ================= BUTTONS =================

    // AUTO MATCH
    public void OnFindMatchClick()
    {
        if (onConnectedAction != null) return; // Already waiting for connection action
        if (PhotonNetwork.InRoom)
        {
            SetRoomIdText("");
            onConnectedAction = OnFindMatchClick;
            PhotonNetwork.LeaveRoom();
            return;
        }
        if (PhotonNetwork.NetworkClientState == ClientState.JoiningLobby || 
            PhotonNetwork.NetworkClientState == ClientState.Joining || 
            PhotonNetwork.NetworkClientState == ClientState.Authenticating) return;

        isMatching = true;
        if (!canInteract)
        {
 
            onConnectedAction = OnFindMatchClick;
            return;
        }

        SetStatus("Finding opponent...");
        // Join a random room that matches the selected mode (filter by gameMode and XO params)
        var expected = new Hashtable();
        expected["gameMode"] = selectedGameMode.ToString();
        expected["isMatchmaking"] = true; // Only join rooms intended for matchmaking
        if (selectedGameMode == GameMode.XO)
        {
            expected["gridSize"] = selectedGridSize;
            expected["winLength"] = selectedWinLength;
        }
        // expected max players byte set to 2 (match only rooms with space for 2)
        PhotonNetwork.JoinRandomRoom(expected, (byte)2);
    }

    // CREATE ROOM (DEBUG)
    public void OnCreateRoomClick()
    {
        if (onConnectedAction != null) return;
        if (PhotonNetwork.InRoom)
        {
            SetRoomIdText("");
            onConnectedAction = OnCreateRoomClick;
            PhotonNetwork.LeaveRoom();
            return;
        }
        if (PhotonNetwork.NetworkClientState == ClientState.JoiningLobby || 
            PhotonNetwork.NetworkClientState == ClientState.Joining || 
            PhotonNetwork.NetworkClientState == ClientState.Authenticating) return;

        isMatching = false;
        if (!canInteract)
        {
            onConnectedAction = OnCreateRoomClick;
            return;
        }

        string roomId = Random.Range(100000, 999999).ToString();

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };

        // Attach selected mode to room custom properties so joins use the same board
        var props = new Hashtable
        {
            { "gameMode", selectedGameMode.ToString() },
            { "isMatchmaking", false } // Private rooms are not for matchmaking
        };
        if (selectedGameMode == GameMode.XO)
        {
            props["gridSize"] = selectedGridSize;
            props["winLength"] = selectedWinLength;
        }
        options.CustomRoomProperties = props;
        if (selectedGameMode == GameMode.XO)
            options.CustomRoomPropertiesForLobby = new string[] { "gameMode", "gridSize", "winLength", "isMatchmaking" };
        else
            options.CustomRoomPropertiesForLobby = new string[] { "gameMode", "isMatchmaking" };

        SetStatus("Creating room...");
        PhotonNetwork.CreateRoom(roomId, options);
    }

    // JOIN ROOM BY ID
    public void OnJoinRoomByIdClick()
    {
        if (onConnectedAction != null) return;
        if (PhotonNetwork.InRoom)
        {
            SetRoomIdText("");
            onConnectedAction = OnJoinRoomByIdClick;
            PhotonNetwork.LeaveRoom();
            return;
        }
        if (PhotonNetwork.NetworkClientState == ClientState.JoiningLobby || 
            PhotonNetwork.NetworkClientState == ClientState.Joining || 
            PhotonNetwork.NetworkClientState == ClientState.Authenticating) return;

        isMatching = false;
        if (!canInteract)
        {
            onConnectedAction = OnJoinRoomByIdClick;
            return;
        }

        string roomId = GetRoomIdInput().text.Trim();
        if (string.IsNullOrEmpty(roomId)) return;
        SetStatus("Joining room " + roomId + "...");
        PhotonNetwork.JoinRoom(roomId);
    }

    // ================= ROOM CALLBACKS =================

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // Auto match fail → create room
        string roomId = Random.Range(100000, 999999).ToString();

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };

        // use currently selected mode for auto-created room
        var props = new Hashtable
        {
            { "gameMode", selectedGameMode.ToString() },
            { "isMatchmaking", true } // Matchmaking rooms are flagged for others to find
        };
        if (selectedGameMode == GameMode.XO)
        {
            props["gridSize"] = selectedGridSize;
            props["winLength"] = selectedWinLength;
        }
        options.CustomRoomProperties = props;
        if (selectedGameMode == GameMode.XO)
            options.CustomRoomPropertiesForLobby = new string[] { "gameMode", "gridSize", "winLength", "isMatchmaking" };
        else
            options.CustomRoomPropertiesForLobby = new string[] { "gameMode", "isMatchmaking" };

        PhotonNetwork.CreateRoom(roomId, options);
    }

    // Called when the local client successfully created a room
    public override void OnCreatedRoom()
    {
        Debug.Log("OnCreatedRoom: Room created: " + PhotonNetwork.CurrentRoom.Name);
        if (!isMatching)
            SetStatus("Created room: " + PhotonNetwork.CurrentRoom.Name);
        // roomIdText is updated on join to ensure server accepted
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"OnCreateRoomFailed: ({returnCode}) {message}");
        SetStatus("Create failed: " + message);
    }

    public override void OnJoinedRoom()
    {
        SetRoomIdText("Room ID: " + PhotonNetwork.CurrentRoom.Name);
        if (isMatching && PhotonNetwork.CurrentRoom.PlayerCount < 2)
            SetStatus("Searching for opponent...");
        else
            SetStatus("Joined room (" + PhotonNetwork.CurrentRoom.PlayerCount + "/2)");

        // Read room custom properties for mode and configure game manager accordingly
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties != null)
        {
            var rprops = PhotonNetwork.CurrentRoom.CustomProperties;
            int gSize = selectedGridSize;
            int wLen = selectedWinLength;
            string gmStr = selectedGameMode.ToString();
            if (rprops.ContainsKey("gameMode"))
            {
                try { gmStr = (string)rprops["gameMode"]; } catch { }
            }
            if (rprops.ContainsKey("gridSize"))
            {
                try { gSize = System.Convert.ToInt32(rprops["gridSize"]); } catch { }
            }
            if (rprops.ContainsKey("winLength"))
            {
                try { wLen = System.Convert.ToInt32(rprops["winLength"]); } catch { }
            }

            // Apply to the appropriate game manager
            if (gmStr == GameMode.XO.ToString())
            {
                selectedGameMode = GameMode.XO;
                if (XOGameManager.Instance != null)
                    XOGameManager.Instance.ConfigureMode(gSize, wLen);
            }
            else if (gmStr == GameMode.Chess.ToString())
            {
                selectedGameMode = GameMode.Chess;
                // ChessGameManager will be started when TryStartGame triggers start
                if (ChessGameManager.Instance != null)
                {
                }
            }
        }

        TryStartGame();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SetStatus("Player joined (" + PhotonNetwork.CurrentRoom.PlayerCount + "/2)");
        TryStartGame();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetStatus("Join failed: " + message);
    }

    // ================= GAME START =================

    void TryStartGame()
    {
        // Determine game mode from room props (fallback to selectedGameMode)
        string gmStr = selectedGameMode.ToString();
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("gameMode"))
        {
            try { gmStr = (string)PhotonNetwork.CurrentRoom.CustomProperties["gameMode"]; } catch { }
        }

        bool forceStartLocal = (XOGameManager.Instance != null && XOGameManager.Instance.localMode) || false;
        if ((PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount == 2) || forceStartLocal)
        {
            isMatching = false;
            GetLobbyPanel().SetActive(false);
            GetGamePanel().SetActive(true);
            SetStatus(forceStartLocal ? "Local Game Started!" : "Game Started!");

            if (gmStr == GameMode.XO.ToString())
            {
                if (XOGameManager.Instance != null)
                    XOGameManager.Instance.StartGame();
                else
                    Debug.LogWarning("XOGameManager.Instance is null when trying to start XO game.");
            }
            else if (gmStr == GameMode.Chess.ToString())
            {
                if (ChessGameManager.Instance != null)
                    ChessGameManager.Instance.StartGame();
                else
                    Debug.LogWarning("ChessGameManager.Instance is null when trying to start Chess game.");
            }
            else
            {
                Debug.LogWarning("Unknown game mode when trying to start game: " + gmStr);
            }
        }
    }
    public override void OnLeftRoom()
    {
        isMatching = false;
        SetRoomIdText("");
        SetStatus("Left room.");

        // Only toggle panels for the active game root
        if (xoRoot != null && xoRoot.activeInHierarchy)
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            if (gamePanel != null) gamePanel.SetActive(false);
        }
        else if (chessRoot != null && chessRoot.activeInHierarchy)
        {
            if (lobbyPanelChess != null) lobbyPanelChess.SetActive(true);
            if (gamePanelChess != null) gamePanelChess.SetActive(false);
        }
    }

    // New function to exit back to mode selection
    public void OnExitToModeSelection()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }
}
