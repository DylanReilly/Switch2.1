using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;

public class Deck : MonoBehaviour
{
    private Stack<Card> drawDeck = new Stack<Card>();
    private Stack<Card> playDeck = new Stack<Card>();
    private Dictionary<short, Card> lookupDeck = new Dictionary<short, Card>();

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
        if (drawDeck.Count == 0)
        {
            FlipDeck();
        }
        return card;
    }

    public void PlayCard(short[] cards)
    {
        foreach(short cardId in cards)
        {
            playDeck.Push(lookupDeck[cardId]);   
        }
        RenderCards(cards);
    }

    public void RenderCards(short[] cards)
    {
        //If you are the host, destroy all cards on the network and add new card
        //Host does this to stop multiple copies of each card from being instantiated
        if (PhotonNetwork.IsMasterClient)
        {
            //Delete cards until there are none left
            if(GameObject.FindGameObjectWithTag("Card") != null)
            {
                do
                {
                    PhotonNetwork.Destroy(GameObject.FindGameObjectWithTag("Card"));
                }
                while (GameObject.FindGameObjectWithTag("Card") != null);
            }

            float xOffset = 0.05f;
            float yOffset = 0.001f;
            foreach (short cardId in cards)
            {
                PhotonNetwork.Instantiate("Cards/CardInstances/" + lookupDeck[cardId].name, transform.position + new Vector3(xOffset, yOffset, 0), Quaternion.identity);
                xOffset += 0.05f;
                yOffset += 0.001f;
            }
        }
    }

    public Card FindCard(short id)
    {
        return lookupDeck[id];
    }

    public void ReplaceCard(Card card)
    {
        drawDeck.Push(card);
    }

    public bool CheckCardMatch(short cardId) 
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

    public void Start()
    {
        LoadDeck();
        short[] startCard = new short[1];
        startCard[0] = DrawCard().GetCardId();
        PlayCard(startCard);
    }

    #region Deck handling
    //Flip the deck when there are no cards left to draw
    private void FlipDeck()
    {
        Card topCard = playDeck.Pop();
        foreach (Card card in playDeck)
        {
            drawDeck.Push(playDeck.Pop());
        }
        playDeck.Push(topCard);
    }

    //Used to shuffle the deck
    public void Shuffle(UnityEngine.Object[] deck)
    {
        UnityEngine.Object tempGO;
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
        //Shuffle(loadDeck);

        //Push shuffled numbers onto stack
        foreach (Card card in loadDeck)
        {
            //Generic list of type Card
            drawDeck.Push(card);
            lookupDeck.Add(card.GetCardId(), card);
        }
    }
    #endregion
}
