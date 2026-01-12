using UnityEngine;
using UnityEngine.UI;

public class ChessPromotionUI : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject uiPanel;

    // Assign these in Inspector to the buttons
    public void SelectQueen()
    {
        ChessGameManager.Instance.PromotePawn(PieceType.Queen);
        Hide();
    }

    public void SelectRook()
    {
        ChessGameManager.Instance.PromotePawn(PieceType.Rook);
        Hide();
    }

    public void SelectBishop()
    {
        ChessGameManager.Instance.PromotePawn(PieceType.Bishop);
        Hide();
    }

    public void SelectKnight()
    {
        ChessGameManager.Instance.PromotePawn(PieceType.Knight);
        Hide();
    }

    public void Show()
    {
        if (uiPanel != null) uiPanel.SetActive(true);
    }

    public void Hide()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
    }
}
