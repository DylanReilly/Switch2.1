using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class JoinMenu : MonoBehaviourPunCallbacks
{
    public InputField createInput;
    public InputField joinInput;

    public void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 8;

        PhotonNetwork.CreateRoom(createInput.text, roomOptions);
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(joinInput.text);
    }

    //Called when joining a room. Host also joins room when creating, so this is also called
    public override void OnJoinedRoom()
    {
        //Changes scene for all players
        PhotonNetwork.LoadLevel("Scene_Map_Shed");
    }

    public void ReturnToMenu()
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("Scene_MainMenu");
    }
}
