using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;
using ExitGames.Client.Photon;

public class Deck : MonoBehaviour
{
    private Stack<Card> drawDeck = new Stack<Card>();
    private Stack<Card> playDeck = new Stack<Card>();
    [SerializeField]List<Card> tempDraw = new List<Card>();
    [SerializeField] List<Card> tempPlay = new List<Card>();
    private Dictionary<byte, Card> lookupDeck = new Dictionary<byte, Card>();

    public void Start()
    {
        LoadDeck();
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
        Card card = drawDeck.Pop();
        tempDraw.Remove(card);

        if (drawDeck.Count == 0)
        {
            FlipDeck();
        }

        return card;
    }

    public void PlayCard(byte[] cards)
    {
        foreach (byte cardId in cards)
        {
            playDeck.Push(lookupDeck[cardId]);
            tempPlay.Add(lookupDeck[cardId]);
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
            float yOffset = 0.0f;
            foreach (byte cardId in cards)
            {
                PhotonNetwork.Instantiate("Cards/CardInstances/" + lookupDeck[cardId].name, transform.position + new Vector3(xOffset, yOffset, 0), Quaternion.identity);
                xOffset += 0.05f;
                yOffset += 0.001f;
            }
        }
    }
    #endregion

    #region Deck handling
    //Flip the deck when there are no cards left to draw
    private void FlipDeck()
    {
        Card topCard = playDeck.Pop();
        tempPlay.Remove(topCard);

        while (playDeck.Count > 0)
        {
            Card card = playDeck.Pop();
            tempPlay.Remove(card);
            drawDeck.Push(card);
            tempDraw.Add(card);
        }
        
        playDeck.Push(topCard);
        tempPlay.Add(topCard);
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
    public void Shuffle(UnityEngine.Object[] deck)
    {
        UnityEngine.Object tempGO;

        //Seed ensures all players have the same randomization
        UnityEngine.Random.InitState(10);

        for (int i = 0; i < deck.Length; i++)
        {
            int rnd = UnityEngine.Random.Range(0, deck.Length);
            tempGO = deck[rnd];
            deck[rnd] = deck[i];
            deck[i] = tempGO;
        }
    }

    //Loads all card prefabs from Resources, inserts them into stack and lookup dictionary
    private void LoadDeck()
    {
        UnityEngine.Object[] loadDeck;
        loadDeck = Resources.LoadAll("Cards/CardInstances", typeof(Card));

        //Shuffle the deck
        Shuffle(loadDeck);

        //Push shuffled numbers onto stack
        foreach (Card card in loadDeck)
        {
            //Generic list of type Card
            drawDeck.Push(card);
            lookupDeck.Add(card.GetCardId(), card);
            tempDraw.Add(card);
        }
    }
    #endregion
}
