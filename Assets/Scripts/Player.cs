﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Linq;

public class Player : MonoBehaviour
{
    #region Member Variables
    //Containers
    Dictionary<short, Card> myCards = new Dictionary<short, Card>();
    Dictionary<short, Image> uiCards = new Dictionary<short, Image>();
    [SerializeField] List<short> cardsToPlay = new List<short>();
    public Stack<Player> players = new Stack<Player>();

    //UI References
    [SerializeField] private Image imagePrefab = null;
    GameObject handStartPosition = null;
    Button drawCardsButton = null;
    Button playCardsButton = null;
    Button gameStartButton = null;
    Button sortCardsButton = null;
    Image topCardImage = null;
    Canvas hud = null;
    int spawnPoint = 0;
    bool gameOn = false;

    //Game objects
    Deck deck = null;
    PhotonView view = null;
    Camera mainCamera = null;
    TurnHandler turnHandler = null;

    //Event Codes
    public const byte PlayCardEventCode = 1;
    public const byte GameStartEventCode = 2;
    public const byte DrawCardEventCode = 3;
    public const byte DrawStartCardsEventCode = 4;
    public const byte DealStartCardsLoopCode = 5;

    public int GetSpawnPoint()
    {
        return spawnPoint;
    }
    #endregion

    #region Start/Stop/Update
    private void Start()
    {
        //Sets game objects
        mainCamera = Camera.main;
        view = GetComponent<PhotonView>();
        hud = GameObject.FindWithTag("Hud").GetComponent<Canvas>();
        deck = GameObject.FindWithTag("Deck").GetComponent<Deck>();
        turnHandler = GameObject.Find("TurnHandler").GetComponent<TurnHandler>();

        //Sets all UI element references
        handStartPosition = hud.transform.Find("HandStartPosition").gameObject;
        drawCardsButton = hud.transform.Find("DrawCardsButton").GetComponent<Button>();
        playCardsButton = hud.transform.Find("PlayCardsButton").GetComponent<Button>();
        sortCardsButton = hud.transform.Find("SortCardsButton").GetComponent<Button>();
        topCardImage = hud.transform.Find("TopCardImage").GetComponent<Image>();

        if (PhotonNetwork.IsMasterClient)
        {
            gameStartButton = hud.transform.Find("GameStartButton").GetComponent<Button>();
            gameStartButton.gameObject.SetActive(true);
            gameStartButton.onClick.AddListener(HostGameStart);
        }
        else
        {
            gameStartButton = hud.transform.Find("GameStartWaitButton").GetComponent<Button>();
            gameStartButton.gameObject.SetActive(true);
        }

        //Adds method calls to UI buttons
        drawCardsButton.onClick.AddListener(NetworkDrawCard);
        playCardsButton.onClick.AddListener(TryPlayCard);
        sortCardsButton.onClick.AddListener(SortHand);


        //Subscribe to event
        PhotonNetwork.NetworkingClient.EventReceived += HandlePhotonEvents;
        UICardHandler.cardSelected += ChangeCardsToPlay;

        FindSpawnPoint();
    }

    private void OnDestroy()
    {
        //Unsubscribe from event
        PhotonNetwork.NetworkingClient.EventReceived -= HandlePhotonEvents;
        UICardHandler.cardSelected -= ChangeCardsToPlay;
    }
    #endregion

    #region Card Handling
    //Recives event to play card, updating deck on all clients
    public void HandlePhotonEvents(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;

        //------Playing Cards
        if (eventCode == PlayCardEventCode)
        {
            if (view.IsMine)
            {
                //Stores the number of each trick card where the number matters
                short[] cards = (short[])photonEvent.CustomData;
                //A count of each trick card played (not including aces as they do not stack)
                //2s = index[0] | 8s = index[1] | Jacks = index[2] | Black Queens = index[3] | Kings of Hearts = index[4]
                byte[] trickCards = new byte[5];
                deck.PlayCard(cards);
                topCardImage.sprite = deck.GetPlayDeckTopCard().GetCardSprite();

                #region Trick Card reading
                //Get a count of each trick card in the cards played
                foreach (short id in cards)
                {
                    Card card = deck.FindCard(id);

                    switch (card.GetValue())
                    {
                        case 2:
                            trickCards[0]++;
                            break;

                        case 8:
                            turnHandler.PlayerUseTurn();
                            trickCards[1]++;
                            break;

                        case 11:
                            turnHandler.ReverseOrder();
                            trickCards[2]++;
                            break;

                        case 12:
                            if (card.GetSuit() == 3 || card.GetSuit() == 4)
                            {
                                trickCards[3]++;
                            }
                            break;

                        case 13:
                            if (card.GetSuit() == 1)
                            {
                                trickCards[4]++;
                            }
                            break;
                    }
                }
                //Only use turn if jacks havnt reversed the order
                if (trickCards[2] % 2 == 0 || trickCards[2] == 0)
                {
                    turnHandler.PlayerUseTurn();
                }
                #endregion


                //Only let the player play cards if it is their turn
                if (turnHandler.GetCurrentPlayer() == view.ViewID)
                {
                    playCardsButton.interactable = true;
                }
                else
                {
                    playCardsButton.interactable = false;
                }
            }
        }

        //------Drawing Cards
        else if (eventCode == DrawCardEventCode)
        {
            if (view.IsMine)
            {
                if ((int)photonEvent.CustomData != view.ViewID)
                {
                    Card card = deck.DrawCard();
                }

                if ((int)photonEvent.CustomData == turnHandler.GetCurrentPlayer())
                {
                    turnHandler.PlayerUseTurn();
                    if (turnHandler.GetCurrentPlayer() == view.ViewID)
                    {
                        playCardsButton.interactable = true;
                    }
                }
            }
        }

        //------Game Start
        else if (photonEvent.Code == GameStartEventCode)
        {
            if (view.IsMine)
            {
                drawCardsButton.interactable = true;
                sortCardsButton.interactable = true;
                gameStartButton.gameObject.SetActive(false);
                turnHandler.AddPlayers();
            }
        }

        //------Game Start Card Dealing
        else if (photonEvent.Code == DrawStartCardsEventCode)
        {
            int eventViewID = (int)photonEvent.CustomData;

            if (eventViewID == view.ViewID && view.IsMine)
            {
                DrawMultipleCards(5);
                DealStartCardsLoop();
            }
        }

        //------Game Start Card Loop
        else if (photonEvent.Code == DealStartCardsLoopCode)
        {
            DealStartCards();
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

    //Used for adding multiple cards for mistakes, tricks or deals
    public void DrawMultipleCards(short numCards)
    {
        if (view.IsMine)
        {
            for (int i = 0; i < numCards; i++)
            {
                NetworkDrawCard();
            }
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
                cardsToPlay.Clear();
                DrawMultipleCards(2);
                return;
            }

            NetworkPlayCards();
        }
    }
    #endregion

    #region Networking
    //Sends event to all players to replace the top card with cardId "content"
    private void NetworkPlayCards()
    {
        short[] content = cardsToPlay.ToArray();
        RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(PlayCardEventCode, content, eventOptions, SendOptions.SendReliable);

        if (view.IsMine)
        {
            //Removes the card from player hand and updates UI to match
            foreach (short cardId in cardsToPlay)
            {
                myCards.Remove(cardId);
            }
            cardsToPlay.Clear();
            UpdateCardUI();
        }
    }

    //Picks up a card and alerts other players that the deck has been modified
    private void NetworkDrawCard()
    {
        if (view.IsMine)
        {
            cardsToPlay.Clear();

            int content = view.ViewID;
            RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(DrawCardEventCode, content, eventOptions, SendOptions.SendReliable);

            Card card = deck.DrawCard();
            myCards.Add(card.GetCardId(), card);
            UpdateCardUI();

            playCardsButton.interactable = false;
        }
    }

    //Enables all players UI, plays the first card on the deck and deals 5 cards to everyone
    public void HostGameStart()
    {
        if (PhotonNetwork.IsMasterClient && view.IsMine)
        {

            byte dummy = 0;
            RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(GameStartEventCode, dummy, eventOptions, SendOptions.SendReliable);

            //Used to play the very first card of the game
            NetworkDrawCard();
            foreach (KeyValuePair<short, Card> card in myCards)
            {
                cardsToPlay.Add(card.Key);
            }
            NetworkPlayCards();

            //Creates a list of players viewID to later deal 5 cards each at start of game
            foreach (Player player in FindObjectsOfType<Player>())
            {
                players.Push(player);
            }
            DealStartCards();
        }
    }

    //Deals 5 cards to every player in the game on game start.
    //Works recursively with HandlePhotonEvents to ensure players take their cards in order
    public void DealStartCards()
    {
        if (PhotonNetwork.IsMasterClient && view.IsMine && players.Count != 0)
        {
            int content = players.Pop().GetComponent<PhotonView>().ViewID;
            RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(DrawStartCardsEventCode, content, eventOptions, SendOptions.SendReliable);
        }
        else if (players.Count == 0)
        {
            gameOn = true;
        }
    }

    //Event is used to recursively call DealStartCards for each player in turn
    public void DealStartCardsLoop()
    {
        byte content = 0;
        RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(DealStartCardsLoopCode, content, eventOptions, SendOptions.SendReliable);
    }

    //Sets the index of the spawn point used by the player. Used to determine turn order
    private void FindSpawnPoint()
    {
        SpawnPlayers spawner = GameObject.FindGameObjectWithTag("PauseMenu").GetComponentInChildren<SpawnPlayers>();
        foreach (SpawnPoint point in spawner.spawnPoints)
        {
            if (gameObject.transform.position == point.transform.position)
            {
                spawnPoint = spawner.spawnPoints.IndexOf(point);
            }
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

    public void SortHand()
    {
        List<KeyValuePair<short, Card>> myList = myCards.ToList();

        myList.Sort(
            delegate (KeyValuePair<short, Card> pair1, KeyValuePair<short, Card> pair2)
            {
                return pair1.Value.GetValue().CompareTo(pair2.Value.GetValue());
            }
        );

        myCards.Clear();
        foreach (KeyValuePair<short, Card> card in myList)
        {
            myCards.Add(card.Key, card.Value);
        }
        UpdateCardUI();
    }
    #endregion
}