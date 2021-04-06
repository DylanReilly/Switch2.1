using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject player;
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    PhotonView view;

    private void Start()
    {
        view = GetComponent<PhotonView>();
        Transform spawnPoint = null;

        //Loops through spawn points to find unused spawn
        //foreach (SpawnPoint location in spawnPoints)
        //{
        //    if (location.GetIsUsed() == false)
        //    {
        //        spawnPoint = location.transform;
        //        view.RPC("UseSpawnPointRPC", RpcTarget.All, location);
        //        break;
        //    }
        //}
        spawnPoint = spawnPoints[Random.Range(0, 4)].transform;

        PhotonNetwork.Instantiate(player.name, spawnPoint.position, Quaternion.identity);
    }

    [PunRPC]
    void UseSpawnPointRPC(SpawnPoint spawnPoint)
    {
        spawnPoints.Remove(spawnPoint);
    }
}
