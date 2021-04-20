using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Linq;
using System.Collections;

public class Player : MonoBehaviour
{
    #region Member Variables
    //Containers
    Dictionary<byte, Card> myCards = new Dictionary<byte, Card>();
    Dictionary<byte, Image> uiCards = new Dictionary<byte, Image>();
    [SerializeField] List<byte> cardsToPlay = new List<byte>();
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

    //Ace selection handlers
    GameObject aceSuitSelection = null;
    Button chooseHearts = null;
    Button chooseDiamonds = null;
    Button chooseSpades = null;
    Button chooseClubs = null;

    //Game objects
    Deck deck = null;
    PhotonView view = null;
    Camera mainCamera = null;
    TurnHandler turnHandler = null;

    byte twoCount = 0;
    byte kingCount = 0;

    //Event Codes
    public const byte PlayCardEventCode = 1;
    public const byte GameStartEventCode = 2;
    public const byte DrawCardEventCode = 3;
    public const byte DrawStartCardsEventCode = 4;
    public const byte DealStartCardsLoopCode = 5;
    public const byte ResetTrickCount = 6;

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
            Debug.Log("Start button");
            gameStartButton.gameObject.SetActive(true);
            gameStartButton.onClick.AddListener(HostGameStart);
        }
        else
        {
            gameStartButton = hud.transform.Find("GameStartWaitButton").GetComponent<Button>();
            gameStartButton.gameObject.SetActive(true);
        }

        //Sets ace selection buttons
        aceSuitSelection = hud.transform.Find("AceSuitSelection").gameObject;
        //Sets reference to each button
        chooseHearts = aceSuitSelection.transform.Find("HeartsButton").GetComponent<Button>();
        chooseDiamonds = aceSuitSelection.transform.Find("DiamondsButton").GetComponent<Button>();
        chooseClubs = aceSuitSelection.transform.Find("ClubsButton").GetComponent<Button>();
        chooseSpades = aceSuitSelection.transform.Find("SpadesButton").GetComponent<Button>();

        //Sets method calls for each button
        chooseHearts.onClick.AddListener(delegate { SetAceSuitRPC(deck.GetPlayDeckTopCard().GetCardId(), 1); });
        chooseDiamonds.onClick.AddListener(delegate { SetAceSuitRPC(deck.GetPlayDeckTopCard().GetCardId(), 2); });
        chooseClubs.onClick.AddListener(delegate { SetAceSuitRPC(deck.GetPlayDeckTopCard().GetCardId(), 3); });
        chooseSpades.onClick.AddListener(delegate { SetAceSuitRPC(deck.GetPlayDeckTopCard().GetCardId(), 4); });


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
                byte[] cards = (byte[])photonEvent.CustomData;

                //A count of each trick card played
                //Aces = index[0] 2s = index[2] | 8s = index[2] | Jacks = index[3] | Black Queens = index[4] | Kings of Hearts = index[5]
                byte[] trickCards = new byte[6];

                #region Trick Card reading
                //Get a count of each trick card in the cards played
                foreach (byte id in cards)
                {
                    Card card = deck.FindCard(id);

                    switch (card.GetValue())
                    {
                        //Ace
                        case 1:
                            trickCards[0]++;
                            break;

                        //Twos
                        case 2:
                            trickCards[1]++;
                            break;

                        //Eights
                        case 8:
                            turnHandler.PlayerUseTurn();
                            trickCards[2]++;
                            break;

                        //Jacks
                        case 11:
                            turnHandler.ReverseOrder();
                            trickCards[3]++;
                            break;

                        //Black Queens
                        case 12:
                            if (card.GetSuit() == 3 || card.GetSuit() == 4)
                            {
                                trickCards[4]++;
                            }
                            break;

                        //King of Hearts
                        case 13:
                            if (card.GetSuit() == 1)
                            {
                                trickCards[5]++;
                            }
                            break;
                    }
                }

                twoCount += trickCards[1];
                kingCount += trickCards[5];

                //Let player set ace value
                if (trickCards[0] > 0 && turnHandler.GetCurrentPlayer() == view.ViewID)
                {
                    aceSuitSelection.GetComponent<CanvasGroup>().alpha = 1;
                }

                //Only use turn if jacks havn't reversed the order
                if (trickCards[3] % 2 == 0 || trickCards[3] == 0)
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

                deck.PlayCard(cards);
                topCardImage.sprite = deck.GetPlayDeckTopCard().GetCardSprite();
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

        else if (photonEvent.Code == ResetTrickCount)
        {
            twoCount = 0;
            kingCount = 0;
        }
    }

    //Adds or removes cards from play hand when selected
    //Also numbers cards based on the order they will be played
    public void ChangeCardsToPlay(bool isPickup, byte cardId)
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
            foreach (byte card in cardsToPlay)
            {
                uiCards[card].GetComponentInChildren<Text>().text = (cardsToPlay.IndexOf(card) + 1).ToString();
            }
        }
    }

    [PunRPC]
    public void SetAceSuit(byte[] content)
    {
        deck.SetAceSuit(content[0], content[1]);
        aceSuitSelection.GetComponent<CanvasGroup>().alpha = 0;
    }

    public void SetAceSuitRPC(byte id, byte suit)
    {
        byte[] content = new byte[2];
        content[0] = id;
        content[1] = suit;
        view.RPC("SetAceSuit", RpcTarget.All, content);
    }

    //Used for adding multiple cards for mistakes, tricks or deals
    public void DrawMultipleCards(byte numCards)
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
            if (deck.GetPlayDeckTopCard().GetValue() == 2 && twoCount > 0)
            {                
                if (deck.FindCard(cardsToPlay[0]).GetValue() != 2)
                {
                    int numCards = twoCount * 2;

                    RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                    PhotonNetwork.RaiseEvent(ResetTrickCount, 0, eventOptions, SendOptions.SendReliable);

                    numCards += 2; 
                    cardsToPlay.Clear();
                    DrawMultipleCards((byte)numCards);
                    return;
                }
            }

            if (deck.GetPlayDeckTopCard().GetValue() == 13 && deck.GetPlayDeckTopCard().GetSuit() == 1 && kingCount > 0)
            {
                if (deck.FindCard(cardsToPlay[0]).GetValue() != 5 || deck.FindCard(cardsToPlay[0]).GetSuit() != 1)
                {
                    int numCards = kingCount * 5;

                    RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                    PhotonNetwork.RaiseEvent(ResetTrickCount, 0, eventOptions, SendOptions.SendReliable);

                    //numCards ;
                    cardsToPlay.Clear();
                    DrawMultipleCards((byte)numCards);
                    return;
                }
            }

            //Checks if the first card matches the deck ie: can be played
            if (!deck.CheckCardMatch(cardsToPlay[0]))
            {
                //RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                //PhotonNetwork.RaiseEvent(ResetTrickCardList, 0, eventOptions, SendOptions.SendReliable);

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
        byte[] content = cardsToPlay.ToArray();
        RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(PlayCardEventCode, content, eventOptions, SendOptions.SendReliable);

        if (view.IsMine)
        {
            //Removes the card from player hand and updates UI to match
            foreach (byte cardId in cardsToPlay)
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
            foreach (KeyValuePair<byte, Card> card in myCards)
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

        foreach (KeyValuePair<byte, Card> card in myCards)
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
        if (view.IsMine)
        {
            List<KeyValuePair<byte, Card>> myList = myCards.ToList();

            myList.Sort(
                delegate (KeyValuePair<byte, Card> pair1, KeyValuePair<byte, Card> pair2)
                {
                    return pair1.Value.GetValue().CompareTo(pair2.Value.GetValue());
                }
            );

            myCards.Clear();
            foreach (KeyValuePair<byte, Card> card in myList)
            {
                myCards.Add(card.Key, card.Value);
            }
            UpdateCardUI();
        }
    }
    #endregion
}