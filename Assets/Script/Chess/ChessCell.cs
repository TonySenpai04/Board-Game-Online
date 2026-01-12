using UnityEngine;
using UnityEngine.UI;

public class ChessCell : MonoBehaviour
{
    public int x;
    public int y;
    public Button button;
    // assign this in the prefab to change square visuals
    public Image background;
    public Image highlight;


    void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClick);

        if (highlight != null)
        {
            highlight.gameObject.SetActive(false);
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
            background.sprite = s;
        }
    }

    public void SetHighlight(Color c)
    {
        if (highlight != null)
            highlight.gameObject.SetActive(true);
    }

    public void ClearHighlight()
    {
        if (highlight != null)
            highlight.gameObject.SetActive(false);
    }
}
