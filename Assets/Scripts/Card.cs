using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    [SerializeField] private int value;
    [SerializeField] private int suit;
    [SerializeField] GameObject cardModel;
    [SerializeField] private int id = -1;
    [SerializeField] private Sprite cardSprite;

    public int GetCardId()
    {
        return id;
    }

    public int GetValue()
    {
        return value;
    }

    public int GetSuit()
    {
        return suit;
    }

    public Sprite GetCardSprite()
    {
        return cardSprite;
    }
}
