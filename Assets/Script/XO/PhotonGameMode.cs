using Photon.Pun;

public class PhotonGameMode : IGameMode
{
    PhotonView view;

    public PhotonGameMode(PhotonView view)
    {
        this.view = view;
    }

    public bool IsMyTurn(int turn)
    {
        return PhotonNetwork.IsMasterClient && turn == 1
            || !PhotonNetwork.IsMasterClient && turn == 2;
    }

    public int MyPlayer()
    {
        return PhotonNetwork.IsMasterClient ? 1 : 2;
    }

    public void SendMove(int index, int player)
    {
        view.RPC("RPC_MakeMove", RpcTarget.All, index, player);
    }
}
