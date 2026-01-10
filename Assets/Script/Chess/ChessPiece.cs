using UnityEngine;
using UnityEngine.UI;

public class ChessPiece : MonoBehaviour
{
    public PieceType type;
    public PieceColor color;
     public Image image;
    public Button button;

    [HideInInspector] public int x;
    [HideInInspector] public int y;

    public void Init(PieceType t, PieceColor c, int px, int py)
    {
        type = t;
        color = c;
        SetPosition(px, py);
        // wire up piece click if a Button is provided on the piece prefab
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnPieceClicked());
        }
    }
    public void SetSprite(Sprite sprite)
    {
        image.sprite = sprite;
    }

    public void SetPosition(int px, int py)
    {
        x = px;
        y = py;
    }

    void OnPieceClicked()
    {
        if (ChessGameManager.Instance != null)
            ChessGameManager.Instance.OnPieceClicked(this);
    }
}
