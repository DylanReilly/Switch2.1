using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [SerializeField] List<Card> myCards = new List<Card>();
    [SerializeField] private Image imagePrefab;
    [SerializeField] GameObject handStartPosition;

    [SerializeField]private int playerId = -1;
    Deck deck = null;
    PhotonView view = null;
    Camera mainCamera = null;
    Canvas hud = null;

    //An offset to stop cards rendering directly on top of each other
    int OFFSET = 0;

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

    //Takes a card from the deck and adds it to the players hand
    public void DrawCard()
    {
        Card card = deck.DrawCard();
        myCards.Add(card);
        UpdateCardUI(card.GetCardId());
    }

    //Simulates another player drawing a card. Does not add the card to the players hand
    [PunRPC]
    public void UpdateDeckRpc()
    {
        deck.DrawCard();
    }

    public void UpdateCardUI(short cardId)
    {
        //Renders the latest card in the players hand to the UI
        Card card = deck.FindCard(cardId);

        Image imageInstance = Instantiate(imagePrefab);
        imageInstance.transform.SetParent(handStartPosition.transform, false);
        imageInstance.sprite = card.GetCardSprite();
        imageInstance.rectTransform.anchoredPosition += new Vector2(OFFSET, 0);

        //Offset moves cards over so they aren't rendered on top of each other
        OFFSET += 50;
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
                view.RPC("UpdateDeckRpc", RpcTarget.Others);
            }
           
        }
    }
}
