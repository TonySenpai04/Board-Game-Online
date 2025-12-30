using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class XOCell : MonoBehaviour
{
    public int index;
    public Button button;
    public TextMeshProUGUI text;

    public void Start()
    {
        button.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        XOGameManager.Instance.MakeMove(index);
    }

    public void SetValue(int value)
    {
        text.text = value == 1 ? "X" : "O";
        button.interactable = false;
    }

    public void ResetCell()
    {
        text.text = "";
        button.interactable = true;
    }
}
