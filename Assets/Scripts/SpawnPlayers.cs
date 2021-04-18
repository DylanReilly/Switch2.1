using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject player;
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    public Player[] playerList;

    private void Start()
    {
        playerList = FindObjectsOfType<Player>();
        Debug.Log(playerList.Length);
    }

    public void SelectSpawnPoint(int point)
    {
        bool canSpawn = true;

        foreach (Player player in playerList)
        {
            Debug.Log("Working");
            if (spawnPoints[point - 1].transform.position == player.transform.position)
            {
                Debug.Log("Point used");
                canSpawn = false;
                break;
            }
        }

        if (canSpawn)
        {
            PhotonNetwork.Instantiate(player.name, spawnPoints[point - 1].transform.position, Quaternion.identity);
            gameObject.SetActive(false);
        }
    }
}
