using UnityEngine;
using UnityEngine.UI;

public class ChessCell : MonoBehaviour
{
    public int x;
    public int y;
    public Button button;
    // assign this in the prefab to change square visuals
    public Image background;

    Color originalColor = Color.white;
    Sprite originalSprite = null;

    void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClick);

        if (background != null)
        {
            originalColor = background.color;
            originalSprite = background.sprite;
        }
    }

    void OnClick()
    {
        if (ChessGameManager.Instance == null) return;

        // If there's a piece on this cell, prefer selecting the piece (so clicking piece works even
        // if piece prefab doesn't have its own Button). Otherwise treat it as a cell click (move target).
        var gm = ChessGameManager.Instance;
        if (gm.board != null)
        {
            var p = gm.board.pieces[x, y];
            if (p != null)
            {
                gm.OnPieceClicked(p);
                return;
            }
        }

        gm.OnCellClicked(this);
    }

    public void SetBackground(Sprite s)
    {
        if (background != null)
        {
            originalSprite = s;
            background.sprite = s;
            background.color = originalColor;
        }
    }

    public void SetHighlight(Color c)
    {
        if (background != null)
            background.color = c;
    }

    public void ClearHighlight()
    {
        if (background != null)
        {
            background.color = originalColor;
            background.sprite = originalSprite;
        }
    }
}
