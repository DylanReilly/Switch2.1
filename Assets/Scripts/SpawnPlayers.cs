using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject player;
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    public GameObject[] playerList;
    public TurnHandler turnHandler = null;

    private void Start()
    {
        playerList = GameObject.FindGameObjectsWithTag("LocalPlayer");
    }

    public void SelectSpawnPoint(int point)
    {
        bool canSpawn = true;

        foreach (GameObject player in playerList)
        {
            Debug.Log("Working");
            if (spawnPoints[point].transform.position == player.transform.position)
            {
                Debug.Log("Point used");
                canSpawn = false;
                break;
            }
        }

        if (canSpawn)
        {
            PhotonNetwork.Instantiate(player.name, spawnPoints[point].transform.position, Quaternion.identity);
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
        }
    }
}
