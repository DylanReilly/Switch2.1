using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour, IComparer
{
    [SerializeField] private byte value;
    [SerializeField] private byte suit;
    [SerializeField] GameObject cardModel;
    [SerializeField] private byte id = 0;
    [SerializeField] private Sprite cardSprite;
    private static Deck deck = null;

    #region Getters/Setters
    public byte GetCardId()
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

    public void SetCardId(byte cardId)
    {
        id = cardId;
    }

    public void SetValue(byte cardValue)
    {
        value = cardValue;
    }

    public void SetSuit(byte cardSuit)
    {
        suit = cardSuit;
    }

    public void SetCardSprite(Sprite sprite)
    {
        cardSprite = sprite;
    }

    public void SetCardModel(GameObject model)
    {
        cardModel = model;
    }
    #endregion

    public void Start()
    {
        deck = GameObject.FindWithTag("Deck").GetComponent<Deck>();
    }

    int IComparer.Compare(object x, object y)
    {
        Card current = (Card)x;
        Card other = (Card)y;

        return current.value.CompareTo(other.value);
    }

    public override string ToString()
    {
        string sValue = "";
        string sSuit = "";

        switch (value)
        {
            case 1:
                sValue = "Ace ";
                break;

            case 11:
                sValue = "Jack ";
                break;

            case 12:
                sValue = "Queen ";
                break;

            case 13:
                sValue = "King ";
                break;

            default:
                sValue = value.ToString() + " ";
                break;
        }

        switch(suit)
        {
            case 1:
                sSuit = " Hearts";
                break;

            case 2:
                sSuit = " Diamonds";
                break;

            case 3:
                sSuit = " Clubs";
                break;

            case 4:
                sSuit = " Spades";
                break;
        }

        return sValue + "of" + sSuit + " ";
    }

    #region Serialization
    public static Card Deserialize(byte[] data)
    {
        byte result = 0;
        using (MemoryStream m = new MemoryStream(data))
        {
            using (BinaryReader reader = new BinaryReader(m))
            {
                result = reader.ReadByte();
            }
        }
        return deck.FindCard(result);
    }

    public static byte[] Serialize(object obj)
    {
        //Only serialize the cardId to reduce data travelling over the network
        var card = (Card)obj;
        using (MemoryStream m = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(m))
            {
                writer.Write(card.GetCardId());
            }
            return m.ToArray();
        }
    }
    #endregion
}
