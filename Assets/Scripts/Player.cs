using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;

public class Player : MonoBehaviour
{
    Dictionary<short, Card> myCards = new Dictionary<short, Card>();
    Dictionary<short, Image> uiCards = new Dictionary<short, Image>();
    List<short> cardsToPlay = new List<short>();

    [SerializeField] private Image imagePrefab;
    [SerializeField] GameObject handStartPosition;

    private int playerId = -1;
    Deck deck = null;
    PhotonView view = null;
    Camera mainCamera = null;
    Canvas hud = null;

    public const byte PlayCardEventCode = 1;

    public void SetPlayerId(int id)
    {
        playerId = id;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        view = GetComponent<PhotonView>();
        hud = GameObject.FindWithTag("Hud").GetComponent<Canvas>();
        handStartPosition = hud.transform.Find("HandStartPosition").gameObject;
        if (view.IsMine)
        {
            playerId = view.ViewID;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate("Deck", new Vector3(0, 1.1f, 0), Quaternion.identity);
        }
        deck = GameObject.FindWithTag("Deck").GetComponent<Deck>();

        //Subscribe to event
        PhotonNetwork.NetworkingClient.EventReceived += PlayCard;
    }

    private void OnDestroy()
    {
        //Unsubscribe from event
        PhotonNetwork.NetworkingClient.EventReceived -= PlayCard;
    }

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
        if(eventCode == PlayCardEventCode)
        {
            short[] cards = (short[])photonEvent.CustomData;
            deck.PlayCard(cards);
        }   
    }

    //Simulates another player drawing a card. Does not add the card to the players hand
    [PunRPC]
    public void UpdateDrawDeckRpc()
    {
        deck.DrawCard();
    }

    public void UpdateCardUI()
    {
        int offset = 0;

        //Destroys all cards to accomidate cards being removed
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

    public void SelectCard(short id)
    {
        uiCards[id].rectTransform.anchoredPosition += new Vector2(uiCards[id].rectTransform.anchoredPosition.x, uiCards[id].rectTransform.anchoredPosition.y + 20);
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
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

        if (Input.GetKeyUp(KeyCode.P))
        {
            if (view.IsMine)
            {
                cardsToPlay.Add(38);
                cardsToPlay.Add(25);
                cardsToPlay.Add(12);

                //Checks if the first card matches the deck ie: can be played
                if (!deck.CheckCardMatch(cardsToPlay[0])) 
                {
                    //TODO 2 cards cause you should know better...
                    Debug.Log("Invalid card");
                    return; 
                }

                //Sends event to all players to replace the top card with cardId "content"
                short[] content = cardsToPlay.ToArray();
                RaiseEventOptions eventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                PhotonNetwork.RaiseEvent(PlayCardEventCode, content, eventOptions, SendOptions.SendReliable);

                //Removes the card from player hand and updates UI to match
                foreach (short cardId in cardsToPlay)
                {
                    myCards.Remove(cardId);
                }
                UpdateCardUI();
            }
        }
    }    
}
