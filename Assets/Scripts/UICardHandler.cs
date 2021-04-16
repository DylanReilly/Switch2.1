using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICardHandler : MonoBehaviour
{
    bool isSelcted = false;

    public void OnEnter()
    {
        Image image = gameObject.GetComponent<Image>();
        if (!isSelcted)
        {
            image.rectTransform.anchoredPosition += new Vector2(0, 50);
        }
    }

    public void OnExit()
    {
        Image image = gameObject.GetComponent<Image>();
        if (!isSelcted)
        {
            image.rectTransform.anchoredPosition -= new Vector2(0, 50);
        }
    }

    public void OnClick()
    {
        Image image = gameObject.GetComponent<Image>();
        if (isSelcted) 
        { 
            isSelcted = false; 
        }
        else 
        { 
            isSelcted = true; 
        }
    }
}
