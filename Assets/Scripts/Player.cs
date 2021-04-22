﻿using System.Collections.Generic;
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

    //Event Codes
    public const byte PlayCardEventCode = 1;
    public const byte GameStartEventCode = 2;
    public const byte DrawCardEventCode = 3;
    public const byte DrawStartCardsEventCode = 4;
    public const byte DealStartCardsLoopCode = 5;
    public const byte ResetTrickCount = 6;
    public const byte UpdateGameLogEventCode = 7;
    public const byte GameOverEventCode = 8;

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
        if (PhotonNetwork.IsMasterClient)
        {
            gameStartButton = playerHud.transform.Find("GameStartButton").GetComponent<Button>();
            gameStartButton.gameObject.SetActive(true);
            gameStartButton.onClick.AddListener(HostGameStart);
        }
        else
        {
            gameStartButton = playerHud.transform.Find("GameStartWaitButton").GetComponent<Button>();
            gameStartButton.gameObject.SetActive(true);
        }

        //Subscribe to events
        playerHud.drawCardsEvent += NetworkDrawCard;
        playerHud.playCardsEvent += TryPlayCard;
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

    #region Network Overrides
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        //base.OnMasterClientSwitched(newMasterClient);
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("Scene_Loading");
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
                    playerHud.aceSelectionArea.GetComponent<CanvasGroup>().alpha = 1;
                }

                //Only use turn if jacks havn't reversed the order
                if (trickCards[3] % 2 == 0 || trickCards[3] == 0)
                {
                    turnHandler.PlayerUseTurn();
                }
                #endregion

                deck.PlayCard(cards);
            }
            //playCardsButton.interactable = false;
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
                }
            }
        }

        //------Game Start
        else if (photonEvent.Code == GameStartEventCode)
        {
            if (view.IsMine)
            {
                playerHud.drawCardsButton.interactable = true;
                playerHud.sortCardsButton.interactable = true;
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

        //------Update game log
        else if (photonEvent.Code == UpdateGameLogEventCode)
        {
            if (view.IsMine)
            {
                string message = (string)photonEvent.CustomData;
                playerHud.SendChatboxMessage(message);
            }
        }

        //------handle Game Over
        else if (photonEvent.Code == GameOverEventCode)
        {
            string winner = (string)photonEvent.CustomData;
            NetworkUpdateChatBox(winner + " is the winner!");

            playerHud.gameObject.SetActive(false);

            GameObject winnerScreen = GameObject.Find("WinnerScreen");
            winnerScreen.GetComponent<CanvasGroup>().alpha = 1;
            winnerScreen.GetComponentInChildren<Text>().text = winner + "\n is the winner!";

            string path = "GameLog.txt";

            StreamWriter writer = new StreamWriter(path, true);
            writer.WriteLine(DateTime.Now.ToString());
            writer.WriteLine(playerHud.chatBoxtext.text);
            writer.WriteLine("----------------------------<  Game End  >----------------------------");
            writer.Close();

            StartCoroutine("ChangeScene");
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

            if (cardsToPlay.Count > 0)
            {
                playerHud.playCardsButton.interactable = true;
            }
        }
    }

    [PunRPC]
    public void SetAceSuitRPC(byte[] content)
    {
        deck.SetAceSuit(content[0], content[1]);
    }

    public void SetAceSuit(byte id, byte suit)
    {
        if (view.IsMine)
        {
            byte[] content = new byte[2];
            content[0] = id;
            content[1] = suit;

            NetworkUpdateChatBox(PhotonNetwork.NickName + " changed the suit to " + deck.CheckSuit(suit));
            view.RPC("SetAceSuit", RpcTarget.All, content);
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
                NetworkUpdateChatBox("QUUAAAAAAACK!");
            }

            //Check for kings
            if (deck.GetPlayDeckTopCard().GetValue() == 13 && deck.GetPlayDeckTopCard().GetSuit() == 1 && kingCount > 0)
            {
                if (deck.FindCard(cardsToPlay[0]).GetValue() != 5 || deck.FindCard(cardsToPlay[0]).GetSuit() != 1)
                {
                    int numCards = kingCount * 5;

                    PhotonNetwork.RaiseEvent(ResetTrickCount, 0, eventOptions, SendOptions.SendReliable);

                    cardsToPlay.Clear();
                    DrawMultipleCards((byte)numCards);
                    return;
                }
                else if (deck.FindCard(cardsToPlay[0]).GetValue() == 5 && deck.FindCard(cardsToPlay[0]).GetSuit() == 1)
                {
                    PhotonNetwork.RaiseEvent(ResetTrickCount, 0, eventOptions, SendOptions.SendReliable);
                    NetworkUpdateChatBox(PhotonNetwork.NickName + " is a King Slayer!");
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

            string message = PhotonNetwork.NickName + " ";
            foreach (byte card in cardsToPlay)
            {
                message += deck.FindCard(card).ToString() + ", ";
            }

            NetworkUpdateChatBox(message);
            NetworkPlayCards();
        }
        else if (view.IsMine)
        {
            NetworkUpdateChatBox(PhotonNetwork.NickName + " played out of turn, 2 cards!");
            DrawMultipleCards(2);
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

            //If the player is out of cards, game is over
            if (myCards.Count == 0 && deck.GetPlayDeckCount() > 1)
            {
                PhotonNetwork.RaiseEvent(GameOverEventCode, PhotonNetwork.NickName, eventOptions, SendOptions.SendReliable);
            }

            playerHud.UpdateCardUI();
        }
    }

    //Picks up a card and alerts other players that the deck has been modified
    private void NetworkDrawCard()
    {
        if (view.IsMine)
        {
            cardsToPlay.Clear();
            RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };

            //Check for twos
            if (twoCount > 0)
            {
                int numCards = twoCount * 2;
                NetworkUpdateChatBox(PhotonNetwork.NickName + " didn't believe enough and took " + numCards.ToString() + " cards");
                PhotonNetwork.RaiseEvent(ResetTrickCount, 0, eventOptions, SendOptions.SendReliable);
                twoCount = 0;
                DrawMultipleCards((byte)numCards);
                return;
            }

            //Check for kings
            else if (kingCount > 0)
            {
                int numCards = kingCount * 2;
                NetworkUpdateChatBox(PhotonNetwork.NickName + " couldn't commit regicide, " + numCards.ToString() + " cards");
                PhotonNetwork.RaiseEvent(ResetTrickCount, 0, eventOptions, SendOptions.SendReliable);
                kingCount = 0;
                DrawMultipleCards((byte)numCards);
                return;
            }

            int content = view.ViewID;
            PhotonNetwork.RaiseEvent(DrawCardEventCode, content, eventOptions, SendOptions.SendReliable);

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

    public void UpdateGameLogOnPickup()
    {
        if (view.IsMine && turnHandler.GetCurrentPlayer() == view.ViewID)
        {
            NetworkUpdateChatBox(PhotonNetwork.NickName + " picked up");
        }
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
                SceneManager.LoadScene("Scene_Loading");
            }

            yield return new WaitForSeconds(1f);
        }
    }
    #endregion
}