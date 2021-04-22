
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HudHandler : MonoBehaviour
{
    public Player player = null;
    public Image imagePrefab = null;

    //GameObjects
    public GameObject handStartPosition = null;
    public GameObject chatBox = null;
    public Text chatBoxtext = null;

    //Buttons
    public Button sortCardsButton = null;
    public Button drawCardsButton = null;
    public Button playCardsButton = null;
    public GameObject aceSelectionArea = null;

    //Events
    public event Action drawCardsEvent;
    public event Action playCardsEvent;

    // Start is called before the first frame update
    public void Start()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("LocalPlayer"))
        {
            if (go.GetComponent<PhotonView>().IsMine)
            {
                player = go.GetComponent<Player>();
                break;
            }
        }
    }

    #region Button Events
    public void DrawCardsEventCall()
    {
        drawCardsEvent?.Invoke();
    }

    public void PlayCardsEventCall()
    {
        playCardsEvent?.Invoke();
    }

    public void SetAceSuit(int suit)
    {
        byte[] content = new byte[2];
        content[0] = (byte)(player.deck.GetPlayDeckTopCard().GetCardId());
        content[1] = (byte)suit;
        player.photonView.RPC("SetAceSuitRPC", RpcTarget.All, content);
        aceSelectionArea.GetComponent<CanvasGroup>().alpha = 0;
    }
    #endregion

    #region Chatbox
    //Fades the chatbox out after sending a message
    public void SendChatboxMessage(string message)
    {
        StopCoroutine("ChatBoxFade");
        chatBox.GetComponent<CanvasGroup>().alpha = 1;

        chatBoxtext.text += "\n";
        chatBoxtext.text += message;
        StartCoroutine("ChatBoxFade");
    }

    IEnumerator ChatBoxFade()
    {
        for (float ft = 1f; ft >= 0; ft -= 0.03f)
        {
            if (ft < 0.05) { ft = 0f; }
            chatBox.GetComponent<CanvasGroup>().alpha = ft;
            yield return new WaitForSeconds(.1f);
        }
    }

    #endregion

    #region Card Rendering
    public void UpdateCardUI()
    {
        int offset = 0;

        //Destroys all cards to accomodate cards being removed
        var cardList = GameObject.FindGameObjectsWithTag("UICard");
        foreach (GameObject uiCard in cardList)
        {
            Destroy(uiCard);
        }

        player.uiCards.Clear();

        foreach (KeyValuePair<byte, Card> card in player.myCards)
        {
            Image imageInstance = Instantiate(imagePrefab);
            imageInstance.transform.SetParent(handStartPosition.transform, false);
            imageInstance.sprite = card.Value.GetCardSprite();
            imageInstance.rectTransform.anchoredPosition += new Vector2(offset, 0);

            player.uiCards.Add(card.Value.GetCardId(), imageInstance);

            //Offset moves cards over so they aren't rendered on top of each other
            offset += 50;
        }
    }

    public void SortHand()
    {
        List<KeyValuePair<byte, Card>> myList = player.myCards.ToList();

        myList.Sort(
            delegate (KeyValuePair<byte, Card> pair1, KeyValuePair<byte, Card> pair2)
            {
                return pair1.Value.GetValue().CompareTo(pair2.Value.GetValue());
            }
        );

        player.myCards.Clear();

        foreach (KeyValuePair<byte, Card> card in myList)
        {
            player.myCards.Add(card.Key, card.Value);
        }
        UpdateCardUI();
    }
    #endregion
}
