﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;

public class Player : MonoBehaviour
{
    #region Member Variables
    //Containers
    Dictionary<short, Card> myCards = new Dictionary<short, Card>();
    Dictionary<short, Image> uiCards = new Dictionary<short, Image>();
    List<short> cardsToPlay = new List<short>();

    //UI References
    [SerializeField] private Image imagePrefab = null;
    GameObject handStartPosition = null;
    Button drawCardsButton = null;
    Button playCardsButton = null;
    Image topCardImage = null;
    Canvas hud = null;

    //Game objects
    Deck deck = null;
    PhotonView view = null;
    Camera mainCamera = null;

    //Event Codes
    public const byte PlayCardEventCode = 1;
    #endregion

    #region Start/Stop/Update
    private void Start()
    {
        mainCamera = Camera.main;
        view = GetComponent<PhotonView>();
        hud = GameObject.FindWithTag("Hud").GetComponent<Canvas>();
        deck = GameObject.FindWithTag("Deck").GetComponent<Deck>();

        //Sets all UI element references
        handStartPosition = hud.transform.Find("HandStartPosition").gameObject;
        drawCardsButton = hud.transform.Find("DrawCardsButton").GetComponent<Button>();
        playCardsButton = hud.transform.Find("PlayCardsButton").GetComponent<Button>();
        topCardImage = hud.transform.Find("TopCardImage").GetComponent<Image>();

        drawCardsButton.onClick.AddListener(NetworkDrawCard);
        playCardsButton.onClick.AddListener(TryPlayCard);

        //Subscribe to event
        PhotonNetwork.NetworkingClient.EventReceived += PlayCard;
        UICardHandler.cardSelected += ChangeCardsToPlay;
    }

    private void OnDestroy()
    {
        //Unsubscribe from event
        PhotonNetwork.NetworkingClient.EventReceived -= PlayCard;
        UICardHandler.cardSelected -= ChangeCardsToPlay;
    }
    #endregion

    #region Card Handling

    //Takes a card from the drawdeck and adds it to the players hand
    public void DrawCard()
    {
        Card card = deck.DrawCard();
        myCards.Add(card.GetCardId(), card);
        UpdateCardUI();
    }

    //Recives event to play card, updating deck on all clients
    public void PlayCard(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        if (eventCode == PlayCardEventCode)
        {
            short[] cards = (short[])photonEvent.CustomData;
            deck.PlayCard(cards);
            topCardImage.sprite = deck.GetPlayDeckTopCard().GetCardSprite();
        }
    }

    //Adds or removes cards from play hand when selected
    //Also numbers cards based on the order they will be played
    public void ChangeCardsToPlay(bool isPickup, short cardId)
    {
        if (view.IsMine)
        {
            if (isPickup)
            {
                cardsToPlay.Add(cardId);
            }
            else
            {
                cardsToPlay.Remove(cardId);
                uiCards[cardId].GetComponentInChildren<Text>().text = null;
            }
            foreach (short card in cardsToPlay)
            {
                uiCards[card].GetComponentInChildren<Text>().text = (cardsToPlay.IndexOf(card) + 1).ToString();
            }
        }
    }

    //Used for adding multiple cards for mistakes or tricks
    public void CardMistake(short numCards)
    {
        for (int i = 0; i < numCards; i++)
        {
            NetworkDrawCard();
        }
    }

    //Checks if the cards are valid to play, if true plays card on the network
    public void TryPlayCard()
    {
        if (view.IsMine)
        {
            //Checks if the first card matches the deck ie: can be played
            if (!deck.CheckCardMatch(cardsToPlay[0]))
            {
                //Draw two cards for a mistake
                CardMistake(2);
                cardsToPlay.Clear();
                Debug.Log("Invalid card");
                return;
            }

            NetworkPlayCards();
            UpdateCardUI();
        }
    }
    #endregion

    #region Networking

    //Simulates another player drawing a card. Does not add the card to the players hand
    [PunRPC]
    public void UpdateDrawDeckRpc()
    {
        deck.DrawCard();
    }

    //Sends event to all players to replace the top card with cardId "content"
    private void NetworkPlayCards()
    {
        short[] content = cardsToPlay.ToArray();
        RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(PlayCardEventCode, content, eventOptions, SendOptions.SendReliable);

        //Removes the card from player hand and updates UI to match
        foreach (short cardId in cardsToPlay)
        {
            myCards.Remove(cardId);
        }
        cardsToPlay.Clear();
    }

    private void NetworkDrawCard()
    {
        if (view.IsMine)
        {
            DrawCard();
        }
        else
        {
            view.RPC("UpdateDrawDeckRpc", RpcTarget.Others);
        }
    }
    #endregion

    #region UI

    public void UpdateCardUI()
    {
        int offset = 0;

        //Destroys all cards to accomodate cards being removed
        var cardList = GameObject.FindGameObjectsWithTag("UICard");
        foreach (GameObject uiCard in cardList)
        {
            Destroy(uiCard);
        }

        //Removes all pairs so the dictionary can be re-populated below
        uiCards.Clear();

        foreach (KeyValuePair<short, Card> card in myCards)
        {
            //Button imageInstance = Instantiate(imagePrefab);
            Image imageInstance = Instantiate(imagePrefab);
            imageInstance.transform.SetParent(handStartPosition.transform, false);
            imageInstance.sprite = card.Value.GetCardSprite();
            imageInstance.rectTransform.anchoredPosition += new Vector2(offset, 0);

            uiCards.Add(card.Value.GetCardId(), imageInstance);

            //Offset moves cards over so they aren't rendered on top of each other
            offset += 50;
        }
    }
    #endregion
}
