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

    public void PlayCard(Card card)
    {
        playDeck.Push(card);
    }

    public Card FindCard(short id)
    {
        return lookupDeck[id];
    }

    public void ReplaceCard(Card card)
    {
        drawDeck.Push(card);
    }

    public void Start()
    {
        LoadDeck();
        PlayCard(DrawCard());

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate("Cards/CardInstances/" + GetPlayDeckTopCard().name, transform.position, Quaternion.identity);
        }
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
            drawDeck.Push(card);
            lookupDeck.Add(card.GetCardId(), card);
        }
    }
    #endregion
}
