using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class XOCell : MonoBehaviour
{
    public int index;
    public Button button;
    public TextMeshProUGUI text;
    [Header("Sprites")]
    // Optional: use image sprites instead of text. If assigned, image takes precedence.
    public Image iconImage;
    public Image backgroundImage;
    public Sprite xSprite;
    public Sprite oSprite;

    void Start()
    {
        // Ensure button listener is set (safe if already wired in prefab)
        if (button == null)
            button = GetComponentInChildren<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (iconImage == null)
        {
            // Try to find an image that is NOT the background image
            var images = GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img != backgroundImage)
                {
                    iconImage = img;
                    break;
                }
            }
        }

        // disable icon initially if present (and it's not the background!)
        if (iconImage != null && iconImage != backgroundImage)
            iconImage.enabled = false;
    }

    public void OnClick()
    {
        if (XOGameManager.Instance != null)
            XOGameManager.Instance.MakeMove(index);
    }

    public void SetValue(int value)
    {
        // Prefer sprite icon if available
        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>();

        if (iconImage != null && (xSprite != null || oSprite != null))
        {
            if (value == 1 && xSprite != null)
                iconImage.sprite = xSprite;
            else if (value == 2 && oSprite != null)
                iconImage.sprite = oSprite;
            else
                iconImage.sprite = null;

            iconImage.enabled = iconImage.sprite != null;
        }
        else
        {
            // fallback to text if no sprite assigned
            if (text == null)
                text = GetComponentInChildren<TextMeshProUGUI>();

            if (text != null)
            {
                if (value == 1)
                    text.text = "X";
                else if (value == 2)
                    text.text = "O";
                else
                    text.text = "";
            }
        }

        // Keep button interactable so it can still be clicked (visual feedback) 
        // but logic in GameManager prevents changing the value.
        // if (button != null) button.interactable = false;
    }

    public void ResetCell()
    {
        if (text != null) text.text = "";
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        if (button != null) button.interactable = true;
    }
}
