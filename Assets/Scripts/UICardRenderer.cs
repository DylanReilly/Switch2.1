using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICardRenderer : MonoBehaviour
{
    [SerializeField] private Image imagePrefab;
    [SerializeField] GameObject HandStartPosition;

    private Dictionary<int, Card> referenceDeck = new Dictionary<int, Card>();

    [SerializeField]public Player player;
    //An offset to stop cards rendering directly on top of each other
    int OFFSET = 0;

    private void Start()
    {
        //START HERE
        HandStartPosition = GameObject.Find("CardUI");
        LoadReferenceDeck();
    }

    //Loads dictionary storing card data, using cardId as a key
    private void LoadReferenceDeck()
    {
        UnityEngine.Object[] loadDeck;
        loadDeck = Resources.LoadAll("Cards/CardInstances", typeof(Card));

        foreach (Card card in loadDeck)
        {
            referenceDeck.Add(card.GetCardId(), card);
        }
    }

    public void UpdateCardUI(int cardId)
    {
        //Renders the latest card in the players hand to the UI
        Card card = referenceDeck[cardId];

        Image imageInstance = Instantiate(imagePrefab);
        imageInstance.transform.SetParent(HandStartPosition.transform, false);
        imageInstance.sprite = card.GetCardSprite();
        imageInstance.rectTransform.anchoredPosition += new Vector2(OFFSET, 0);

        //Offset moves cards over so they arent rendered on top of each other
        OFFSET += 50;
    }
}
