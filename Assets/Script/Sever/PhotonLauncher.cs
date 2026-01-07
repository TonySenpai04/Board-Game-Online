using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using ExitGames.Client.Photon;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{
    public static PhotonLauncher Instance;

    [Header("Panels")]
    public GameObject lobbyPanel;
    public GameObject gamePanel;

    [Header("UI")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI roomIdText;
    public TMP_InputField roomIdInput;

    [Header("Game Mode")]
    // Selected mode applied when creating a room. Default: 3x3, win=3
    public int selectedGridSize = 3;
    public int selectedWinLength = 3;

    bool canInteract = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        

        statusText.text = "Connecting to Photon...";
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LogLevel = PunLogLevel.Full;

        PhotonNetwork.ConnectUsingSettings();
    }

    // ================= CONNECT =================

    public override void OnConnectedToMaster()
    {
        statusText.text = "Connected to Master. Joining Lobby...";
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        canInteract = true;
        statusText.text = "Connected ✔";
    }

    // Mode selection buttons (wire these to UI buttons)
    public void OnMode3Click()
    {
        selectedGridSize = 3;
        selectedWinLength = 3;
        if (statusText != null) statusText.text = "Mode: 3x3 (3 in a row)";
    }

    public void OnMode4Click()
    {
        selectedGridSize = 6;
        selectedWinLength = 4;
        if (statusText != null) statusText.text = "Mode: 4x4 (6 in a row)";
    }

    public void OnMode5Click()
    {
        selectedGridSize = 9;
        selectedWinLength = 5;
        if (statusText != null) statusText.text = "Mode: 5x5 (9 in a row)";
    }

    // ================= BUTTONS =================

    // AUTO MATCH
    public void OnFindMatchClick()
    {
        if (!canInteract) return;

        statusText.text = "Finding opponent...";
        // Join a random room that matches the selected mode (gridSize/winLength)
        var expected = new Hashtable
        {
            { "gridSize", selectedGridSize },
            { "winLength", selectedWinLength }
        };
        // expected max players byte set to 2 (match only rooms with space for 2)
        PhotonNetwork.JoinRandomRoom(expected, (byte)2);
    }

    // CREATE ROOM (DEBUG)
    public void OnCreateRoomClick()
    {
        if (!canInteract) return;

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
            { "gridSize", selectedGridSize },
            { "winLength", selectedWinLength }
        };
        options.CustomRoomProperties = props;
        options.CustomRoomPropertiesForLobby = new string[] { "gridSize", "winLength" };

        statusText.text = "Creating room...";
        PhotonNetwork.CreateRoom(roomId, options);
    }

    // JOIN ROOM BY ID
    public void OnJoinRoomByIdClick()
    {
        if (!canInteract) return;

        string roomId = roomIdInput.text.Trim();
        if (string.IsNullOrEmpty(roomId)) return;

        statusText.text = "Joining room " + roomId + "...";
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
            { "gridSize", selectedGridSize },
            { "winLength", selectedWinLength }
        };
        options.CustomRoomProperties = props;
        options.CustomRoomPropertiesForLobby = new string[] { "gridSize", "winLength" };

        PhotonNetwork.CreateRoom(roomId, options);
    }

    // Called when the local client successfully created a room
    public override void OnCreatedRoom()
    {
        Debug.Log("OnCreatedRoom: Room created: " + PhotonNetwork.CurrentRoom.Name);
        if (statusText != null)
            statusText.text = "Created room: " + PhotonNetwork.CurrentRoom.Name;
        // roomIdText is updated on join to ensure server accepted
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"OnCreateRoomFailed: ({returnCode}) {message}");
        if (statusText != null)
            statusText.text = "Create failed: " + message;
    }

    public override void OnJoinedRoom()
    {
        roomIdText.text = "Room ID: " + PhotonNetwork.CurrentRoom.Name;
        statusText.text = "Joined room (" + PhotonNetwork.CurrentRoom.PlayerCount + "/2)";

        // Read room custom properties for mode and configure game manager accordingly
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties != null)
        {
            var rprops = PhotonNetwork.CurrentRoom.CustomProperties;
            int gSize = selectedGridSize;
            int wLen = selectedWinLength;
            if (rprops.ContainsKey("gridSize"))
            {
                try { gSize = System.Convert.ToInt32(rprops["gridSize"]); } catch { }
            }
            if (rprops.ContainsKey("winLength"))
            {
                try { wLen = System.Convert.ToInt32(rprops["winLength"]); } catch { }
            }

            // Apply to XOGameManager (ensure instance exists)
            if (XOGameManager.Instance != null)
            {
                XOGameManager.Instance.ConfigureMode(gSize, wLen);
            }
        }

        TryStartGame();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        statusText.text = "Player joined (" + PhotonNetwork.CurrentRoom.PlayerCount + "/2)";
        TryStartGame();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "Join failed: " + message;
    }

    // ================= GAME START =================

    void TryStartGame()
    {
        bool forceStartLocal = XOGameManager.Instance != null && XOGameManager.Instance.localMode;
        if ((PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount == 2) || forceStartLocal)
        {
            lobbyPanel.SetActive(false);
            gamePanel.SetActive(true);
            statusText.text = forceStartLocal ? "Local Game Started!" : "Game Started!";
            if (XOGameManager.Instance != null)
                XOGameManager.Instance.StartGame();
            else
                Debug.LogWarning("XOGameManager.Instance is null when trying to start game.");
        }
    }
}
