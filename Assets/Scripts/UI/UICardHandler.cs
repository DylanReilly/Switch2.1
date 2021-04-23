using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICardHandler : MonoBehaviour
{
    public bool isSelected = false;
    public static event Action<bool, byte> cardSelected;

    //Raise card whe hovering over the UI
    public void OnEnter()
    {
        Image image = gameObject.GetComponent<Image>();
        if (!isSelected)
        {
            image.rectTransform.anchoredPosition += new Vector2(0, 50);
        }
    }

    //Drop card when not hovering
    public void OnExit()
    {
        Image image = gameObject.GetComponent<Image>();
        if (!isSelected)
        {
            image.rectTransform.anchoredPosition -= new Vector2(0, 50);
        }
    }

    //Permenantly raise card when clicked, pass event to player to select the card to play
    public void OnClick()
    {   
        Image image = gameObject.GetComponent<Image>();
        byte numSize = 1;
        if(image.sprite.name.Length > 14)
        {
            numSize = 2;
        }
        byte imageId = byte.Parse(image.sprite.name.Substring(13, numSize));

        if (isSelected) 
        {
            isSelected = false;
            cardSelected?.Invoke(false, imageId);
        }
        else 
        { 
            isSelected = true;
            cardSelected?.Invoke(true, imageId);
        }
    }
}
