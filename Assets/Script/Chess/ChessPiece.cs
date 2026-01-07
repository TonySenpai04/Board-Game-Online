using UnityEngine;
using UnityEngine.UI;

public class ChessPiece : MonoBehaviour
{
    public PieceType type;
    public PieceColor color;
     public Image image;

    [HideInInspector] public int x;
    [HideInInspector] public int y;

    public void Init(PieceType t, PieceColor c, int px, int py)
    {
        type = t;
        color = c;
        SetPosition(px, py);
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
}
