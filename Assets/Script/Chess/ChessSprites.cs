using UnityEngine;


[System.Serializable]
public class PieceSprite
{
    public PieceType type;
    public PieceColor color;
    public Sprite sprite;
}

public class ChessSprites : MonoBehaviour
{
    public static ChessSprites Instance;
    public PieceSprite[] sprites;

    void Awake()
    {
        Instance = this;
    }

    public Sprite GetSprite(PieceType type, PieceColor color)
    {
        foreach (var s in sprites)
        {
            if (s.type == type && s.color == color)
                return s.sprite;
        }
        return null;
    }
}