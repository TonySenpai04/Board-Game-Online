using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpenLinkButton : MonoBehaviour
{
    public Button button; 
    public string url ; 

    void Start()
    {
        button= GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OpenLink);
        }
    }


    void OpenLink()
    {
        Application.OpenURL(url);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
