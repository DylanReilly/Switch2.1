using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Linq;
using System.Collections;
using System.IO;
using System;
using Cinemachine;
using Assets.SimpleAndroidNotifications;

public class Player : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    #region Member Variables
    //Containers
    public Dictionary<byte, Card> myCards = new Dictionary<byte, Card>();
    public Dictionary<byte, Image> uiCards = new Dictionary<byte, Image>();
    List<byte> cardsToPlay = new List<byte>();
    public Stack<Player> players = new Stack<Player>();

    //UI References
    public HudHandler playerHud = null;
    Button gameStartButton = null;
    int spawnPoint = 0;

    //Game objects
    public Deck deck = null;
    PhotonView view = null;
    public CinemachineVirtualCamera virtualCamera = null;
    TurnHandler turnHandler = null;

    byte twoCount = 0;
    byte kingCount = 0;
    bool hasKnocked = false;
    string firstWinner = null;

    //Event Codes
    public const byte PlayCardEventCode = 1;
    public const byte GameStartEventCode = 2;
    public const byte DrawCardEventCode = 3;
    public const byte DrawStartCardsEventCode = 4;
    public const byte DealStartCardsLoopCode = 5;
    public const byte ResetTrickCount = 6;
    public const byte UpdateGameLogEventCode = 7;
    public const byte GameOverEventCode = 8;
    public const byte SetFirstWinnerEventCode = 9;
    public const byte BumgameEventCode = 10;

    public int GetSpawnPoint()
    {
        return spawnPoint;
    }
    #endregion

    #region Start/Stop/Update
    private void Start()
    {
        //Sets game objects
        view = GetComponent<PhotonView>();
        deck = GameObject.FindWithTag("Deck").GetComponent<Deck>();
        turnHandler = GameObject.Find("TurnHandler").GetComponent<TurnHandler>();

        //Instantiate local hud
        if (view.IsMine)
        {
            playerHud = Instantiate(playerHud);
        }

        //Sets game on button for host, sets waiting screen for clients
        if (PhotonNetwork.IsMasterClient && view.IsMine)
        {
            gameStartButton = playerHud.transform.Find("GameStartButton").GetComponent<Button>();
            gameStartButton.gameObject.SetActive(true);
            gameStartButton.onClick.AddListener(HostGameStart);
        }
        else if(view.IsMine)
        {
            gameStartButton = playerHud.transform.Find("GameStartWaitButton").GetComponent<Button>();
            gameStartButton.gameObject.SetActive(true);
        }

        //Subscribe to events
        playerHud.drawCardsEvent += NetworkDrawCard;
        playerHud.playCardsEvent += TryPlayCard;
        playerHud.believeEvent += BlindPlayCard;
        PhotonNetwork.NetworkingClient.EventReceived += HandlePhotonEvents;
        UICardHandler.cardSelected += ChangeCardsToPlay;
        deck.BumGame += BumGame;

        FindSpawnPoint();
    }

    private void OnDestroy()
    {
        //Unsubscribe from event
        PhotonNetwork.NetworkingClient.EventReceived -= HandlePhotonEvents;
        UICardHandler.cardSelected -= ChangeCardsToPlay;
    }
    #endregion

    #region Network Overrides
    //Diconnects players when the host leaves, returning them to the Main Menu
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("Scene_MainMenu");
    }

    //Sets the gameobject on instantiation
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        info.Sender.TagObject = gameObject;
    }
    #endregion

    #region Card Handling
    //Recives event to play card, updating deck on all clients
    public void HandlePhotonEvents(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;

        switch (eventCode)
        {
            case 1:
                //------Playing Cards
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
                        playerHud.aceSelectionArea.GetComponent<CanvasGroup>().alpha = 1;
                    }

                    //Only use turn if jacks havn't reversed the order
                    if (trickCards[3] % 2 == 0 || trickCards[3] == 0)
                    {
                        turnHandler.PlayerUseTurn();
                    }
                    #endregion

                    deck.PlayCard(cards);
                    playerHud.topCardPrompt.GetComponent<Image>().sprite = deck.GetPlayDeckTopCard().GetCardSprite();
                }
                break;

            case 2:
                //------Game Start
                if (view.IsMine)
                {
                    byte seed = (byte)photonEvent.CustomData;
                    deck.Shuffle(seed);
                    deck.PlayFirstCard();

                    playerHud.drawCardsButton.interactable = true;
                    playerHud.sortCardsButton.interactable = true;
                    playerHud.believeButton.interactable = true;
                    playerHud.lastCardButton.interactable = true;
                    gameStartButton.gameObject.SetActive(false);

                    turnHandler.AddPlayers();
                }
                break;

            case 3:
                //------Drawing Cards
                if (view.IsMine)
                {
                    if ((int)photonEvent.CustomData != view.ViewID)
                    {
                        Card card = deck.DrawCard();
                    }

                    if ((int)photonEvent.CustomData == turnHandler.GetCurrentPlayer())
                    {
                        turnHandler.PlayerUseTurn();
                    }
                }
                break;

            case 4:
                //------Game Start Card Dealing
                int eventViewID = (int)photonEvent.CustomData;
                if (eventViewID == view.ViewID && view.IsMine)
                {
                    DrawMultipleCards(5);
                    DealStartCardsLoop();
                }
                break;

            case 5:
                //------Game Start Card Loop
                DealStartCards();
                break;

            case 6:
                //------Set trick cards to be used
                twoCount = 0;
                kingCount = 0;
                break;

            case 7:
                //------Update game log
                if (view.IsMine)
                {
                    string text = (string)photonEvent.CustomData;
                    playerHud.SendChatboxMessage(text);
                }
                break;

            case 8:
                //------handle Game Over
                string[] message = (string[])photonEvent.CustomData;

                playerHud.gameObject.SetActive(false);

                GameObject winnerScreen = GameObject.Find("WinnerScreen");
                winnerScreen.GetComponent<CanvasGroup>().alpha = 1;
                string gameOverMessage;

                if (message[1] == "Winner")
                {
                    if (firstWinner == message[0])
                    {
                        gameOverMessage = winnerScreen.GetComponentInChildren<Text>().text = message[0] + "\n is flawless!";
                    }
                    else
                    {
                        gameOverMessage = winnerScreen.GetComponentInChildren<Text>().text = message[0] + "\n is the winner!";
                    }
                }
                else 
                {
                    gameOverMessage = winnerScreen.GetComponentInChildren<Text>().text = message[0] + "\n ruined the game...";
                }   

                string path = "GameLog.txt";

                StreamWriter writer = new StreamWriter(path, true);
                writer.WriteLine(DateTime.Now.ToString());
                writer.WriteLine(playerHud.chatBoxText.GetComponent<Text>().text);
                writer.WriteLine(gameOverMessage);
                writer.WriteLine("----------------------------<  Game End  >----------------------------");
                writer.Close();

                StartCoroutine("ChangeScene");
                break;

            case 9:
                //------Set first winner
                firstWinner = (string)photonEvent.CustomData;
                break;
        }
        
        try
        {
            SetCinemachineCamera();
        }
        catch(ArgumentOutOfRangeException)
        {
            
        }
    }

    //Adds or removes cards from play hand when selected
    //Also numbers cards based on the order they will be played
    public void ChangeCardsToPlay(bool isPickup, byte cardId)
    {
        if (view.IsMine)
        {
            //Add the card to the cards to play
            if (isPickup)
            {
                //If hand is empty, add card
                if (cardsToPlay.Count == 0)
                {
                    cardsToPlay.Add(cardId);
                }
                //Checks the last card in cards to play with the card to be selected
                else if (CanSelectCard(cardsToPlay[cardsToPlay.Count - 1], cardId))
                {
                    cardsToPlay.Add(cardId);
                }
                else 
                {
                    cardsToPlay.Remove(cardId);
                    uiCards[cardId].GetComponent<UICardHandler>().isSelected = false;
                }
            }
            //Remove the card from cards to play
            else
            {
                cardsToPlay.Remove(cardId);
                uiCards[cardId].GetComponentInChildren<Text>().text = null;
            }

            //Render card order above each card
            foreach (byte card in cardsToPlay)
            {
                uiCards[card].GetComponentInChildren<Text>().text = (cardsToPlay.IndexOf(card) + 1).ToString();
            }

            //If the player has any cards selected, enable the play button
            if (cardsToPlay.Count > 0)
            {
                playerHud.playCardsButton.interactable = true;
            }
        }
    }

    //Checks if the selected cards can be played on each other
    public bool CanSelectCard(byte firstCardId, byte secondCardId)
    {
        Card firstCard = deck.FindCard(firstCardId);
        Card secondCard = deck.FindCard(secondCardId);

        //If cards have the same value ie: 2 10s, return true
        if (firstCard.GetValue() == secondCard.GetValue())
        {
            return true;
        }

        //If the suits match...
        if (firstCard.GetSuit() == secondCard.GetSuit())
        {
            //A jack can be played on an 8
            if (firstCard.GetValue() == 8 && secondCard.GetValue() == 11)
            {
                return true;
            }

            //An 8 can be played on a jack
            if (firstCard.GetValue() == 11 && secondCard.GetValue() == 8)
            {
                return true;
            }

            //An 2 or a king of hearts can be played on a 8
            if (firstCard.GetValue() == 8 && (secondCard.GetValue() == 2 || (secondCard.GetValue() == 13 && secondCard.GetSuit() == 1)))
            {
                return true;
            }

            //An 2 or a king of hearts can be played on a jack
            if (firstCard.GetValue() == 11 && (secondCard.GetValue() == 2 || (secondCard.GetValue() == 13 && secondCard.GetSuit() == 1)))
            {
                return true;
            }

            //A black queen can be played on an 8
            if (firstCard.GetValue() == 8 && ((secondCard.GetValue() == 12 && secondCard.GetSuit() == 3) || (secondCard.GetValue() == 12 && secondCard.GetSuit() == 4)))
            {
                return true;
            }

            //A black queen can be played on a jack
            if (firstCard.GetValue() == 11 && ((secondCard.GetValue() == 12 && secondCard.GetSuit() == 3) || (secondCard.GetValue() == 12 && secondCard.GetSuit() == 4)))
            {
                return true;
            }
        }

        return false;
    }

    [PunRPC]
    public void SetAceSuitRPC(byte[] content)
    {
        deck.SetAceSuit(content[0], content[1]);
        if (view.IsMine)
        {
            NetworkUpdateChatBox(PhotonNetwork.NickName + " changed the suit to " + deck.CheckSuit(content[1]));
        }
    }

    public void SetAceSuit(byte id, byte suit)
    {
        if (view.IsMine)
        {
            byte[] content = new byte[2];
            content[0] = id;
            content[1] = suit;

            view.RPC("SetAceSuitRPC", RpcTarget.All, content);
        }
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
        //Check if the command came from me and its my turn
        if (view.IsMine && turnHandler.GetCurrentPlayer() == view.ViewID)
        {
            RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };

            //Check for 2s played into the player
            if (deck.GetPlayDeckTopCard().GetValue() == 2 && twoCount > 0)
            {
                if (deck.FindCard(cardsToPlay[0]).GetValue() != 2)
                {
                    int numCards = twoCount * 2;

                    PhotonNetwork.RaiseEvent(ResetTrickCount, 0, eventOptions, SendOptions.SendReliable);

                    numCards += 2;
                    cardsToPlay.Clear();
                    DrawMultipleCards((byte)numCards);

                    NetworkUpdateChatBox("The quacks got to " + PhotonNetwork.NickName + " , " + numCards + " cards");
                    return;
                }
            }

            //Check for kings
            if (deck.GetPlayDeckTopCard().GetValue() == 13 && deck.GetPlayDeckTopCard().GetSuit() == 1 && kingCount > 0)
            {
                //If you dont play the 5 of hearts
                if (deck.FindCard(cardsToPlay[0]).GetValue() != 5 || deck.FindCard(cardsToPlay[0]).GetSuit() != 1)
                {
                    int numCards = kingCount * 5;

                    PhotonNetwork.RaiseEvent(ResetTrickCount, 0, eventOptions, SendOptions.SendReliable);

                    cardsToPlay.Clear();
                    DrawMultipleCards((byte)numCards);
                    return;
                }
                //If you do play the 5 of hearts
                else if (deck.FindCard(cardsToPlay[0]).GetValue() == 5 && deck.FindCard(cardsToPlay[0]).GetSuit() == 1)
                {
                    PhotonNetwork.RaiseEvent(ResetTrickCount, 0, eventOptions, SendOptions.SendReliable);
                    NetworkUpdateChatBox(PhotonNetwork.NickName + " is a King Slayer!");
                }
            }

            //Check for last card
            {
                if (myCards.Count - cardsToPlay.Count == 0 && hasKnocked == false)
                {
                    cardsToPlay.Clear();
                    DrawMultipleCards(2);
                    NetworkUpdateChatBox(PhotonNetwork.NickName + " never called last card, 2 cards!");
                    return;
                }
            }

            //Checks if the first card matches the deck ie: can be played
            if (!deck.CheckCardMatch(cardsToPlay[0]))
            {
                cardsToPlay.Clear();
                DrawMultipleCards(2);
                NetworkUpdateChatBox(PhotonNetwork.NickName + " should know better...2 cards");
                return;
            }

            string message = PhotonNetwork.NickName + " played ";
            if (cardsToPlay.Count < 4)
            {
                foreach (byte card in cardsToPlay)
                {
                    message += deck.FindCard(card).ToString() + ", ";
                }
            }
            else 
            {
                message += "...a lot of cards";
            }
            
            NetworkUpdateChatBox(message);
            NetworkPlayCards();
        }

        //If its not my turn, take 2 cards
        else if (view.IsMine)
        {
            NetworkUpdateChatBox(PhotonNetwork.NickName + " played out of turn, 2 cards!");
            DrawMultipleCards(2);
        }

    }

    public void BlindPlayCard()
    {
        if (view.IsMine)
        {
            NetworkUpdateChatBox(PhotonNetwork.NickName + " believes...");
            cardsToPlay.Clear();

            Card card = deck.DrawCard();
            myCards.Add(card.GetCardId(), card);

            int content = view.ViewID;
            RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(DrawCardEventCode, content, eventOptions, SendOptions.SendReliable);

            cardsToPlay.Add(card.GetCardId());
            TryPlayCard();
        }
    }
    #endregion

    #region Networking
    //Sends event to all players to replace the top card with cardId "content"
    private void NetworkPlayCards()
    {
        int cardCount = cardsToPlay.Count;
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

            //If the player is out of cards, game is over
            if (myCards.Count == 0 && deck.GetPlayDeckCount() > 1)
            {
                if (firstWinner == null)
                {
                    DrawMultipleCards(2);
                    NetworkUpdateChatBox(PhotonNetwork.NickName + " is the first winner, 2 cards!");
                    PhotonNetwork.RaiseEvent(SetFirstWinnerEventCode, PhotonNetwork.NickName, eventOptions, SendOptions.SendReliable);
                }
                else 
                {
                    string[] message = new string[2];
                    message[0] = PhotonNetwork.NickName;
                    message[1] = "Winner";

                    PhotonNetwork.RaiseEvent(GameOverEventCode, message, eventOptions, SendOptions.SendReliable);
                } 
            }
            playerHud.UpdateCardUI();
        }
    }

    //Picks up a card and alerts other players that the deck has been modified
    private void NetworkDrawCard()
    {
        if (view.IsMine)
        {
            hasKnocked = false;
            cardsToPlay.Clear();
            RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };

            //Check for twos
            if (twoCount > 0)
            {
                int numCards = twoCount * 2;
                NetworkUpdateChatBox(PhotonNetwork.NickName + " took " + numCards.ToString() + " cards");
                PhotonNetwork.RaiseEvent(ResetTrickCount, 0, eventOptions, SendOptions.SendReliable);
                twoCount = 0;
                DrawMultipleCards((byte)numCards);
                return;
            }

            //Check for kings
            else if (kingCount > 0)
            {
                int numCards = kingCount * 5;
                NetworkUpdateChatBox(PhotonNetwork.NickName + " couldn't commit regicide, " + numCards.ToString() + " cards");
                PhotonNetwork.RaiseEvent(ResetTrickCount, 0, eventOptions, SendOptions.SendReliable);
                kingCount = 0;
                DrawMultipleCards((byte)numCards);
                return;
            }

            int content = view.ViewID;
            PhotonNetwork.RaiseEvent(DrawCardEventCode, content, eventOptions, SendOptions.SendReliable);

            
            if (deck.GetPlayDeckCount() > 1)
            {
                if (turnHandler.GetCurrentPlayer() == view.ViewID)
                {
                    NetworkUpdateChatBox(PhotonNetwork.NickName + " picked up");
                }
            }

            Card card = deck.DrawCard();
            myCards.Add(card.GetCardId(), card);
            playerHud.UpdateCardUI();
        }
    }

    //Enables all players UI, plays the first card on the deck and deals 5 cards to everyone
    public void HostGameStart()
    {
        if (PhotonNetwork.IsMasterClient && view.IsMine)
        {
            System.Random rand = new System.Random();

            //Generates a random number and sends in event to ensure all players use the same "random" order 
            int num = rand.Next(0, 255);
            byte seed = (byte)num;

            RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(GameStartEventCode, seed, eventOptions, SendOptions.SendReliable);

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

    public void NetworkUpdateChatBox(string message)
    {
        RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(UpdateGameLogEventCode, message, eventOptions, SendOptions.SendReliable);
    }

    public void CheckLastCardCall()
    {
        if (view.IsMine)
        {
            cardsToPlay.Clear();
            if (myCards.Count == 1)
            {
                hasKnocked = true;
                NetworkUpdateChatBox(PhotonNetwork.NickName + ": *knock knock* Last Card");
            }
            else
            {
                DrawMultipleCards(2);
                NetworkUpdateChatBox(PhotonNetwork.NickName + " knocked too early, two cards");
            }
        }
    }

    public void BumGame()
    {
        string[] message = new string[2];
        message[0] = PhotonNetwork.NickName;
        message[1] = "BumGame";
        RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(GameOverEventCode, message, eventOptions, SendOptions.SendReliable);
    }
    #endregion

    #region UI
    public void SetCinemachineCamera()
    {
        if (turnHandler.GetCurrentPlayer() == view.ViewID)
        {
            virtualCamera.Priority = 1;
        }
        else 
        {
            virtualCamera.Priority = 0;
        }
    }
    #endregion

    #region Coroutines
    //Displays winner screen, disconnects from server and loads starting scene
    IEnumerator ChangeScene()
    {
        for (float ft = 3f; ft >= 0; ft -= 1f)
        {
            if (ft < 1)
            {
                PhotonNetwork.Disconnect();
                SceneManager.LoadScene("Scene_MainMenu");
            }

            yield return new WaitForSeconds(1f);
        }
    }
    #endregion
}