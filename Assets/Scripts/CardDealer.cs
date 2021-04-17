using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;

public class CardDealer : MonoBehaviour
{
    [SerializeField]Deck deck = null;

    void Start()
    {
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        { 
            //player.TagObject
        }
    }
}
