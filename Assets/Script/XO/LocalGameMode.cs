public class LocalGameMode : IGameMode
{
    public bool IsMyTurn(int currentTurn) => true;
    public int MyPlayer() => 1;
    public void SendMove(int index, int player) { }
}
public interface IGameMode
{
    bool IsMyTurn(int currentTurn);
    int MyPlayer();
    void SendMove(int index, int player);
}
