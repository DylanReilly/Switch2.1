
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
    public GameObject easyInsultBox = null;
    public GameObject topCardPrompt = null;

    //GameObjects
    public GameObject handStartPosition = null;
    public GameObject chatBox = null;
    public GameObject chatBoxText = null;

    //Buttons
    public Button sortCardsButton = null;
    public Button drawCardsButton = null;
    public Button playCardsButton = null;
    public Button believeButton = null;
    public Button lastCardButton = null;
    public GameObject aceSelectionArea = null;

    //Events
    public event Action drawCardsEvent;
    public event Action playCardsEvent;
    public event Action hudSpawned;
    public event Action believeEvent;

    //Message Handling
    public InputField textInput = null;
    public Button sendButton = null;

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

        GameObject.Find("InGameMenu").GetComponent<PauseMenu>().easyInsultBox = easyInsultBox;
        GameObject.Find("InGameMenu").GetComponent<PauseMenu>().topCardPrompt = topCardPrompt;

        if (Application.platform == RuntimePlatform.Android)
        {
            sortCardsButton.gameObject.GetComponent<RectTransform>().position += new Vector3(0, 55f, 0);
            drawCardsButton.gameObject.GetComponent<RectTransform>().position += new Vector3(0, 55f, 0);
            playCardsButton.gameObject.GetComponent<RectTransform>().position += new Vector3(0, 55f, 0);
            believeButton.gameObject.GetComponent<RectTransform>().position += new Vector3(0, 55f, 0);
            imagePrefab.gameObject.GetComponent<RectTransform>().position += new Vector3(20f, 0, 0);
        }

        hudSpawned?.Invoke();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SendTextMessage();
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

    public void BelieveEvent()
    {
        believeEvent?.Invoke();
    }

    public void SetAceSuit(int suit)
    {
        byte[] content = new byte[2];
        content[0] = (byte)(player.deck.GetPlayDeckTopCard().GetCardId());
        content[1] = (byte)suit;
        player.photonView.RPC("SetAceSuitRPC", RpcTarget.All, content);
        aceSelectionArea.GetComponent<CanvasGroup>().alpha = 0;
    }

    public void CallLastCard()
    {
        player.CheckLastCardCall();
    }
    #endregion

    #region Chatbox
    //Fades the chatbox out after sending a message
    public void SendChatboxMessage(string message)
    {
        StopCoroutine("ChatBoxFade");
        chatBox.GetComponent<CanvasGroup>().alpha = 1;

        chatBoxText.GetComponent<Text>().text += "\n";
        chatBoxText.GetComponent<Text>().text += message;

        chatBoxText.GetComponent<RectTransform>().localPosition += new Vector3(0, 16, 0);

        StartCoroutine("ChatBoxFade");
    }

    public void SendTextMessage()
    {
        string message = "<color=green>" + PhotonNetwork.NickName + "</color>" + " " + textInput.text;
        //Limits the size of messages to not overload the chat
        if (message.Length > 40)
        {
            message = message.Substring(0, 40);
        }
        
        player.NetworkUpdateChatBox(message);
        textInput.text = "";
    }

    public void SendTextMessage(string message)
    {
        message = "<color=green>" + PhotonNetwork.NickName + "</color>" + " " + message;
        player.NetworkUpdateChatBox(message);
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

        foreach (KeyValuePair<byte, Card> card in player.GetMyCards())
        {
            Image imageInstance = Instantiate(imagePrefab);
            imageInstance.transform.SetParent(handStartPosition.transform, false);
            imageInstance.sprite = card.Value.GetCardSprite();
            imageInstance.rectTransform.anchoredPosition += new Vector2(offset, 0);

            if(Application.platform == RuntimePlatform.Android)
            {
                imageInstance.rectTransform.localScale *= 1.5f;
            }

            player.uiCards.Add(card.Value.GetCardId(), imageInstance);

            //Offset moves cards over so they aren't rendered on top of each other
            offset += 50;
        }
    }

    public void SortHand()
    {
        List<KeyValuePair<byte, Card>> myList = player.GetMyCards().ToList();

        myList.Sort(
            delegate (KeyValuePair<byte, Card> pair1, KeyValuePair<byte, Card> pair2)
            {
                return pair1.Value.GetValue().CompareTo(pair2.Value.GetValue());
            }
        );

        player.GetMyCards().Clear();

        foreach (KeyValuePair<byte, Card> card in myList)
        {
            player.GetMyCards().Add(card.Key, card.Value);
        }
        UpdateCardUI();
    }
    #endregion
}
