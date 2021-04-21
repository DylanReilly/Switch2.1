using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviourPunCallbacks
{
    public void LeaveGame()
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("Scene_Loading");
    }
}
