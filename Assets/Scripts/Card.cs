using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour, IComparer
{
    [SerializeField] private short value;
    [SerializeField] private int suit;
    [SerializeField] GameObject cardModel;
    [SerializeField] private short id = -1;
    [SerializeField] private Sprite cardSprite;
    private static Deck deck = null;

    #region Getters/Setters
    public short GetCardId()
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

    public void SetCardId(short cardId)
    {
        id = cardId;
    }

    public void SetValue(short cardValue)
    {
        value = cardValue;
    }

    public void SetSuit(int cardSuit)
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

    #region Serialization
    public static Card Deserialize(byte[] data)
    {
        short result = -1;
        using (MemoryStream m = new MemoryStream(data))
        {
            using (BinaryReader reader = new BinaryReader(m))
            {
                result = reader.ReadInt16();
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
