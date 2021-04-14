using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    Dictionary<short, Card> myCards = new Dictionary<short, Card>();
    Dictionary<short, Image> uiCards = new Dictionary<short, Image>();
    [SerializeField] private Image imagePrefab;
    [SerializeField] GameObject handStartPosition;

    [SerializeField]private int playerId = -1;
    Deck deck = null;
    PhotonView view = null;
    Camera mainCamera = null;
    Canvas hud = null;

    public void SetPlayerId(int id)
    {
        playerId = id;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        deck = GameObject.FindWithTag("Deck").GetComponent<Deck>();
        view = GetComponent<PhotonView>();
        hud = GameObject.FindWithTag("Hud").GetComponent<Canvas>();
        handStartPosition = hud.transform.Find("HandStartPosition").gameObject;
        if (view.IsMine)
        {
            playerId = view.ViewID;
        }
    }

    //Takes a card from the drawdeck and adds it to the players hand
    public void DrawCard()
    {
        Card card = deck.DrawCard();
        myCards.Add(card.GetCardId(), card);
        UpdateCardUI();
    }

    //Removes a card from the players hand and adds it to the playdeck
    public void PlayCard(short cardId)
    {
        //Check if the card to play matches the top card of the deck. If not, do nothing
        if (!deck.CheckCardMatch(myCards[cardId]))
        {
            return;
        }
        myCards.Remove(cardId);
        deck.PlayCard(cardId);
        UpdateCardUI();
    }

    //Simulates another player drawing a card. Does not add the card to the players hand
    [PunRPC]
    public void UpdateDrawDeckRpc()
    {
        deck.DrawCard();
    }

    //Simulates another player playing a card. Does not affect the local players hand
    [PunRPC]
    public void UpdatePlayDeckRpc(short cardId)
    {
        deck.PlayCard(cardId);
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
            short temp = 39;
            if (view.IsMine)
            {
                PlayCard(temp);
            }
            else
            {
                view.RPC("UpdatePlayDeckRpc", RpcTarget.Others, temp);
            }
        }
    }    
}
