using TMPro;
using UnityEngine;

public class XOUIController : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public GameObject rematchButton;

    public void SetStatus(string text)
    {
        statusText.text = text;
    }

    public void ShowRematch(bool show)
    {
        rematchButton.SetActive(show);
    }
}
