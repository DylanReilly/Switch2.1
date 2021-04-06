using ExitGames.Client.Photon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonRegister : MonoBehaviour
{
    private void Start()
    {
        PhotonPeer.RegisterType(typeof(SpawnPoint), (byte)'S', SpawnPoint.Serialize, SpawnPoint.Deserialize);
        PhotonPeer.RegisterType(typeof(Card), (byte)'C', Card.Serialize, Card.Deserialize);
    }
}
