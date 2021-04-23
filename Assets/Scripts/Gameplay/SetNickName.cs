using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine.UI;

public class SetNickName : MonoBehaviour
{
    public Text stoolName = null;

    public void SetName()
    {
        PhotonNetwork.NickName = stoolName.text;
    }
}
