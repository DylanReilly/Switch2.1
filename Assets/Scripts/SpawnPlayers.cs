using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject player;
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    private void Start()
    {
        Transform spawnPoint = null;

        spawnPoint = spawnPoints[Random.Range(0, 4)].transform;

        //Loops through spawn points to find unused spawn
        //foreach (SpawnPoint location in spawnPoints)
        //{
        //    if (location.GetUsedStatus() == false)
        //    {
        //        spawnPoint = location.transform;
        //        location.SetUsed(true);
        //        break;
        //    }
        //}

        PhotonNetwork.Instantiate(player.name, spawnPoint.position, Quaternion.identity);
    }
}
