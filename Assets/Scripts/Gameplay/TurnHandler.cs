using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnHandler : MonoBehaviour
{
    public SpawnPlayers spawns = null;
    public Player[] players = new Player[8];
    public List<Player> playerQueue = new List<Player>();

    //Populates a list of players in order, based on their spawn point
    public void AddPlayers()
    {
        GameObject[] temp = GameObject.FindGameObjectsWithTag("LocalPlayer");
        for(int i = 0; i < temp.Length; i++)
        {
            players[temp[i].GetComponent<Player>().GetSpawnPoint()] = temp[i].GetComponent<Player>();
        }

        foreach (Player player in players)
        {
            if (player != null)
            {
                playerQueue.Add(player);
            }
        }
    }

    //Reverses the order players are playing in
    public void ReverseOrder()
    {
        playerQueue.Reverse();
    }

    //Moves a player from the top of the list to the bottom
    public void PlayerUseTurn()
    {
        Player player = playerQueue[0];
        player.SetCinemachineCamera(0);
        playerQueue.Remove(player);
        playerQueue.Add(player);
        playerQueue[0].SetCinemachineCamera(1);
    }

    public int GetCurrentPlayer()
    {
        Player player = playerQueue[0];
        return player.GetComponent<PhotonView>().ViewID;
    }
}
