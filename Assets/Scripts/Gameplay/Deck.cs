using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;
using ExitGames.Client.Photon;

public class Deck : MonoBehaviour
{
    private Stack<Card> drawDeck = new Stack<Card>();
    public List<Card> tempDrawDeck = new List<Card>();
    private Stack<Card> playDeck = new Stack<Card>();
    public GameObject drawDeckModel = null;
    private Dictionary<byte, Card> lookupDeck = new Dictionary<byte, Card>();

    public void Start()
    {
        LoadDeck();
        SetDeckSize();
    }

    #region Playing/Drawing

    //Used to play the very first card at the beginning of the game. Called by host on game start
    public void PlayFirstCard()
    {
        byte[] startCard = new byte[1];
        startCard[0] = DrawCard().GetCardId();
        PlayCard(startCard);
    }

    public Card GetDrawDeckTopCard()
    {
        return drawDeck.Peek();
    }

    public Card GetPlayDeckTopCard()
    {
        return playDeck.Peek();
    }

    public Card DrawCard()
    {
        if (drawDeck.Count == 0)
        {
            FlipDeck();
        }

        Card card = drawDeck.Pop();
        tempDrawDeck.Remove(card);
        SetDeckSize();

        return card;
    }

    public void PlayCard(byte[] cards)
    {
        foreach (byte cardId in cards)
        {
            playDeck.Push(lookupDeck[cardId]);
        }
        RenderCards(cards);
    }

    public void ReplaceCard(Card card)
    {
        drawDeck.Push(card);
    }

    public Card FindCard(byte id)
    {
        return lookupDeck[id];
    }

    public void SetAceSuit(byte id, byte suit)
    {
        lookupDeck[id].SetSuit(suit);
    }

    public int GetPlayDeckCount()
    {
        return playDeck.Count;
    }

    public string CheckSuit(byte suit)
    {
        string sSuit = "";

        switch (suit)
        {
            case 1:
                sSuit = "Hearts";
                break;

            case 2:
                sSuit = "Diamonds";
                break;

            case 3:
                sSuit = "Clubs";
                break;

            case 4:
                sSuit = "Spades";
                break;
        }
        return sSuit;
    }
    #endregion

    #region 3D deck handling
    public void RenderCards(byte[] cards)
    {
        //If you are the host, destroy all cards on the network and add new card
        //Host does this to stop multiple copies of each card from being instantiated
        if (PhotonNetwork.IsMasterClient)
        {
            //Delete cards until there are none left
            if(playDeck.Count > 1)
            {
                do
                {
                    PhotonNetwork.Destroy(GameObject.FindGameObjectWithTag("Card"));
                }
                while (GameObject.FindGameObjectWithTag("Card") != null);
            }

            float xOffset = 0.0f;
            float rotationYOffset = 0.0f;
            foreach (byte cardId in cards)
            {
                PhotonNetwork.Instantiate("Cards/CardInstances/" + lookupDeck[cardId].name, transform.position + new Vector3(xOffset, 0.01f, 0), Quaternion.Euler(0, 30 + rotationYOffset, -1));
                xOffset += 0.05f;
                rotationYOffset += 5f;
            }
        }
    }

    private void SetDeckSize()
    {
        if (drawDeck.Count == 0)
        {
            drawDeckModel.transform.localPosition = new Vector3(drawDeckModel.transform.localPosition.x, -1.5f, drawDeckModel.transform.localPosition.z);
        }
        else 
        {
            drawDeckModel.transform.localPosition = new Vector3(drawDeckModel.transform.localPosition.x, ((float)drawDeck.Count / (float)104) - 1f, drawDeckModel.transform.localPosition.z);
        }
    }
    #endregion

    #region Deck handling
    //Flip the deck when there are no cards left to draw
    private void FlipDeck()
    {
        Card topCard = playDeck.Pop();

        while (playDeck.Count > 0)
        {
            Card card = playDeck.Pop();
            drawDeck.Push(card);
        }
        
        playDeck.Push(topCard);
    }

    //Checks the card of cardId to see if it can be played on the deck
    public bool CheckCardMatch(byte cardId)
    {
        Card card = lookupDeck[cardId];
        //Checks if suits value
        if (card.GetValue() == GetPlayDeckTopCard().GetValue())
        {
            return true;
        }
        //Checks is suits match
        else if (card.GetSuit() == GetPlayDeckTopCard().GetSuit())
        {
            return true;
        }
        //Check if card is ace, as ace can be played on anything
        else if (card.GetValue() == 1)
        {
            return true;
        }
        //If none, return false
        else
        {
            return false;
        }
    }

    //Used to shuffle the deck
    public void Shuffle(byte seed)
    {
        Card tempCard;
        Card[] tempDeck = drawDeck.ToArray();
        drawDeck.Clear();
        tempDrawDeck.Clear();

        //Seed ensures all players have the same randomization
        UnityEngine.Random.InitState(seed);

        for (int i = 0; i < tempDeck.Length; i++)
        {
            int rnd = UnityEngine.Random.Range(0, tempDeck.Length);
            tempCard = tempDeck[rnd];
            tempDeck[rnd] = tempDeck[i];
            tempDeck[i] = tempCard;
        }

        for(int i = 0; i < tempDeck.Length; i++)
        {
            drawDeck.Push(tempDeck[i]);
            tempDrawDeck.Add(tempDeck[i]);
        }
    }

    //Loads all card prefabs from Resources, inserts them into stack and lookup dictionary
    private void LoadDeck()
    {
        UnityEngine.Object[] loadDeck;
        loadDeck = Resources.LoadAll("Cards/CardInstances", typeof(Card));

        //Shuffle the deck
        //Shuffle(loadDeck);

        //Push shuffled numbers onto stack
        foreach (Card card in loadDeck)
        {
            //Generic list of type Card
            drawDeck.Push(card);
            tempDrawDeck.Add(card);
            lookupDeck.Add(card.GetCardId(), card);
        }
    }
    #endregion
}
