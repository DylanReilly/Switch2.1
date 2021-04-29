using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviourPunCallbacks
{
    public GameObject easyInsultBox = null;
    public GameObject scenery = null;
    public GameObject topCardPrompt = null;
    void Start()
    {
        scenery = GameObject.Find("Scenery");
    }

    public void LeaveGame()
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("Scene_MainMenu");
    }

    public void EnableEasyInsult()
    {
        easyInsultBox.gameObject.SetActive(true);
    }

    public void DisableEasyInsult()
    {
        easyInsultBox.gameObject.SetActive(false);
    }

    public void EnableTopCardPrompt()
    {
        topCardPrompt.GetComponent<CanvasGroup>().alpha = 1;
    }

    public void DisableTopCardPrompt()
    {
        topCardPrompt.GetComponent<CanvasGroup>().alpha = 0;
    }

    public void EnableScenery()
    {
        scenery.gameObject.SetActive(true);
    }

    public void DisableScenery()
    {
        scenery.gameObject.SetActive(false);
    }
}
