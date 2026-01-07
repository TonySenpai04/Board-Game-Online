using UnityEngine;
using UnityEngine.UI;

public class ChessCell : MonoBehaviour
{
    public int x;
    public int y;
    public Button button;
    // assign this in the prefab to change square visuals
    public Image background;

    void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (ChessGameManager.Instance != null)
            ChessGameManager.Instance.OnCellClicked(this);
    }

    public void SetBackground(Sprite s)
    {
        if (background != null)
            background.sprite = s;
    }
}
