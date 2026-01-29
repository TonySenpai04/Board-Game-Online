using TMPro;
using UnityEngine;

public class XOUIController : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject drawPanel;

    public void SetStatus(string text)
    {
        statusText.text = text;
    }

    public void ShowWin()
    {
        if (winPanel != null) winPanel.SetActive(true);
        if (losePanel != null) losePanel.SetActive(false);
    }

    public void ShowLose()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(true);
        if (drawPanel != null) drawPanel.SetActive(false);
    }

    public void ShowDraw()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (drawPanel != null) drawPanel.SetActive(true);
    }

    public void HideEndPanels()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (drawPanel != null) drawPanel.SetActive(false);
    }
}
