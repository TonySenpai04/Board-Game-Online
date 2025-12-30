using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

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
        lobbyPanel.SetActive(true);
        gamePanel.SetActive(false);

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

    // ================= BUTTONS =================

    // AUTO MATCH
    public void OnFindMatchClick()
    {
        if (!canInteract) return;

        statusText.text = "Finding opponent...";
        PhotonNetwork.JoinRandomRoom();
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

        statusText.text = "Creating room...";
        PhotonNetwork.CreateRoom(roomId, options);
    // Wait for OnCreatedRoom / OnJoinedRoom to confirm and update the UI
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

        PhotonNetwork.CreateRoom(roomId, options);
    }

    // Called when the local client successfully created a room (but not necessarily joined?)
    public override void OnCreatedRoom()
    {
        Debug.Log("OnCreatedRoom: Room created: " + PhotonNetwork.CurrentRoom.Name);
        if (statusText != null)
            statusText.text = "Created room: " + PhotonNetwork.CurrentRoom.Name;
        // Optionally update room id UI here (safer to do on join)
        if (roomIdText != null && PhotonNetwork.CurrentRoom != null)
            roomIdText.text = "Room ID: " + PhotonNetwork.CurrentRoom.Name;
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
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            lobbyPanel.SetActive(false);
            gamePanel.SetActive(true);

            statusText.text = "Game Started!";
            XOGameManager.Instance.StartGame();
        }
    }
}
